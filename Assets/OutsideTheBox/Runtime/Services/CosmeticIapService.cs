using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if OUTSIDE_THE_BOX_UNITY_IAP
using UnityEngine.Purchasing;
#endif

namespace OutsideTheBox
{
    /// <summary>
    /// Owns the store controller for permanent cosmetics and repeatable gem packs.
    /// </summary>
    public sealed class CosmeticIapService : MonoBehaviour
    {
        public static CosmeticIapService Instance { get; private set; }

        private readonly HashSet<string> ownedProductIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> configuredProductIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> gemProductIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> availableProductIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> localizedPrices = new(StringComparer.Ordinal);
        private Action<string, bool> entitlementHandler;
        private bool initializationStarted;
        private bool productsReady = false;
        private bool connecting;
        public event Action Changed;
        public event Action<int> GemsDelivered;
        public string PurchaseStatus { get; private set; } = string.Empty;
        public bool ProductsReady => productsReady;

#if OUTSIDE_THE_BOX_UNITY_IAP
        private StoreController storeController;
        private readonly Dictionary<string, PendingOrder> pendingGems = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> pendingGemOwners = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> creditedGems = new(StringComparer.Ordinal);
        private readonly HashSet<string> confirmingGems = new(StringComparer.Ordinal);
        private bool deliveringGems;
        private string requestedGem;
        private string requestedGemOwner;
        private float nextGemRetry;
#endif

        public bool GemPurchaseInProgress
        {
            get
            {
#if OUTSIDE_THE_BOX_UNITY_IAP
                return requestedGem != null || pendingGems.Count > 0;
#else
                return false;
#endif
            }
        }

        public bool IsProductAvailable(string productId) =>
            !string.IsNullOrEmpty(productId) && availableProductIds.Contains(productId);

        public static CosmeticIapService Ensure(GameObject host, IEnumerable<string> productIds,
            Action<string, bool> onEntitled)
        {
            // ProjectBuilder calls GameUIController.Initialize while baking prefabs in Edit Mode.
            // Unity IAP only registers a store factory for a running player, so attempting to
            // create StoreController during prefab generation throws "Store Factory not present".
            if (!Application.isPlaying) return host.GetComponent<CosmeticIapService>();

            if (Instance == null)
            {
                Instance = host.GetComponent<CosmeticIapService>();
                if (Instance == null) Instance = host.AddComponent<CosmeticIapService>();
            }

            Instance.entitlementHandler = onEntitled;
            Instance.Configure(productIds);
            return Instance;
        }

        public void ReapplyEntitlements()
        {
            if (LiveOpsManager.Instance?.Players?.CloudProgressReady != true) return;
            foreach (var productId in ownedProductIds) entitlementHandler?.Invoke(productId, true);
#if OUTSIDE_THE_BOX_UNITY_IAP
            _ = DeliverPendingGemsAsync();
#endif
        }

        public string GetLocalizedPrice(string productId)
        {
            return !string.IsNullOrWhiteSpace(productId) && localizedPrices.TryGetValue(productId, out var price)
                ? price
                : string.Empty;
        }

        public bool Purchase(string productId)
        {
            if (!productsReady || string.IsNullOrWhiteSpace(productId) ||
                !configuredProductIds.Contains(productId) || !IsProductAvailable(productId))
            {
                AnalyticsService.Log("iap_unavailable", "product_id", productId ?? "unconfigured");
                return false;
            }

#if OUTSIDE_THE_BOX_UNITY_IAP
            var players = LiveOpsManager.Instance?.Players;
            if (gemProductIds.Contains(productId))
            {
                if (!LiveOpsConfigStore.Current.gemPurchasesEnabled || players?.CloudProgressReady != true ||
                    !LiveOpsConfigStore.Current.cloudProgressEnabled || GemPurchaseInProgress) return false;
                requestedGem = productId;
                requestedGemOwner = GemPurchaseLedger.Key("account:" + players.PlayerId);
                SetStatus("WAITING FOR PAYMENT");
            }
            AnalyticsService.Log("iap_requested", "product_id", productId);
            try
            {
                if (requestedGem != null)
                    storeController.GooglePlayStoreExtendedService?.SetObfuscatedAccountId(requestedGemOwner);
                storeController.PurchaseProduct(productId);
            }
            catch (Exception exception)
            {
                requestedGem = null;
                requestedGemOwner = null;
                SetStatus("PURCHASE COULD NOT START. PLEASE RETRY.");
                Debug.LogWarning($"Store purchase could not start: {exception.Message}");
                return false;
            }
            return true;
#else
            return false;
#endif
        }

        public void RetryPurchases()
        {
#if OUTSIDE_THE_BOX_UNITY_IAP
            if (!connecting) InitializePurchasing();
            _ = DeliverPendingGemsAsync();
#endif
        }

        private void SetStatus(string status)
        {
            PurchaseStatus = status;
            Changed?.Invoke();
        }

        private void Configure(IEnumerable<string> productIds)
        {
            if (productIds != null)
            {
                foreach (var productId in productIds)
                    if (!string.IsNullOrWhiteSpace(productId)) configuredProductIds.Add(productId.Trim());
            }

            // Retain the shipped SKUs even when Remote Config hides a pack, so an
            // already-paid pending purchase can still be fulfilled.
            var gemCatalog = new List<GemPackConfig>(GemPackConfig.Defaults());
            gemCatalog.AddRange(LiveOpsConfigStore.Current.gemPacks ?? Array.Empty<GemPackConfig>());
            foreach (var pack in gemCatalog)
                if (pack != null && GemPurchaseLedger.TryGetPackAmount(pack.productId, out _))
                {
                    configuredProductIds.Add(pack.productId);
                    gemProductIds.Add(pack.productId);
                }

            if (initializationStarted || configuredProductIds.Count == 0) return;
            initializationStarted = true;

#if OUTSIDE_THE_BOX_UNITY_IAP
            InitializePurchasing();
#else
            Debug.LogWarning("Cosmetic IAP is disabled because the Unity Purchasing package is unavailable.");
#endif
        }

#if OUTSIDE_THE_BOX_UNITY_IAP
        private async void InitializePurchasing()
        {
            if (connecting) return;
            connecting = true;
            try
            {
                var catalog = new CatalogProvider();
                foreach (var productId in configuredProductIds)
                    catalog.AddProduct(productId, gemProductIds.Contains(productId) ? ProductType.Consumable : ProductType.NonConsumable);

                if (storeController == null)
                {
                    storeController = UnityIAPServices.StoreController();
                    storeController.OnStoreDisconnected += OnStoreDisconnected;
                    storeController.OnProductsFetched += OnProductsFetched;
                    storeController.OnProductsFetchFailed += OnProductsFetchFailed;
                    storeController.OnPurchasesFetched += OnPurchasesFetched;
                    storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                    storeController.OnPurchasePending += OnPurchasePending;
                    storeController.OnPurchaseDeferred += OnPurchaseDeferred;
                    storeController.OnPurchaseFailed += OnPurchaseFailed;
                    storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                }

                await storeController.Connect();
                storeController.ProcessPendingOrdersOnPurchasesFetched(true);
                catalog.FetchProducts(products => storeController.FetchProducts(products));
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("iap_init_failed");
                Debug.LogException(exception);
                SetStatus("STORE IS UNAVAILABLE. TAP RETRY.");
            }
            finally { connecting = false; }
        }

        private void OnProductsFetched(List<Product> products)
        {
            productsReady = products != null && products.Count > 0;
            availableProductIds.Clear();
            AnalyticsService.Log("iap_products_loaded", "product_count", products?.Count ?? 0);
            if (products != null)
            {
                foreach (var product in products)
                {
                    if (product?.definition == null || product.metadata == null) continue;
                    localizedPrices[product.definition.id] = product.metadata.localizedPriceString;
                    if (product.availableToPurchase) availableProductIds.Add(product.definition.id);
                }
            }
            if (!GemPurchaseInProgress) PurchaseStatus = string.Empty;
            storeController.FetchPurchases();
            Changed?.Invoke();
        }

        private static void TrackOrder(string eventName, Order order, string reason = "")
        {
            if (order?.CartOrdered == null) return;
            foreach (var item in order.CartOrdered.Items())
                AnalyticsService.Log(eventName, ("product_id", item.Product?.definition?.id ?? "unknown"),
                    ("reason", reason));
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed) OnPurchaseFailed(failed);
            else if (order is ConfirmedOrder)
            {
                TrackOrder("iap_confirmed", order);
                var key = PendingGemKey(order);
                if (key != null && pendingGems.Remove(key))
                {
                    confirmingGems.Remove(key);
                    pendingGemOwners.Remove(key);
                    creditedGems.TryGetValue(key, out var amount);
                    creditedGems.Remove(key);
                    SetStatus("PURCHASE COMPLETE");
                    GemsDelivered?.Invoke(amount);
                    _ = DeliverPendingGemsAsync();
                }
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            TrackOrder("iap_failed", order, order?.FailureReason.ToString() ?? "unknown");
            var key = PendingGemKey(order);
            if (key != null) confirmingGems.Remove(key);
            if (ContainsGem(order) || requestedGem != null)
            {
                requestedGem = null;
                requestedGemOwner = null;
                SetStatus(pendingGems.Count > 0 ? "PURCHASE SAVED. RETRY TO FINISH." : "PURCHASE NOT COMPLETED.");
            }
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            TrackOrder("iap_deferred", order);
            if (!ContainsGem(order)) return;
            requestedGem = null;
            requestedGemOwner = null;
            SetStatus("PAYMENT AWAITS APPROVAL. GEMS WILL ARRIVE AFTER PAYMENT.");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            // Operational event only: Firebase/Google Play owns automatic in_app_purchase revenue.
            // Never send receipts, order tokens or a second synthetic purchase/revenue event.
            if (order?.CartOrdered != null)
                foreach (var item in order.CartOrdered.Items())
                    AnalyticsService.Log("iap_pending", "product_id", item.Product?.definition?.id ?? "unknown");
            if (ContainsGem(order))
            {
                var transaction = order?.Info?.TransactionID;
                if (string.IsNullOrWhiteSpace(transaction))
                {
                    requestedGem = null;
                    requestedGemOwner = null;
                    SetStatus("PURCHASE DETAILS ARE STILL LOADING. TAP RETRY.");
                    return;
                }
                var key = GemPurchaseLedger.Key("transaction:" + transaction);
                pendingGems[key] = order;
                if (!pendingGemOwners.ContainsKey(key) && requestedGemOwner != null)
                    pendingGemOwners[key] = requestedGemOwner;
                requestedGem = null;
                requestedGemOwner = null;
                _ = DeliverPendingGemsAsync();
                Changed?.Invoke();
                return;
            }
            GrantOrder(order, false);
            storeController.ConfirmPurchase(order);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            if (orders?.ConfirmedOrders == null) return;
            foreach (var order in orders.ConfirmedOrders) GrantOrder(order, true);
            _ = DeliverPendingGemsAsync();
        }

        private void GrantOrder(Order order, bool restored)
        {
            if (order?.CartOrdered == null) return;
            foreach (var item in order.CartOrdered.Items())
            {
                var productId = item.Product?.definition?.id;
                if (!string.IsNullOrWhiteSpace(productId) && configuredProductIds.Contains(productId) && !gemProductIds.Contains(productId))
                {
                    ownedProductIds.Add(productId);
                    if (LiveOpsManager.Instance?.Players?.CloudProgressReady == true)
                        entitlementHandler?.Invoke(productId, restored);
                    if (restored) AnalyticsService.Log("iap_entitlement_restored", "product_id", productId);
                }
            }
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            productsReady = false;
            availableProductIds.Clear();
            // Keep paid orders and their wallet markers for a safe retry after reconnect.
            confirmingGems.Clear();
            AnalyticsService.Log("iap_store_disconnected");
            Debug.LogWarning($"Unity IAP store disconnected: {failure}");
            SetStatus("STORE DISCONNECTED. TAP RETRY.");
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            AnalyticsService.Log("iap_products_failed");
            Debug.LogWarning($"Unity IAP product fetch failed: {failure}");
            if (!GemPurchaseInProgress) SetStatus("SOME PRODUCTS ARE UNAVAILABLE. TAP RETRY.");
            else Changed?.Invoke();
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            AnalyticsService.Log("iap_restore_failed");
            Debug.LogWarning($"Unity IAP purchase restore failed: {failure}");
            SetStatus("PURCHASE SYNC IS UNAVAILABLE. TAP RETRY.");
        }

        private bool ContainsGem(Order order)
        {
            if (order?.CartOrdered == null) return false;
            foreach (var item in order.CartOrdered.Items())
                if (gemProductIds.Contains(item.Product?.definition?.id ?? string.Empty)) return true;
            return false;
        }

        private string PendingGemKey(Order order)
        {
            var transaction = order?.Info?.TransactionID;
            if (!string.IsNullOrWhiteSpace(transaction))
            {
                var key = GemPurchaseLedger.Key("transaction:" + transaction);
                if (pendingGems.ContainsKey(key)) return key;
            }
            // Some stores omit the transaction ID on consumable confirmation. Only one
            // confirmation is in flight, so match that order by its product instead.
            if (order?.CartOrdered != null)
                foreach (var item in order.CartOrdered.Items())
                    foreach (var key in confirmingGems)
                        foreach (var pendingItem in pendingGems[key].CartOrdered.Items())
                            if (item.Product?.definition?.id == pendingItem.Product?.definition?.id) return key;
            return null;
        }

        private async Task DeliverPendingGemsAsync()
        {
            var players = LiveOpsManager.Instance?.Players;
            if (!productsReady || deliveringGems || pendingGems.Count == 0 || confirmingGems.Count > 0 ||
                players?.CloudProgressReady != true || !LiveOpsConfigStore.Current.cloudProgressEnabled || SaveService.Data == null) return;
            deliveringGems = true;
            nextGemRetry = Time.unscaledTime + 15f;
            try
            {
                foreach (var pair in new List<KeyValuePair<string, PendingOrder>>(pendingGems))
                {
                    if (confirmingGems.Count > 0 || players.CloudProgressReady != true) break;
                    var owner = players.PlayerId;
                    var profile = SaveService.Data;
                    var ownerKey = GemPurchaseLedger.Key("account:" + owner);
                    var storeOwner = pair.Value.Info.Google?.ObfuscatedAccountId;
                    if ((!string.IsNullOrEmpty(storeOwner) && storeOwner != ownerKey) ||
                        (pendingGemOwners.TryGetValue(pair.Key, out var knownOwner) && knownOwner != ownerKey))
                    {
                        SetStatus("SIGN IN TO THE ACCOUNT THAT MADE THIS PURCHASE.");
                        continue;
                    }
                    pendingGemOwners[pair.Key] = ownerKey;
                    var amount = 0;
                    foreach (var item in pair.Value.CartOrdered.Items())
                    {
                        if (!gemProductIds.Contains(item.Product?.definition?.id ?? string.Empty) ||
                            !GemPurchaseLedger.TryGetPackAmount(item.Product.definition.id, out var packAmount) || item.Quantity < 1)
                            throw new InvalidOperationException("Unexpected gem purchase contents.");
                        amount = checked(amount + checked(packAmount * item.Quantity));
                    }
                    profile.processedGemTransactions ??= new List<string>();
                    var granted = GemPurchaseLedger.Grant(profile.processedGemTransactions, pair.Key, amount,
                        ref profile.softCurrency, ref profile.lifetimeCurrencyEarned);
                    if (granted)
                    {
                        creditedGems[pair.Key] = amount;
                        SaveService.SaveDeferred();
                        AnalyticsService.Log("earn_virtual_currency", ("value", amount), ("virtual_currency_name", "gems"),
                            ("source", "iap_gem_pack"), ("balance", profile.softCurrency));
                    }
                    SetStatus("SAVING YOUR GEMS");
                    await players.SaveProgressNowAsync(SaveService.ToJson());
                    if (!LiveOpsConfigStore.Current.cloudProgressEnabled || players.PlayerId != owner ||
                        !ReferenceEquals(profile, SaveService.Data)) throw new InvalidOperationException("Player changed during purchase delivery.");
                    confirmingGems.Add(pair.Key);
                    SetStatus("FINISHING PURCHASE");
                    try { storeController.ConfirmPurchase(pair.Value); }
                    catch { confirmingGems.Remove(pair.Key); throw; }
                }
            }
            catch (Exception exception)
            {
                // Wallet and transaction marker stay together in memory and cloud snapshots.
                // Leave the store order unconfirmed; retry never grants this marker twice.
                AnalyticsService.Log("iap_delivery_waiting", "reason", "cloud_or_confirmation_unavailable");
                Debug.LogWarning($"Gem purchase will retry: {exception.Message}");
                SetStatus("RECONNECT OR TAP RETRY TO FINISH YOUR PURCHASE.");
            }
            finally { deliveringGems = false; Changed?.Invoke(); }
        }

        private void Update()
        {
            if (pendingGems.Count > 0 && Time.unscaledTime >= nextGemRetry)
                _ = DeliverPendingGemsAsync();
        }

        private void OnDestroy()
        {
            if (storeController != null)
            {
                storeController.OnStoreDisconnected -= OnStoreDisconnected;
                storeController.OnProductsFetched -= OnProductsFetched;
                storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
                storeController.OnPurchasesFetched -= OnPurchasesFetched;
                storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
                storeController.OnPurchasePending -= OnPurchasePending;
                storeController.OnPurchaseDeferred -= OnPurchaseDeferred;
                storeController.OnPurchaseFailed -= OnPurchaseFailed;
                storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            }
            if (Instance == this) Instance = null;
        }
#endif
    }
}

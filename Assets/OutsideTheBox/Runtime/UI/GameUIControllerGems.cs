using System;
using TMPro;
using UnityEngine;

namespace OutsideTheBox
{
    public sealed partial class GameUIController
    {
        private GemStoreView gemStoreView;
        private CosmeticIapService gemStoreIap;
        private bool showingGemPacks;

        private void EnsureMainMenuGemBalance()
        {
            if (mainMenuPanel == null) return;
            var root = mainMenuPanel.transform;
            var wallet = root.Find("Gem Balance");
            if (wallet == null)
                wallet = prefabs.Spawn(prefabs.gemBalanceButtonPrefab, "Gem Balance", root).transform;
            var balance = wallet.Find("Balance")?.GetComponent<TMP_Text>();
            var button = wallet.GetComponent<UnityEngine.UI.Button>();
            if (balance == null || button == null)
                throw new InvalidOperationException("GemBalanceButton prefab bindings are incomplete.");

            // Old HUD instances can still contain the original, unclickable balance.
            // Keep those objects intact while the authored wallet replaces their display.
            var oldBalance = root.Find("Currency");
            if (oldBalance != null) oldBalance.gameObject.SetActive(false);
            var oldChip = root.Find("Currency Chip");
            if (oldChip != null) oldChip.gameObject.SetActive(false);
            if (!currencyTexts.Contains(balance)) currencyTexts.Add(balance);
            button.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(button);
            button.onClick.AddListener(OpenGemStoreFromBalance);
        }

        private void OpenGemStoreFromBalance()
        {
            EnsureStoreUI();
            showingGemPacks = true;
            AnalyticsService.Log("store_opened", ("source", "gem_balance"), ("category", "Gems"));
            ShowStore();
        }

        private void EnsureGemStoreUI()
        {
            if (gemStoreView != null || storePanel == null) return;
            var authored = storePanel.transform.Find("Gem Store");
            if (authored == null)
                authored = prefabs.Spawn(prefabs.gemStorePanelPrefab, "Gem Store", storePanel.transform).transform;
            gemStoreView = authored.GetComponent<GemStoreView>();
            if (gemStoreView == null || gemStoreView.openButton == null || gemStoreView.content == null ||
                gemStoreView.status == null || gemStoreView.retryButton == null || gemStoreView.cards == null || gemStoreView.cards.Length != 6)
                throw new InvalidOperationException("GemStorePanel prefab bindings are incomplete.");
            var open = gemStoreView.openButton;
            open.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(open);
            open.onClick.AddListener(() =>
            {
                showingGemPacks = !showingGemPacks;
                AnalyticsService.Log("store_category_selected", "category", showingGemPacks ? "Gems" : selectedStoreCategory.ToString());
                RefreshStore();
                if (showingGemPacks) gemStoreView.Reveal();
                else RevealStoreItems();
            });
            gemStoreView.retryButton.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(gemStoreView.retryButton);
            gemStoreView.retryButton.onClick.AddListener(() => gemStoreIap?.RetryPurchases());
            for (var i = 0; i < gemStoreView.cards.Length; i++)
            {
                var index = i;
                var card = gemStoreView.cards[i];
                if (card == null || card.purchaseButton == null || card.packName == null || card.amount == null || card.price == null)
                    throw new InvalidOperationException("Gem pack card prefab bindings are incomplete.");
                card.purchaseButton.onClick.RemoveAllListeners();
                AnalyticsButtonTracker.Bind(card.purchaseButton);
                card.purchaseButton.onClick.AddListener(() => BuyGemPack(index));
            }
            gemStoreIap = CosmeticIapService.Instance;
            if (gemStoreIap != null)
            {
                gemStoreIap.Changed += RefreshGemStorePurchaseState;
                gemStoreIap.GemsDelivered += HandleGemsDelivered;
            }
            gemStoreView.content.gameObject.SetActive(false);
        }

        private bool ApplyGemStoreVisibility()
        {
            EnsureGemStoreUI();
            var live = LiveOpsConfigStore.Current;
            var enabled = live.gemPurchasesEnabled && live.gemPacks?.Length > 0;
            if (!enabled && !(gemStoreIap?.GemPurchaseInProgress ?? false)) showingGemPacks = false;
            gemStoreView.openButton.gameObject.SetActive(enabled || showingGemPacks);
            SetButtonFill(gemStoreView.openButton, showingGemPacks ? session.Config.uiPrimary : session.Config.uiButton);
            gemStoreView.openButton.GetComponentInChildren<TMP_Text>(true).text = showingGemPacks ? "GEMS SELECTED" : "BUY GEMS";
            gemStoreView.content.gameObject.SetActive(showingGemPacks);
            foreach (var filter in storeRarityButtons) filter.gameObject.SetActive(!showingGemPacks);
            if (storeStatusText != null) storeStatusText.gameObject.SetActive(!showingGemPacks);
            if (showingGemPacks)
            {
                foreach (var item in storeItemButtons) item.gameObject.SetActive(false);
                if (chapterGiftBrowse != null) chapterGiftBrowse.gameObject.SetActive(false);
                if (chapterGiftClaim != null) chapterGiftClaim.gameObject.SetActive(false);
                if (storeRevealRoutine != null) { StopCoroutine(storeRevealRoutine); storeRevealRoutine = null; }
                RefreshGemPackCards();
            }
            return showingGemPacks;
        }

        private void RefreshGemPackCards()
        {
            var packs = LiveOpsConfigStore.Current.gemPacks ?? Array.Empty<GemPackConfig>();
            for (var i = 0; i < gemStoreView.cards.Length; i++)
            {
                var card = gemStoreView.cards[i];
                card.gameObject.SetActive(i < packs.Length);
                if (i >= packs.Length) continue;
                var pack = packs[i];
                card.packName.text = pack.name;
                card.amount.text = $"{pack.gems:N0} GEMS";
                var localized = Application.isEditor ? string.Empty : gemStoreIap?.GetLocalizedPrice(pack.productId);
                card.price.text = string.IsNullOrWhiteSpace(localized) ? pack.fallbackPrice : localized;
                card.purchaseButton.interactable = !Application.isEditor && gemStoreIap?.IsProductAvailable(pack.productId) == true &&
                    !gemStoreIap.GemPurchaseInProgress && LiveOpsManager.Instance?.Players?.CloudProgressReady == true &&
                    LiveOpsConfigStore.Current.cloudProgressEnabled && LiveOpsConfigStore.Current.gemPurchasesEnabled;
            }
            var anyAvailable = Array.Exists(packs, pack => gemStoreIap?.IsProductAvailable(pack.productId) == true);
            gemStoreView.status.text = Application.isEditor ? "PURCHASES ARE AVAILABLE IN THE DEVICE BUILD" :
                !string.IsNullOrEmpty(gemStoreIap?.PurchaseStatus) ? gemStoreIap.PurchaseStatus :
                LiveOpsManager.Instance?.Players?.CloudProgressReady != true ? "SIGN IN AND SYNC YOUR ACCOUNT TO BUY GEMS" :
                gemStoreIap?.ProductsReady != true ? "CONNECTING TO GOOGLE PLAY" :
                !anyAvailable ? "GEM PACKS ARE UNAVAILABLE. TAP RETRY." : "GEMS FOR SKINS AND REVIVES";
        }

        private void BuyGemPack(int index)
        {
            var packs = LiveOpsConfigStore.Current.gemPacks;
            if (packs == null || index < 0 || index >= packs.Length) return;
            var pack = packs[index];
            AnalyticsService.Log("select_item", ("item_id", pack.productId), ("category", "Gems"), ("gems", pack.gems));
            if (gemStoreIap == null || !gemStoreIap.Purchase(pack.productId))
                ShowToast("CONNECT TO GOOGLE PLAY AND RETRY.");
            RefreshGemPackCards();
        }

        private void RefreshGemStorePurchaseState()
        {
            if (this == null || gemStoreView == null) return;
            RefreshCurrency();
            if (storeBalanceText != null) storeBalanceText.text = $"GEMS  {EconomyService.Balance}";
            if (showingGemPacks) RefreshGemPackCards();
        }

        private void HandleGemsDelivered(int amount)
        {
            RefreshGemStorePurchaseState();
            ShowToast(amount > 0 ? $"{amount:N0} GEMS ADDED" : "PURCHASE RESTORED. BALANCE SYNCED.");
        }

        private void ReleaseGemStoreBindings()
        {
            if (gemStoreIap == null) return;
            gemStoreIap.Changed -= RefreshGemStorePurchaseState;
            gemStoreIap.GemsDelivered -= HandleGemsDelivered;
        }
    }
}

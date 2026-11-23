using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OutsideTheBox
{
    public static class CosmeticArtwork
    {
        private static readonly Dictionary<string, Sprite> Cache = new();
        private static readonly Dictionary<string, Texture2D> Atlases = new();
        // Measured on the original 1619 x 971 artwork, with a top-left origin.
        // The generated atlas has different spacing in each row; equal-width cells
        // include the neighbouring skin and clip the intended one.
        private const float SourceWidth = 1619f;
        private const float SourceHeight = 971f;
        private static readonly int[] RowTop = { 0, 176, 335, 532, 635, 803 };
        private static readonly int[] RowBottom = { 173, 330, 504, 603, 800, 962 };
        private static readonly int[][] ColumnEdges =
        {
            new[] { 12, 179, 341, 499, 656, 813, 969, 1130, 1287, 1448, 1619 },
            new[] { 9, 179, 341, 497, 655, 820, 976, 1130, 1291, 1451, 1619 },
            new[] { 8, 177, 336, 495, 653, 811, 970, 1127, 1288, 1446, 1619 },
            new[] { 6, 173, 332, 492, 650, 812, 971, 1129, 1291, 1447, 1619 },
            new[] { 5, 172, 329, 493, 651, 815, 965, 1128, 1293, 1448, 1619 },
            new[] { 6, 169, 331, 491, 650, 810, 970, 1129, 1290, 1450, 1619 }
        };

        // Visible glass-frame bounds in the source atlas, top-left origin.
        // The preview cells include unequal transparent gutters (especially the
        // classic frame's left and bottom); gameplay needs a centred frame.
        private static readonly int[,] BoardGlassBounds =
        {
            { 21, 164, 639, 783 }, { 180, 326, 638, 784 },
            { 340, 486, 638, 784 }, { 496, 648, 636, 788 },
            { 657, 805, 638, 784 }, { 817, 962, 638, 784 },
            { 976, 1123, 637, 784 }, { 1136, 1284, 638, 784 },
            { 1295, 1443, 637, 783 }, { 1457, 1605, 636, 792 }
        };

        public static Sprite GetBoardGlass(int variant)
        {
            var preview = Get(4, variant);
            if (preview == null) return null;
            if (variant >= 10) variant = 0;
            var texture = preview.texture;
            var key = $"board-glass1:{texture.GetInstanceID()}:{texture.width}:{texture.height}:{variant}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var xScale = texture.width / SourceWidth;
            var yScale = texture.height / SourceHeight;
            var left = Mathf.RoundToInt(BoardGlassBounds[variant, 0] * xScale);
            var right = Mathf.RoundToInt((BoardGlassBounds[variant, 1] + 1) * xScale);
            var bottom = Mathf.RoundToInt((SourceHeight - BoardGlassBounds[variant, 3] - 1) * yScale);
            var top = Mathf.RoundToInt((SourceHeight - BoardGlassBounds[variant, 2]) * yScale);
            var allowed = preview.rect;
            var padding = Mathf.Max(0f, Mathf.Min(3f * Mathf.Min(xScale, yScale),
                Mathf.Min(Mathf.Min(left - allowed.xMin, allowed.xMax - right),
                    Mathf.Min(bottom - allowed.yMin, allowed.yMax - top))));
            var rect = Rect.MinMaxRect(left - padding, bottom - padding, right + padding, top + padding);
            // Keep the entire bevel inside the fixed border rather than stretching
            // part of the left/bottom bevel through the centre of the board.
            var border = Vector4.one * (Mathf.Min(rect.width, rect.height) * 0.24f);
            var sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f,
                Mathf.Max(rect.width, rect.height), 0, SpriteMeshType.FullRect, border);
            sprite.name = $"Board Glass {variant}";
            Cache[key] = sprite;
            return sprite;
        }

        public static Rect GetAtlasRect(int category, int variant, int textureWidth, int textureHeight)
        {
            if (category < 0 || category >= 6 || variant < 0 || variant >= 10 || textureWidth <= 0 || textureHeight <= 0)
                return Rect.zero;
            // Insets keep filtering inside the measured cell, clear of adjacent artwork.
            var left = ColumnEdges[category][variant] + 2f;
            var right = ColumnEdges[category][variant + 1] - 2f;
            var top = RowTop[category] + 1f;
            var bottom = RowBottom[category] - 1f;
            var x0 = Mathf.RoundToInt(left / SourceWidth * textureWidth);
            var x1 = Mathf.RoundToInt(right / SourceWidth * textureWidth);
            var y0 = Mathf.RoundToInt((SourceHeight - bottom) / SourceHeight * textureHeight);
            var y1 = Mathf.RoundToInt((SourceHeight - top) / SourceHeight * textureHeight);
            return new Rect(x0, y0, Mathf.Max(1, x1 - x0), Mathf.Max(1, y1 - y0));
        }

        public static Sprite Get(int category, int variant)
        {
            if (variant >= 10 && ChapterGiftService.OwnsGift(category, variant)) variant = 0;
            if (category < 0 || category >= 6 || variant < 0 || variant >= 10) return null;
            var path = LiveOpsConfigStore.Current.cosmeticArtworkResource;
            if (!Atlases.TryGetValue(path, out var atlas) || atlas == null)
            {
                atlas = Resources.Load<Texture2D>(path);
                if (atlas == null) return null;
                Atlases[path] = atlas;
            }
            // A texture reimport can change its resolution while domain reload is disabled.
            var key = $"{path}:crop3:{atlas.GetInstanceID()}:{atlas.width}:{atlas.height}:{category}:{variant}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var rect = GetAtlasRect(category, variant, atlas.width, atlas.height);
            var border = category == 4 ? new Vector4(rect.width, rect.height, rect.width, rect.height) * 0.12f : Vector4.zero;
            // Wall artwork is horizontal. Give it real slice borders so the
            // renderer can size it to the collision footprint without collapsing
            // its thickness to the atlas cell's wide aspect ratio.
            if (category == 3) border = Vector4.one * rect.height * 0.2f;
            var sprite = Sprite.Create(atlas, rect, Vector2.one * 0.5f, Mathf.Max(rect.width, rect.height), 0,
                SpriteMeshType.FullRect, border);
            sprite.name = $"Cosmetic {category} {variant}";
            Cache[key] = sprite;
            return sprite;
        }
    }

    public sealed partial class GameUIController
    {
        private enum CosmeticCategory
        {
            Ball,
            Magnet,
            Goal,
            Walls,
            Glass,
            Background
        }

        private static readonly string[] CosmeticNames =
        {
            "CLASSIC", "ARCTIC", "SOLAR", "NEBULA", "RUBY",
            "EMERALD", "MIDNIGHT", "EMBER", "COSMIC", "AURORA"
        };

        private sealed class StorePreviewVisual
        {
            public Image backdrop;
            public Image artwork;
            public Image accentPrimary;
            public Image accentSecondary;
        }

        private static readonly Dictionary<string, Sprite> StorePreviewSpriteCache = new(StringComparer.Ordinal);

        private GameObject storePanel;
        private TMP_Text storeBalanceText;
        private TMP_Text storeStatusText;
        private readonly List<Button> storeItemButtons = new();
        private readonly List<StorePreviewVisual> storeItemPreviews = new();
        private readonly List<Button> storeCategoryButtons = new();
        private CosmeticCategory selectedStoreCategory;
        private int selectedStoreRarity = -1;
        private readonly List<Button> storeRarityButtons = new();
        private Coroutine storeRevealRoutine;
        private Button chapterGiftBrowse;
        private Button chapterGiftClaim;
        private Image chapterGiftPreview;
        private int chapterGiftIndex;
        private readonly List<Vector2> storeCardAnchorMins = new();
        private readonly List<Vector2> storeCardAnchorMaxs = new();
        private static readonly string[] RarityNames = { "NORMAL", "RARE", "EPIC", "LEGENDARY" };

        private static int GetCosmeticRarity(int index)
        {
            var values = LiveOpsConfigStore.Current.cosmeticRarities;
            return values != null && index < values.Length ? Mathf.Clamp(values[index], 0, 3) : 0;
        }

        private static Color GetRarityColor(int rarity)
        {
            var colors = LiveOpsConfigStore.Current.cosmeticRarityColors;
            return colors != null && rarity < colors.Length && ColorUtility.TryParseHtmlString(colors[rarity], out var color)
                ? color : Color.white;
        }

        private void EnsureStoreUI()
        {
            EnsureAccountAndDeleteControls();
            EnsureMainMenuStoreButton();
            if (storePanel != null) return;

            var authoredStore = safeRoot.Find("Store Screen");
            if (authoredStore == null && prefabs.storePanelPrefab != null)
                authoredStore = prefabs.Spawn(prefabs.storePanelPrefab, "Store Screen", safeRoot).transform;
            if (authoredStore != null)
            {
                if (BindStorePrefab(authoredStore)) return;
                if (Application.isPlaying)
                    throw new System.InvalidOperationException("StorePanel prefab bindings are incomplete. Repair the authored prefab in the Inspector.");
                Debug.LogWarning("Store prefab is incomplete; using the standard UI pieces to build the store.");
                authoredStore.gameObject.SetActive(false);
                authoredStore.name = "Incomplete Store Screen";
            }

            if (Application.isPlaying)
                throw new System.InvalidOperationException("Assign the StorePanel prefab in the Runtime Prefab Registry.");

            storePanel = CreatePanel("Store Screen", safeRoot, session.Config.uiBackgroundBottom);
            screens.Add(storePanel);
            ThemeScreen(storePanel, true);
            EnsureBackdrop(storePanel, false);
            var root = storePanel.GetComponent<RectTransform>();

            var title = CreateText("Title", root, 48, TextAlignmentOptions.Center, session.Config.uiTextPrimary);
            title.fontStyle = FontStyles.Bold;
            title.text = "COSMETIC STORE";
            SetRect(title.rectTransform, new Vector2(0.17f, 0.90f), new Vector2(0.83f, 0.96f));

            var back = CreateButton("Back", root, "‹", ShowMainMenu);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.03f, 0.90f), new Vector2(0.15f, 0.965f));
            StyleButton(back);

            storeBalanceText = CreateText("Balance", root, 22, TextAlignmentOptions.Right, session.Config.uiTextPrimary);
            storeBalanceText.fontStyle = FontStyles.Bold;
            SetRect(storeBalanceText.rectTransform, new Vector2(0.58f, 0.855f), new Vector2(0.94f, 0.895f));

            var categories = new[] { "BALL", "MAGNET", "HOLE", "WALLS", "GLASS", "BACKGROUND" };
            for (var i = 0; i < categories.Length; i++)
            {
                var captured = (CosmeticCategory)i;
                var button = CreateButton($"Category {categories[i]}", root, categories[i],
                    () => SelectStoreCategory(captured));
                var column = i % 3;
                var row = i / 3;
                var x0 = 0.055f + column * 0.305f;
                var y1 = 0.845f - row * 0.064f;
                SetRect(button.GetComponent<RectTransform>(), new Vector2(x0, y1 - 0.052f),
                    new Vector2(x0 + 0.28f, y1));
                button.GetComponentInChildren<TMP_Text>(true).fontSize = 18;
                StyleButton(button);
                storeCategoryButtons.Add(button);
            }

            storeStatusText = CreateText("Status", root, 20, TextAlignmentOptions.Center, session.Config.uiTextMuted);
            storeStatusText.fontStyle = FontStyles.Bold;
            SetRect(storeStatusText.rectTransform, new Vector2(0.05f, 0.676f), new Vector2(0.95f, 0.722f));

            for (var i = 0; i < 10; i++)
            {
                var captured = i;
                var button = CreateButton($"Cosmetic {i + 1:00}", root, CosmeticNames[i],
                    () => SelectOrBuyCosmetic(captured));
                PositionStoreCard(button.GetComponent<RectTransform>(), i);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.fontSize = 17;
                label.alignment = TextAlignmentOptions.Left;
                SetRect(label.rectTransform, new Vector2(0.32f, 0.08f), new Vector2(0.97f, 0.92f));

                var preview = CreateRect("Skin Preview", button.transform);
                SetRect(preview, new Vector2(0.025f, 0.10f), new Vector2(0.295f, 0.90f));
                var previewBackdrop = preview.gameObject.AddComponent<Image>();
                previewBackdrop.sprite = RoundedPanelSprite;
                previewBackdrop.type = Image.Type.Sliced;
                previewBackdrop.raycastTarget = false;

                var artwork = CreateStorePreviewImage("Artwork", preview, Vector2.zero, Vector2.one);
                artwork.preserveAspect = true;
                var accentPrimary = CreateStorePreviewImage("Accent Primary", preview,
                    new Vector2(0.08f, 0.18f), new Vector2(0.82f, 0.40f));
                var accentSecondary = CreateStorePreviewImage("Accent Secondary", preview,
                    new Vector2(0.08f, 0.18f), new Vector2(0.28f, 0.82f));
                storeItemPreviews.Add(new StorePreviewVisual
                {
                    backdrop = previewBackdrop,
                    artwork = artwork,
                    accentPrimary = accentPrimary,
                    accentSecondary = accentSecondary
                });
                StyleButton(button);
                storeItemButtons.Add(button);
            }

            selectedStoreCategory = CosmeticCategory.Ball;
            CosmeticIapService.Ensure(gameObject, GetAllConfiguredCosmeticProducts(), HandleCosmeticPurchase);
            EnsureGemStoreUI();
            RefreshStore();
        }

        private bool BindStorePrefab(Transform root)
        {
            var names = new[] { "BALL", "MAGNET", "HOLE", "WALLS", "GLASS", "BACKGROUND" };
            var balance = root.Find("Balance")?.GetComponent<TMP_Text>();
            var status = root.Find("Status")?.GetComponent<TMP_Text>();
            var back = root.Find("Back")?.GetComponent<Button>();
            if (balance == null || status == null || back == null) return false;
            var categories = new List<Button>();
            var items = new List<Button>();
            var visuals = new List<StorePreviewVisual>();
            foreach (var name in names)
            {
                var button = root.Find($"Category {name}")?.GetComponent<Button>();
                if (button == null) return false;
                categories.Add(button);
            }
            for (var i = 0; i < 10; i++)
            {
                var item = root.Find($"Cosmetic {i + 1:00}");
                var button = item != null ? item.GetComponent<Button>() : null;
                var preview = item != null ? item.Find("Skin Preview") : null;
                if (button == null || preview == null) return false;
                var visual = new StorePreviewVisual
                {
                    backdrop = preview.GetComponent<Image>(),
                    artwork = preview.Find("Artwork")?.GetComponent<Image>(),
                    accentPrimary = preview.Find("Accent Primary")?.GetComponent<Image>(),
                    accentSecondary = preview.Find("Accent Secondary")?.GetComponent<Image>()
                };
                if (visual.backdrop == null || visual.artwork == null || visual.accentPrimary == null || visual.accentSecondary == null) return false;
                items.Add(button);
                visuals.Add(visual);
            }
            storePanel = root.gameObject;
            if (!screens.Contains(storePanel)) screens.Add(storePanel);
            storeBalanceText = balance;
            storeStatusText = status;
            storeCategoryButtons.Clear(); storeCategoryButtons.AddRange(categories);
            storeItemButtons.Clear(); storeItemButtons.AddRange(items);
            storeItemPreviews.Clear(); storeItemPreviews.AddRange(visuals);
            back.onClick.RemoveAllListeners(); AnalyticsButtonTracker.Bind(back); back.onClick.AddListener(ShowMainMenu);
            for (var i = 0; i < categories.Count; i++)
            {
                var category = (CosmeticCategory)i;
                categories[i].onClick.RemoveAllListeners();
                AnalyticsButtonTracker.Bind(categories[i]);
                categories[i].onClick.AddListener(() => SelectStoreCategory(category));
            }
            for (var i = 0; i < items.Count; i++)
            {
                var index = i;
                items[i].onClick.RemoveAllListeners();
                AnalyticsButtonTracker.Bind(items[i]);
                items[i].onClick.AddListener(() => SelectOrBuyCosmetic(index));
            }
            selectedStoreCategory = CosmeticCategory.Ball;
            CosmeticIapService.Ensure(gameObject, GetAllConfiguredCosmeticProducts(), HandleCosmeticPurchase);
            EnsureGemStoreUI();
            RefreshStore();
            return true;
        }

        private static void PositionStoreCard(RectTransform rect, int index)
        {
            var column = index % 2;
            var row = index / 2;
            var x0 = column == 0 ? 0.06f : 0.52f;
            var y1 = 0.65f - row * 0.105f;
            SetRect(rect, new Vector2(x0, y1 - 0.088f), new Vector2(x0 + 0.42f, y1));
        }

        private void EnsureMainMenuStoreButton()
        {
            if (mainMenuPanel == null) return;
            EnsureMainMenuGemBalance();
            var oldGoogle = mainMenuPanel.transform.Find("Google Play");
            if (oldGoogle != null) oldGoogle.gameObject.SetActive(false);
            var button = mainMenuPanel.transform.Find("Store")?.GetComponent<Button>();
            if (button == null)
            {
                button = CreateButton("Store", mainMenuPanel.transform, "STORE", ShowStore);
                button.GetComponentInChildren<TMP_Text>(true).fontSize = 22;
                StyleButton(button);
            }
            button.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(button);
            button.onClick.AddListener(ShowStore);
            var storeLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (storeLabel != null) storeLabel.text = ChapterGiftService.PendingCount > 0
                ? $"STORE  ({ChapterGiftService.PendingCount} FREE)" : "STORE";
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.51f, 0.045f), new Vector2(0.92f, 0.13f));
            var account = mainMenuPanel.transform.Find("Account") as RectTransform;
            if (account != null) SetRect(account, new Vector2(0.08f, 0.045f), new Vector2(0.49f, 0.13f));
        }

        private void EnsureAccountAndDeleteControls()
        {
            if (accountPanel != null)
            {
                accountGoogleButton ??= accountPanel.transform.Find("Google Play")?.GetComponent<Button>();
                accountLogoutButton ??= accountPanel.transform.Find("Log Out")?.GetComponent<Button>();
                if (accountGoogleButton == null)
                {
                    accountGoogleButton = CreateButton("Google Play", accountPanel.transform,
                        "CONNECT / SWITCH GOOGLE PLAY", () => session.SignInGooglePlay());
                    SetRect(accountGoogleButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.21f),
                        new Vector2(0.87f, 0.29f));
                    StyleButton(accountGoogleButton);
                }
                if (accountLogoutButton == null)
                {
                    accountLogoutButton = CreateButton("Log Out", accountPanel.transform,
                        "LOG OUT / SWITCH ACCOUNT", () => session.SignOutPlayer());
                    SetRect(accountLogoutButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.115f),
                        new Vector2(0.87f, 0.195f));
                    StyleButton(accountLogoutButton);
                }
            }

            if (settingsPanel == null) return;
            deleteAccountButton ??= settingsPanel.transform.Find("Delete Account")?.GetComponent<Button>();
            if (deleteAccountButton != null) return;
            deleteAccountButton = CreateButton("Delete Account", settingsPanel.transform, "DELETE ACCOUNT",
                ConfirmDeleteAccount);
            SetRect(deleteAccountButton.GetComponent<RectTransform>(), new Vector2(0.20f, 0.115f),
                new Vector2(0.80f, 0.175f));
            SetButtonFill(deleteAccountButton, session.Config.uiDanger);
            StyleButton(deleteAccountButton);
        }

        private void ShowStore()
        {
            ChapterGiftService.EnsureRewards(session.Config);
            var pending = SaveService.Data?.chapterGifts?.FindIndex(g => g != null && !g.claimed) ?? -1;
            if (pending >= 0) chapterGiftIndex = pending;
            RefreshStore();
            TransitionToScreen(storePanel);
            if (showingGemPacks) gemStoreView.Reveal();
            else RevealStoreItems();
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        private void SelectStoreCategory(CosmeticCategory category)
        {
            showingGemPacks = false;
            AnalyticsService.Log("store_category_selected", "category", category.ToString());
            selectedStoreCategory = category;
            selectedStoreRarity = -1;
            RefreshStore();
            RevealStoreItems();
        }

        private void RefreshStore()
        {
            if (storePanel == null || SaveService.Data == null) return;
            ChapterGiftService.EnsureRewards(session.Config);
            EnsureStoreRarityFilters();
            RefreshChapterGiftControls();
            if (storeBalanceText != null) storeBalanceText.text = $"GEMS  {EconomyService.Balance}";
            for (var i = 0; i < storeCategoryButtons.Count; i++)
            {
                SetButtonFill(storeCategoryButtons[i], !showingGemPacks && i == (int)selectedStoreCategory
                    ? session.Config.uiPrimary : session.Config.uiButton);
                var frame = storeCategoryButtons[i].transform.Find("Button Frame")?.GetComponent<UIRoundRectGraphic>();
                if (frame != null)
                    frame.Configure(!showingGemPacks && i == (int)selectedStoreCategory ? session.Config.uiPrimary : session.Config.uiButton,
                        !showingGemPacks && i == (int)selectedStoreCategory ? Color.white : session.Config.uiTextMuted,
                        !showingGemPacks && i == (int)selectedStoreCategory ? 3f : 1f, 18f);
            }

            if (ApplyGemStoreVisibility()) return;

            var tints = GetStoreTints(selectedStoreCategory);
            for (var i = 0; i < storeItemButtons.Count; i++)
            {
                var button = storeItemButtons[i];
                var rarity = GetCosmeticRarity(i);
                var visible = i < tints.Length && (selectedStoreRarity < 0 || selectedStoreRarity == rarity);
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                var cardRect = button.GetComponent<RectTransform>();
                if (selectedStoreRarity < 0)
                {
                    cardRect.anchorMin = storeCardAnchorMins[i];
                    cardRect.anchorMax = storeCardAnchorMaxs[i];
                }
                else PositionStoreCard(cardRect, storeItemButtons.GetRange(0, i).FindAll(b => b.gameObject.activeSelf).Count);
                var owned = IsCosmeticOwned(selectedStoreCategory, i);
                var equipped = GetEquippedCosmetic(selectedStoreCategory) == i;
                var suffix = equipped ? "\nEQUIPPED" : owned ? "\nOWNED" : $"\n{GetStorePriceLabel(selectedStoreCategory, i)}";
                var label = button.GetComponentInChildren<TMP_Text>(true);
                var rarityColor = GetRarityColor(rarity);
                if (label != null)
                {
                    label.richText = true;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 14;
                    label.fontSizeMax = 24;
                    label.text = $"<size=65%><color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{RarityNames[rarity]}</color></size>\n{CosmeticNames[i]}" + suffix;
                }
                var fill = Color.Lerp(new Color(0.035f, 0.09f, 0.18f), rarityColor, equipped ? 0.42f : 0.14f);
                SetButtonFill(button, fill);
                var frame = button.transform.Find("Button Frame")?.GetComponent<UIRoundRectGraphic>();
                if (frame != null)
                {
                    frame.Configure(fill, equipped ? Color.white : rarityColor, equipped ? 4f : 1.4f, 18f);
                    frame.ConfigureGradient(fill * new Color(0.8f, 0.8f, 0.8f, 1f),
                        Color.Lerp(fill, rarityColor, equipped ? 0.25f : 0.08f), Color.white, equipped ? 0.14f : 0.025f);
                }
                button.interactable = true;

                var uniqueArtwork = CosmeticArtwork.Get((int)selectedStoreCategory, i);
                ConfigureStorePreview(storeItemPreviews[i], selectedStoreCategory,
                    uniqueArtwork != null ? uniqueArtwork : GetStorePreviewSprite(selectedStoreCategory), tints[i], i);
                if (uniqueArtwork != null) storeItemPreviews[i].artwork.color = Color.white;
                storeItemPreviews[i].backdrop.color = equipped
                    ? new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.18f) : new Color(0.02f, 0.04f, 0.09f, 0.35f);
            }

            if (storeStatusText != null)
                storeStatusText.text = "WHITE BORDER = EQUIPPED  /  TAP OWNED SKINS TO EQUIP";
        }

        private void RefreshChapterGiftControls()
        {
            var gifts = SaveService.Data.chapterGifts;
            if (chapterGiftBrowse == null)
            {
                chapterGiftBrowse = CreateButton("Chapter Gift Collection", storePanel.transform, "CHAPTER GIFTS", () =>
                {
                    chapterGiftIndex++;
                    RefreshChapterGiftControls();
                });
                SetRect(chapterGiftBrowse.GetComponent<RectTransform>(), new Vector2(0.06f, 0.06f), new Vector2(0.65f, 0.13f));
                var preview = prefabs.Spawn(prefabs.uiPanelPrefab, "Gift Preview", chapterGiftBrowse.transform);
                chapterGiftPreview = preview.GetComponent<Image>();
                chapterGiftPreview.preserveAspect = true;
                chapterGiftPreview.raycastTarget = false;
                SetRect(preview.GetComponent<RectTransform>(), new Vector2(0.025f, 0.08f), new Vector2(0.23f, 0.92f));
                var label = chapterGiftBrowse.GetComponentInChildren<TMP_Text>(true);
                SetRect(label.rectTransform, new Vector2(0.25f, 0.07f), new Vector2(0.98f, 0.93f));
                label.enableAutoSizing = true; label.fontSizeMin = 13; label.fontSizeMax = 22;
                chapterGiftClaim = CreateButton("Claim Chapter Gift", storePanel.transform, "CLAIM", () =>
                {
                    var current = SaveService.Data?.chapterGifts;
                    if (current == null || current.Count == 0) return;
                    var gift = current[chapterGiftIndex % current.Count];
                    if (!gift.claimed && !ChapterGiftService.Claim(gift)) return;
                    EquipCosmetic((CosmeticCategory)gift.category, gift.chapter + 9);
                    ShowToast($"CHAPTER {gift.chapter:00} GIFT EQUIPPED");
                });
                SetRect(chapterGiftClaim.GetComponent<RectTransform>(), new Vector2(0.67f, 0.06f), new Vector2(0.94f, 0.13f));
            }
            var available = gifts != null && gifts.Count > 0;
            chapterGiftBrowse.gameObject.SetActive(available);
            chapterGiftClaim.gameObject.SetActive(available);
            if (!available) return;
            chapterGiftIndex %= gifts.Count;
            var selected = gifts[chapterGiftIndex];
            var category = (CosmeticCategory)selected.category;
            var labelText = chapterGiftBrowse.GetComponentInChildren<TMP_Text>(true);
            var categoryName = category == CosmeticCategory.Goal ? "HOLE" : category.ToString().ToUpperInvariant();
            labelText.text = $"CH {selected.chapter:00}  /  {categoryName}\nNORMAL GIFT  /  {(selected.claimed ? "OWNED" : "FREE TO CLAIM")}\n<size=65%>TAP TO BROWSE {gifts.Count} GIFTS</size>";
            chapterGiftPreview.sprite = CosmeticArtwork.Get(selected.category, 0);
            chapterGiftPreview.color = ChapterGiftService.GiftTint(selected.chapter + 9);
            chapterGiftClaim.GetComponentInChildren<TMP_Text>(true).text = !selected.claimed ? "CLAIM FREE"
                : GetEquippedCosmetic(category) == selected.chapter + 9 ? "EQUIPPED" : "EQUIP";
            chapterGiftClaim.interactable = !selected.claimed || GetEquippedCosmetic(category) != selected.chapter + 9;
        }

        public void ShowChapterGiftNotice(bool newGift)
        {
            if (winPanel == null) return;
            var button = winPanel.transform.Find("Chapter Gift Notice")?.GetComponent<Button>();
            if (newGift && button == null)
            {
                button = CreateButton("Chapter Gift Notice", winPanel.transform, "CHAPTER GIFT READY / OPEN STORE", ShowStore);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.10f));
                StyleButton(button);
            }
            if (button != null) button.gameObject.SetActive(newGift);
            if (newGift) ShowToast("CHAPTER COMPLETE! YOUR FREE GIFT IS READY IN THE STORE");
        }

        private void EnsureStoreRarityFilters()
        {
            if (storeRarityButtons.Count == 0)
            {
                foreach (var item in storeItemButtons)
                {
                    var rect = item.GetComponent<RectTransform>();
                    storeCardAnchorMins.Add(rect.anchorMin);
                    storeCardAnchorMaxs.Add(rect.anchorMax);
                }
                for (var i = 0; i < 5; i++)
                {
                    var filter = i - 1;
                    var name = i == 0 ? "ALL" : RarityNames[filter];
                    var button = CreateButton($"Rarity {name}", storePanel.transform, name, () =>
                    {
                        AnalyticsService.Log("store_filter_selected", "rarity", filter < 0 ? "all" : RarityNames[filter]);
                        selectedStoreRarity = filter;
                        RefreshStore();
                        RevealStoreItems();
                    });
                    var x = 0.055f + i * 0.18f;
                    SetRect(button.GetComponent<RectTransform>(), new Vector2(x, 0.692f), new Vector2(x + 0.17f, 0.726f));
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 12;
                    label.fontSizeMax = 20;
                    storeRarityButtons.Add(button);
                }
                if (storeStatusText != null)
                    SetRect(storeStatusText.rectTransform, new Vector2(0.05f, 0.656f), new Vector2(0.95f, 0.688f));
            }
            for (var i = 0; i < storeRarityButtons.Count; i++)
                SetButtonFill(storeRarityButtons[i], selectedStoreRarity == i - 1 ? session.Config.uiPrimary : session.Config.uiButton);
        }

        private void RevealStoreItems()
        {
            if (storeRevealRoutine != null) StopCoroutine(storeRevealRoutine);
            foreach (var preview in storeItemPreviews)
            {
                preview.artwork.transform.localScale = Vector3.one;
                preview.artwork.transform.localRotation = Quaternion.identity;
            }
            if (isActiveAndEnabled && !(SaveService.Data?.reduceMotion ?? false))
                storeRevealRoutine = StartCoroutine(AnimateStorePreviews());
        }

        private IEnumerator AnimateStorePreviews()
        {
            while (screenTransitionRoutine != null) yield return null;
            var elapsed = 0f;
            var live = LiveOpsConfigStore.Current;
            var duration = live.storeRevealSeconds;
            var total = duration + storeItemPreviews.Count * live.storeRevealStagger;
            while (storePanel != null && storePanel.activeInHierarchy && elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var i = 0; i < storeItemPreviews.Count; i++)
                {
                    if (!storeItemButtons[i].gameObject.activeSelf) continue;
                    var t = Mathf.Clamp01((elapsed - i * live.storeRevealStagger) / duration);
                    var rarity = GetCosmeticRarity(i);
                    var artwork = storeItemPreviews[i].artwork.transform;
                    var pulse = Mathf.Sin(t * Mathf.PI) * live.storePreviewMotionAmount * (rarity + 1);
                    artwork.localScale = Vector3.one * (Mathf.Lerp(0.82f, 1f, 1f - Mathf.Pow(1f - t, 3f)) + pulse);
                    artwork.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * live.storePreviewMotionSpeed) * rarity * 2f * (1f - t));
                }
                if (SaveService.Data?.reduceMotion ?? false) break;
                yield return null;
            }
            foreach (var preview in storeItemPreviews)
            {
                if (preview.artwork == null) continue;
                preview.artwork.transform.localScale = Vector3.one;
                preview.artwork.transform.localRotation = Quaternion.identity;
            }
            storeRevealRoutine = null;
        }

        private Image CreateStorePreviewImage(string objectName, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = CreateRect(objectName, parent);
            SetRect(rect, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.sprite = RoundedPanelSprite;
            image.type = Image.Type.Sliced;
            return image;
        }

        private static void ConfigureStorePreview(StorePreviewVisual preview, CosmeticCategory category,
            Sprite artworkSprite, Color tint, int variant)
        {
            var dark = Color.Lerp(new Color(0.015f, 0.035f, 0.06f, 1f), tint, 0.18f);
            dark.a = 0.92f;
            preview.backdrop.color = dark;
            preview.artwork.gameObject.SetActive(false);
            preview.accentPrimary.gameObject.SetActive(false);
            preview.accentSecondary.gameObject.SetActive(false);
            preview.artwork.rectTransform.localEulerAngles = Vector3.zero;
            preview.accentPrimary.rectTransform.localEulerAngles = Vector3.zero;
            preview.accentSecondary.rectTransform.localEulerAngles = Vector3.zero;

            if (artworkSprite != null)
            {
                preview.artwork.gameObject.SetActive(true);
                preview.artwork.sprite = artworkSprite;
                preview.artwork.type = Image.Type.Simple;
                preview.artwork.preserveAspect = true;
                // Keep the detailed highlights readable while still previewing the selected skin tint.
                preview.artwork.color = Color.Lerp(Color.white, tint, variant == 0 ? 0.12f : 0.46f);
                SetRect(preview.artwork.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
                preview.artwork.rectTransform.localEulerAngles = Vector3.zero;
                return;
            }

            if (category == CosmeticCategory.Walls)
            {
                preview.accentPrimary.gameObject.SetActive(true);
                preview.accentSecondary.gameObject.SetActive(true);
                preview.accentPrimary.color = Color.Lerp(tint, Color.white, 0.22f);
                preview.accentSecondary.color = Color.Lerp(tint, Color.black, 0.12f);
                SetRect(preview.accentPrimary.rectTransform, new Vector2(0.10f, 0.20f), new Vector2(0.88f, 0.43f));
                SetRect(preview.accentSecondary.rectTransform, new Vector2(0.10f, 0.20f), new Vector2(0.31f, 0.84f));
                return;
            }

            // Background styles use a miniature layered wallpaper tile, including the accent bands
            // that make the color/pattern difference visible before purchase.
            preview.artwork.gameObject.SetActive(true);
            preview.artwork.sprite = RoundedPanelSprite;
            preview.artwork.type = Image.Type.Sliced;
            preview.artwork.preserveAspect = false;
            preview.artwork.color = tint;
            SetRect(preview.artwork.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
            preview.accentPrimary.gameObject.SetActive(true);
            preview.accentSecondary.gameObject.SetActive(true);
            preview.accentPrimary.color = new Color(1f, 1f, 1f, 0.30f);
            preview.accentSecondary.color = Color.Lerp(tint, Color.black, 0.35f);
            SetRect(preview.accentPrimary.rectTransform, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.68f));
            SetRect(preview.accentSecondary.rectTransform, new Vector2(0.18f, 0.08f), new Vector2(0.38f, 0.92f));
            var angle = variant % 2 == 0 ? -18f : 18f;
            preview.accentPrimary.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            preview.accentSecondary.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        private void SelectOrBuyCosmetic(int index)
        {
            AnalyticsService.Log("select_item", ("item_id", GetCosmeticId(selectedStoreCategory, index)),
                ("category", selectedStoreCategory.ToString()), ("owned", IsCosmeticOwned(selectedStoreCategory, index)));
            if (IsCosmeticOwned(selectedStoreCategory, index))
            {
                EquipCosmetic(selectedStoreCategory, index);
                return;
            }

            if (index >= 1 && index <= 3)
            {
                var price = GetCosmeticGemPrice(index);
                if (!EconomyService.TrySpend(price, $"cosmetic_{GetCosmeticId(selectedStoreCategory, index)}"))
                {
                    AnalyticsService.Log("cosmetic_purchase_rejected", ("item_id", GetCosmeticId(selectedStoreCategory, index)), ("reason", "insufficient_gems"));
                    ShowToast("NOT ENOUGH GEMS");
                    return;
                }
                UnlockAndEquipCosmetic(selectedStoreCategory, index, "GEMS");
                return;
            }

            var product = GetCosmeticIapProductId(selectedStoreCategory, index);
            if (string.IsNullOrWhiteSpace(product))
            {
                AnalyticsService.Log("cosmetic_purchase_rejected", ("item_id", GetCosmeticId(selectedStoreCategory, index)), ("reason", "product_not_configured"));
                ShowToast("PREMIUM PRODUCT IS NOT CONFIGURED");
                return;
            }

            var iap = CosmeticIapService.Instance;
            if (iap == null || !iap.Purchase(product)) ShowToast("STORE IS CONNECTING. TRY AGAIN SHORTLY.");
        }

        private void UnlockAndEquipCosmetic(CosmeticCategory category, int index, string source)
        {
            SaveService.Data.unlockedCosmetics ??= new List<string>();
            var id = GetCosmeticId(category, index);
            if (!SaveService.Data.unlockedCosmetics.Contains(id)) SaveService.Data.unlockedCosmetics.Add(id);
            EquipCosmetic(category, index);
            AnalyticsService.Log("cosmetic_unlocked", ("item_id", id), ("category", category.ToString()), ("source", source));
            ShowToast($"{CosmeticNames[index]} UNLOCKED WITH {source}");
        }

        private void EquipCosmetic(CosmeticCategory category, int index)
        {
            if (index >= 10 && !ChapterGiftService.OwnsGift((int)category, index)) return;
            AnalyticsService.Log("cosmetic_equipped", ("item_id", GetCosmeticId(category, index)), ("category", category.ToString()));
            switch (category)
            {
                case CosmeticCategory.Ball: SaveService.Data.selectedBallSkin = index; break;
                case CosmeticCategory.Magnet: SaveService.Data.selectedMagnetSkin = index; break;
                case CosmeticCategory.Goal: SaveService.Data.selectedGoalSkin = index; break;
                case CosmeticCategory.Walls: SaveService.Data.selectedWallSkin = index; break;
                case CosmeticCategory.Glass: SaveService.Data.selectedGlassSkin = index; break;
                case CosmeticCategory.Background: SaveService.Data.selectedBackgroundSkin = index; break;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
            SaveService.SaveDeferred();
            session?.ApplyPlayerSettings();
            RefreshCurrency();
            RefreshStore();
            RevealStoreItems();
        }

        private bool IsCosmeticOwned(CosmeticCategory category, int index)
        {
            if (index >= 10) return ChapterGiftService.OwnsGift((int)category, index);
            return index == 0 || (SaveService.Data?.unlockedCosmetics?.Contains(GetCosmeticId(category, index)) ?? false);
        }

        private int GetEquippedCosmetic(CosmeticCategory category)
        {
            if (SaveService.Data == null) return 0;
            return category switch
            {
                CosmeticCategory.Ball => SaveService.Data.selectedBallSkin,
                CosmeticCategory.Magnet => SaveService.Data.selectedMagnetSkin,
                CosmeticCategory.Goal => SaveService.Data.selectedGoalSkin,
                CosmeticCategory.Walls => SaveService.Data.selectedWallSkin,
                CosmeticCategory.Glass => SaveService.Data.selectedGlassSkin,
                CosmeticCategory.Background => SaveService.Data.selectedBackgroundSkin,
                _ => 0
            };
        }

        private Color[] GetStoreTints(CosmeticCategory category)
        {
            var config = session.Config;
            return category switch
            {
                CosmeticCategory.Ball => config.ballSkinTints ?? Array.Empty<Color>(),
                CosmeticCategory.Magnet => config.magnetSkinTints ?? Array.Empty<Color>(),
                CosmeticCategory.Goal => config.goalSkinTints ?? Array.Empty<Color>(),
                CosmeticCategory.Walls => config.wallSkinTints ?? Array.Empty<Color>(),
                CosmeticCategory.Glass => config.glassSkinTints ?? Array.Empty<Color>(),
                CosmeticCategory.Background => config.backgroundSkinTints ?? Array.Empty<Color>(),
                _ => Array.Empty<Color>()
            };
        }

        private static string GetCosmeticId(CosmeticCategory category, int index) =>
            $"{category.ToString().ToLowerInvariant()}_{index:00}";

        private static int GetCosmeticGemPrice(int index)
        {
            var prices = LiveOpsConfigStore.Current.cosmeticGemPrices;
            return prices != null && index >= 0 && index < prices.Length ? Mathf.Max(0, prices[index]) : 0;
        }

        private static string GetCosmeticIapProductId(CosmeticCategory category, int index)
        {
            var ids = LiveOpsConfigStore.Current.cosmeticIapProductIds;
            var flatIndex = (int)category * CosmeticNames.Length + index;
            return ids != null && flatIndex >= 0 && flatIndex < ids.Length ? ids[flatIndex] : string.Empty;
        }

        private static string GetStorePriceLabel(CosmeticCategory category, int index)
        {
            if (index == 0) return "FREE";
            if (index <= 3) return $"{GetCosmeticGemPrice(index)} GEMS";
            var id = GetCosmeticIapProductId(category, index);
            if (string.IsNullOrWhiteSpace(id)) return "PREMIUM";
            var localized = CosmeticIapService.Instance?.GetLocalizedPrice(id);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
            var fallbacks = LiveOpsConfigStore.Current.cosmeticIapFallbackPrices;
            return fallbacks != null && index < fallbacks.Length && !string.IsNullOrWhiteSpace(fallbacks[index])
                ? fallbacks[index]
                : "PREMIUM";
        }

        private Sprite GetStorePreviewSprite(CosmeticCategory category)
        {
            var path = category switch
            {
                CosmeticCategory.Ball => session.Config.ballResource,
                CosmeticCategory.Magnet => session.Config.magnetResource,
                CosmeticCategory.Goal => session.Config.goalPocketResource,
                CosmeticCategory.Glass => session.Config.glassFrameResource,
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (StorePreviewSpriteCache.TryGetValue(path, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                Vector2.one * 0.5f, Mathf.Max(texture.width, texture.height), 0u, SpriteMeshType.FullRect);
            sprite.name = texture.name + " Store Preview";
            StorePreviewSpriteCache[path] = sprite;
            return sprite;
        }

        private static IEnumerable<string> GetAllConfiguredCosmeticProducts()
        {
            var ids = LiveOpsConfigStore.Current.cosmeticIapProductIds;
            if (ids == null) yield break;
            foreach (var id in ids)
                if (!string.IsNullOrWhiteSpace(id)) yield return id;
        }

        private void HandleCosmeticPurchase(string productId, bool restored)
        {
            var categoryCount = Enum.GetValues(typeof(CosmeticCategory)).Length;
            for (var category = 0; category < categoryCount; category++)
            for (var index = 4; index < CosmeticNames.Length; index++)
            {
                if (!string.Equals(GetCosmeticIapProductId((CosmeticCategory)category, index), productId,
                        StringComparison.Ordinal)) continue;
                var cosmeticCategory = (CosmeticCategory)category;
                if (restored)
                {
                    if (!IsCosmeticOwned(cosmeticCategory, index))
                    {
                        SaveService.Data.unlockedCosmetics ??= new List<string>();
                        SaveService.Data.unlockedCosmetics.Add(GetCosmeticId(cosmeticCategory, index));
                        SaveService.SaveDeferred();
                    }
                    RefreshStore();
                    return;
                }

                UnlockAndEquipCosmetic(cosmeticCategory, index, "PREMIUM PURCHASE");
                return;
            }
        }
    }
}

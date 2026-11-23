using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace OutsideTheBox.Editor
{
    [InitializeOnLoad]
    internal static class TmpGeneratedObjectVisibility
    {
        private static bool queued;

        static TmpGeneratedObjectVisibility()
        {
            EditorApplication.hierarchyChanged += QueueRefresh;
            QueueRefresh();
        }

        private static void QueueRefresh()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += HideGeneratedSubmeshes;
        }

        private static void HideGeneratedSubmeshes()
        {
            queued = false;
            // These are temporary render helpers owned by TMP. Hiding them does not
            // change labels, font references, prefab contents, or the active prefab stage.
            foreach (var submesh in Resources.FindObjectsOfTypeAll<TMP_SubMeshUI>())
            {
                if (submesh == null || EditorUtility.IsPersistent(submesh)) continue;
                var generatedObject = submesh.gameObject;
                if ((generatedObject.hideFlags & HideFlags.DontSaveInEditor) == 0) continue;
                if ((generatedObject.hideFlags & HideFlags.HideInHierarchy) == 0)
                    generatedObject.hideFlags |= HideFlags.HideInHierarchy;
            }
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    public static class ProjectBuilder
    {
        private const string Root = "Assets/OutsideTheBox";
        private const string ResourceRoot = Root + "/Resources/OutsideTheBox";
        private const string ConfigRoot = ResourceRoot + "/Configs";
        private const string LevelRoot = ConfigRoot + "/Levels";
        private const string LocalizationRoot = ConfigRoot + "/Localization";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string ScenePath = Root + "/Scenes/OutsideTheBox.unity";
        private const string LiveOpsAssetPath = ConfigRoot + "/LiveOpsConfig.asset";
        private const string LiveOpsJsonPath = ResourceRoot + "/default_live_config.json";
        private const string FirebaseJsonPath = Root + "/Documentation/FirebaseRemoteConfigValue.json";

        static ProjectBuilder()
        {
            // Prefabs are designer-owned: rebuild only through explicit menu/CLI commands.
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Leaving Play Mode must preserve the designer's prefab changes.
        }

        private static void BuildMissingContent()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureFolders();
            EnsureLiveOpsConfigAsset();
            CleanupTemporaryHudBuildObjects();
            var prefabRegistry = AssetDatabase.LoadAssetAtPath<RuntimePrefabRegistry>(ConfigRoot + "/RuntimePrefabRegistry.asset");
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/HUD.prefab");
            var gameRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/GameRoot.prefab");
            if (prefabRegistry == null || prefabRegistry.uiCanvasPrefab == null || prefabRegistry.directionArrowPrefab == null ||
                prefabRegistry.uiCanvasPrefab.GetComponent<ResponsiveCanvasScaler>() == null ||
                prefabRegistry.chapterBackgroundPrefab == null || prefabRegistry.tutorialOverlayPrefab == null ||
                prefabRegistry.tooltipPrefab == null || prefabRegistry.signInPanelPrefab == null ||
                prefabRegistry.feedbackBurstPrefab == null ||
                hudPrefab == null || hudPrefab.transform.Find("Mobile UI/Safe Area/Gameplay HUD") == null ||
                hudPrefab.transform.Find("Mobile UI/Full Screen Background") == null ||
                hudPrefab.transform.Find("Mobile UI/Screen Transition") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Gameplay HUD/Energy") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Gameplay HUD/Timer") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Gameplay HUD/Currency") == null ||
                 hudPrefab.transform.Find("Mobile UI/Safe Area/Gameplay HUD/Echo") == null ||
                 hudPrefab.transform.Find("Mobile UI/Safe Area/Gameplay HUD/Signature Status") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Main Menu") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Sign In Screen/Google Play Sign In") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Sign In Screen/Guest Sign In") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Account Screen/Username") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Account Screen/Password") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Account Screen/Confirm Password") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Account Screen/Sign In") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Account Screen/Create Account") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Leaderboard Screen/Results") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Leaderboard Screen/Scope Dropdown/Label") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Leaderboard Screen/Scope Dropdown Menu/All-Time") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Leaderboard Screen/Scope Dropdown Menu/Chapter 06") == null ||
                hudPrefab.GetComponentInChildren<UIGradient>(true) == null ||
                hudPrefab.GetComponentInChildren<UIFloatMotion>(true) == null ||
                hudPrefab.GetComponentInChildren<UIScreenMotion>(true) != null ||
                hudPrefab.GetComponentInChildren<UIRoundRectGraphic>(true) == null ||
                !hudPrefab.GetComponentsInChildren<UIRoundRectGraphic>(true).Any(graphic => graphic.GradientEnabled) ||
                hudPrefab.GetComponentInChildren<UIRadialGlow>(true) == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Main Menu/Back") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Gameplay HUD/Back") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Win Screen/Back") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Time Out/Back") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Pause Sheet/Back") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Main Menu/Daily Challenge") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Main Menu/Free Coins") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Main Menu/Google Play") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Main Menu/Account") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Main Menu/Leaderboard") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Time Out/Watch Ad") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Settings Screen/Privacy") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Tutorial Overlay") == null ||
                hudPrefab.transform.Find("Mobile UI/Safe Area/Tooltip") == null ||
                 hudPrefab.GetComponentInChildren<UIJuice>(true) == null ||
                gameRootPrefab == null || gameRootPrefab.GetComponentInChildren<BoardView>(true) == null ||
                gameRootPrefab.GetComponent<MobilePerformanceController>() == null ||
                gameRootPrefab.GetComponent<LiveOpsManager>() == null ||
                gameRootPrefab.GetComponent<GoogleUmpConsent>() == null)
            {
                EnsureFolders();
                BuildPrefabs();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            EnsureStoreScreenPrefab();
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigRoot + "/GameConfig.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ConfigRoot + "/LevelCatalog.asset");
            var lastLevel = catalog != null && catalog.Count > 0 ? catalog.Get(catalog.Count - 1) : null;
            var validationPath = Path.GetFullPath(Root + "/Documentation/LevelValidation.txt");
            var validationIsCurrent = File.Exists(validationPath) &&
                                      File.ReadLines(validationPath).Any(line => line.Contains("Visible groups"));
            // A generator revision only needs to update generated level definitions.
            // Do not rebuild the user's scene and prefab library just for this upgrade.
            if (config != null && catalog != null && catalog.Count == 60 && lastLevel != null &&
                lastLevel.generatedContentVersion != config.contentVersion)
            {
                BuildLevels(catalog, config);
                AssetDatabase.SaveAssets();
                WriteValidationReport(catalog);
                Debug.Log("Outside the Box generated levels updated for the current generator version.");
                lastLevel = catalog.Get(catalog.Count - 1);
                validationIsCurrent = true;
            }
            if (config != null && catalog != null && catalog.Count == 60 && lastLevel != null &&
                lastLevel.generatedContentVersion == config.contentVersion &&
                lastLevel.width == config.GetChapter(60).boardSize &&
                validationIsCurrent && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            BuildAll();
        }

        [MenuItem("Tools/Outside The Box/Build Project Content")]
        public static void BuildAll()
        {
            EnsureFolders();
            var liveOpsConfig = EnsureLiveOpsConfigAsset();
            var config = GetOrCreate<GameConfig>(ConfigRoot + "/GameConfig.asset");
            var strings = GetOrCreate<GameStrings>(LocalizationRoot + "/Strings_en.asset");
            strings.time = "TIME";
            strings.coins = "GEMS";
            strings.timeUp = "TIME'S UP";
            strings.freeCoins = "FREE GEMS";
            strings.watchAd = "WATCH AD TO CONTINUE";
            strings.useCoins = "USE GEMS";
            strings.googlePlay = "GOOGLE PLAY";
            strings.leaderboard = "LEADERBOARD";
            strings.unityCloud = "GUEST SIGN IN";
            strings.guestWarning = "GUEST SIGN-IN USES A UNIQUE UNITY CLOUD USERNAME AND PASSWORD.";
            var catalog = GetOrCreate<LevelCatalog>(ConfigRoot + "/LevelCatalog.asset");
            BuildLevels(catalog, config);
            EditorUtility.SetDirty(config);
            EditorUtility.SetDirty(strings);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            BuildPrefabs();
            BuildScene();
            ConfigureProject();
            WriteValidationReport(catalog);
            ExportLiveOpsConfigJson(liveOpsConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Outside the Box content built: {catalog.Count} levels, prefab library and mobile scene.");
        }

        public static void BuildAllCommandLine()
        {
            try { BuildAll(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/Outside The Box/Rebuild Prefab Library")]
        public static void BuildPrefabsCommandLine()
        {
            try
            {
                EnsureFolders();
                BuildPrefabs();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root);
            EnsureFolder(Root + "/Resources");
            EnsureFolder(ResourceRoot);
            EnsureFolder(ConfigRoot);
            EnsureFolder(LevelRoot);
            EnsureFolder(LocalizationRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Documentation");
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static LiveOpsConfigAsset EnsureLiveOpsConfigAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LiveOpsConfigAsset>(LiveOpsAssetPath);
            if (asset != null)
            {
                asset.values ??= new LiveOpsConfigData();
                asset.values.Normalize();
                return asset;
            }

            asset = ScriptableObject.CreateInstance<LiveOpsConfigAsset>();
            asset.values = new LiveOpsConfigData();
            var jsonPath = Path.GetFullPath(LiveOpsJsonPath);
            if (File.Exists(jsonPath))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(jsonPath), asset.values);
            asset.values.Normalize();
            AssetDatabase.CreateAsset(asset, LiveOpsAssetPath);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void ExportLiveOpsConfigJson(LiveOpsConfigAsset asset)
        {
            if (asset == null) return;
            asset.values ??= new LiveOpsConfigData();
            asset.values.Normalize();
            var json = JsonUtility.ToJson(asset.values, true) + Environment.NewLine;
            File.WriteAllText(Path.GetFullPath(LiveOpsJsonPath), json);
            File.WriteAllText(Path.GetFullPath(FirebaseJsonPath), json);
            EditorUtility.SetDirty(asset);
            AssetDatabase.ImportAsset(LiveOpsJsonPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(FirebaseJsonPath, ImportAssetOptions.ForceUpdate);
        }

        private static void BuildLevels(LevelCatalog catalog, GameConfig config)
        {
            catalog.levels.Clear();
            for (var number = 1; number <= 60; number++)
            {
                var path = $"{LevelRoot}/Level_{number:000}.asset";
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null)
                {
                    level = ScriptableObject.CreateInstance<LevelDefinition>();
                    AssetDatabase.CreateAsset(level, path);
                }
                if (level.wallSegments == null || level.wallSegments.Count == 0)
                    PopulateLevel(level, number, config);
                EditorUtility.SetDirty(level);
                catalog.levels.Add(level);
            }
            EditorUtility.SetDirty(catalog);
        }

        private static void PopulateLevel(LevelDefinition level, int number, GameConfig config)
        {
            level.levelId = $"level_{number:000}";
            level.levelNumber = number;
            level.generatorSeed = 74000 + number * 977;
            level.useNormalPhysics = number == 14;
            level.generatedContentVersion = 0;
            level.wallSegments = new List<WallSegmentDefinition>();

            var generated = ProceduralLevelGenerator.Create(level, config);
            if (generated == null || !LevelSolver.Solve(generated).solved)
                throw new InvalidOperationException($"Could not produce a solvable definition for level {number}.");

            level.width = generated.width;
            level.height = generated.height;
            level.start = generated.start;
            level.exit = generated.exit;
            level.useCustomStartingMagnet = generated.useCustomStartingMagnet;
            level.startingMagnet = generated.startingMagnet;
            level.walls = new List<GridCell>(generated.walls);
            level.wallSegments = new List<WallSegmentDefinition>(generated.wallSegments);
            level.gates = new List<GateDefinition>(generated.gates);
            level.movingWalls = new List<MovingWallDefinition>(generated.movingWalls);
            level.fieldZones = new List<FieldZoneDefinition>(generated.fieldZones);
            level.magneticImprints = new List<MagneticImprintDefinition>(generated.magneticImprints);
            level.fieldLenses = new List<FieldLensDefinition>(generated.fieldLenses);
            level.polarityLocks = new List<PolarityLockDefinition>(generated.polarityLocks);
            level.echoMagnetEnabled = generated.echoMagnetEnabled;
            level.ferrofluidMemoryEnabled = generated.ferrofluidMemoryEnabled;
            level.orbitGoalEnabled = generated.orbitGoalEnabled;
            level.orbitGoalTurns = generated.orbitGoalTurns;
            level.pushUnlocked = generated.pushUnlocked;
            level.targetMoves = generated.targetMoves;
            level.targetEnergy = generated.targetEnergy;
            level.tutorialPrompt = generated.tutorialPrompt;
            level.verifiedSolution = new List<SolutionStep>(generated.verifiedSolution);
            level.generatedContentVersion = config.contentVersion;
            UnityEngine.Object.DestroyImmediate(generated);
        }

        private static List<GridCell> GenerateWalls(LevelDefinition level, System.Random random,
            ChapterSettings chapter, int chapterIndex, int levelsPerChapter)
        {
            var result = new List<GridCell>();
            var progress = chapterIndex / (float)Mathf.Max(1, levelsPerChapter - 1);
            var baseDensity = (chapter?.wallDensity ?? 0.1f) + (chapter?.wallDensityRamp ?? 0.04f) * progress;
            for (var x = 0; x < level.width; x++)
            for (var y = 0; y < level.height; y++)
            {
                var cell = new GridCell(x, y);
                if (cell == level.start || cell == level.exit) continue;
                if (random.NextDouble() < baseDensity) result.Add(cell);
            }
            // Every non-first level visibly demonstrates at least one obstacle.
            if (level.levelNumber > 1 && result.Count == 0)
            {
                var center = new GridCell(level.width / 2, level.height / 2);
                if (center != level.start && center != level.exit) result.Add(center);
            }
            return result;
        }

        private static List<GateDefinition> GenerateGates(LevelDefinition level, System.Random random, ChapterSettings chapter)
        {
            var result = new List<GateDefinition>();
            if (chapter == null || !chapter.oneWayGates) return result;
            var count = Mathf.Max(1, chapter.gateCount);
            var candidates = new List<GridCell>();
            for (var x = 0; x < level.width; x++)
            for (var y = 0; y < level.height; y++)
            {
                var cell = new GridCell(x, y);
                if (cell != level.start && cell != level.exit && !level.walls.Contains(cell)) candidates.Add(cell);
            }
            for (var i = 0; i < count && candidates.Count > 0; i++)
            {
                var pick = random.Next(candidates.Count);
                var cell = candidates[pick];
                candidates.RemoveAt(pick);
                result.Add(new GateDefinition
                {
                    cell = cell,
                    allowedEntryDirection = (CardinalDirection)random.Next(0, 4)
                });
            }
            return result;
        }

        private static void BuildPrefabs()
        {
            SavePrefab("Camera", go =>
            {
                go.tag = "MainCamera";
                go.transform.position = new Vector3(0f, 0f, -10f);
                var camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                go.AddComponent<AudioListener>();
            });
            SavePrefab("Container", _ => { });
            SavePrefab("SpritePiece", go => go.AddComponent<SpriteRenderer>());
            SavePrefab("BoardSurface", go => go.AddComponent<SpriteRenderer>());
            SavePrefab("GlassPiece", go => go.AddComponent<SpriteRenderer>());
            SavePrefab("Wall", go => go.AddComponent<SpriteRenderer>());
            SavePrefab("Exit", go => go.AddComponent<SpriteRenderer>());
            SavePrefab("GoalGlow", go => go.AddComponent<SpriteRenderer>());
            SavePrefab("OneWayGate", go => go.AddComponent<SpriteRenderer>());
            SavePrefab("Ball", go =>
            {
                go.AddComponent<SpriteRenderer>();
                var highlight = CreateChild("Highlight", go.transform, typeof(SpriteRenderer));
                highlight.transform.localPosition = new Vector3(-0.2f, 0.2f, -0.05f);
                highlight.transform.localScale = Vector3.one * 0.22f;
            });
            SavePrefab("Magnet", go =>
            {
                go.AddComponent<SpriteRenderer>();
                var red = CreateChild("Red Pole", go.transform, typeof(SpriteRenderer));
                red.transform.localPosition = new Vector3(-0.27f, 0f, 0f);
                red.transform.localScale = new Vector3(0.46f, 1f, 1f);
                var blue = CreateChild("Blue Pole", go.transform, typeof(SpriteRenderer));
                blue.transform.localPosition = new Vector3(0.27f, 0f, 0f);
                blue.transform.localScale = new Vector3(0.46f, 1f, 1f);
            });
            SavePrefab("MagneticField", go =>
            {
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = GetOrCreateSpriteMaterial();
                line.useWorldSpace = true;
                line.sortingOrder = 6;
            });
            SavePrefab("DirectionArrow", go =>
            {
                var mesh = go.AddComponent<TextMesh>();
                mesh.text = "▲";
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.characterSize = 0.14f;
                mesh.fontSize = 56;
            });
            SavePrefab("ChapterBackground", go =>
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = GetOrCreateChapterBackgroundMaterial();
                renderer.sortingOrder = -100;
            });
            SavePrefab("FeedbackBurst", go =>
            {
                var particles = go.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.maxParticles = 120;
                var emission = particles.emission;
                emission.enabled = false;
                var shape = particles.shape;
                shape.enabled = false;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sortingOrder = 20;
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            });
            SavePrefab("Board", go => go.AddComponent<BoardView>());
            BuildUiPrefabs();
            BuildPrefabRegistry();
            BuildHudPrefab();
            SavePrefab("GameRoot", go =>
            {
                var session = go.AddComponent<GameSession>();
                var mobilePerformance = go.AddComponent<MobilePerformanceController>();
                go.AddComponent<LiveOpsManager>();
                go.AddComponent<GoogleUmpConsent>();
                var input = go.AddComponent<MagnetInput>();
                var feedback = go.AddComponent<FeedbackService>();
                var effectsSource = go.AddComponent<AudioSource>();
                var musicSource = go.AddComponent<AudioSource>();
                var board = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("Board"), go.transform);
                board.name = "Board";
                var hud = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("HUD"), go.transform);
                hud.name = "HUD";

                var boardView = board.GetComponent<BoardView>();
                var ui = hud.GetComponent<GameUIController>();
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigRoot + "/GameConfig.asset");
                var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(ConfigRoot + "/LevelCatalog.asset");

                var serializedSession = new SerializedObject(session);
                serializedSession.FindProperty("config").objectReferenceValue = config;
                serializedSession.FindProperty("catalog").objectReferenceValue = catalog;
                serializedSession.FindProperty("boardView").objectReferenceValue = boardView;
                serializedSession.FindProperty("magnetInput").objectReferenceValue = input;
                serializedSession.FindProperty("ui").objectReferenceValue = ui;
                serializedSession.FindProperty("feedback").objectReferenceValue = feedback;
                serializedSession.FindProperty("mobilePerformance").objectReferenceValue = mobilePerformance;
                serializedSession.ApplyModifiedPropertiesWithoutUndo();

                var serializedInput = new SerializedObject(input);
                serializedInput.FindProperty("boardView").objectReferenceValue = boardView;
                serializedInput.ApplyModifiedPropertiesWithoutUndo();

                var serializedFeedback = new SerializedObject(feedback);
                serializedFeedback.FindProperty("effectsSource").objectReferenceValue = effectsSource;
                serializedFeedback.FindProperty("musicSource").objectReferenceValue = musicSource;
                serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
            });
            BuildPrefabRegistry();
        }

        private static void EnsureStoreScreenPrefab()
        {
            var registry = AssetDatabase.LoadAssetAtPath<RuntimePrefabRegistry>(ConfigRoot + "/RuntimePrefabRegistry.asset");
            var asset = LoadPrefab("StorePanel");
            if (registry == null) return;
            if (asset == null)
            {
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigRoot + "/GameConfig.asset");
                if (config == null) return;
                GameObject root = null;
                GameObject sessionObject = null;
                try
                {
                    root = new GameObject("HUD");
                    root.hideFlags = HideFlags.HideAndDontSave;
                    var controller = root.AddComponent<GameUIController>();
                    sessionObject = new GameObject("HUD Build Session");
                    sessionObject.SetActive(false);
                    sessionObject.hideFlags = HideFlags.HideAndDontSave;
                    var session = sessionObject.AddComponent<GameSession>();
                    var serializedSession = new SerializedObject(session);
                    serializedSession.FindProperty("config").objectReferenceValue = config;
                    serializedSession.ApplyModifiedPropertiesWithoutUndo();
                    controller.Initialize(session);
                    var screen = root.transform.Find("Mobile UI/Safe Area/Store Screen");
                    if (screen == null) throw new InvalidOperationException("Store screen could not be built.");
                    if (PrefabUtility.IsPartOfPrefabInstance(screen.gameObject))
                        PrefabUtility.UnpackPrefabInstance(screen.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    screen.gameObject.hideFlags = HideFlags.None;
                    asset = PrefabUtility.SaveAsPrefabAsset(screen.gameObject, $"{PrefabRoot}/StorePanel.prefab");
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Store prefab generation did not finish: {exception.Message}");
                }
                finally
                {
                    if (sessionObject != null) UnityEngine.Object.DestroyImmediate(sessionObject);
                    if (root != null) UnityEngine.Object.DestroyImmediate(root);
                }
            }
            if (asset != null && registry.storePanelPrefab != asset)
            {
                registry.storePanelPrefab = asset;
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }
        }

        private static void BuildHudPrefab()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigRoot + "/GameConfig.asset");
            if (config == null)
                throw new InvalidOperationException("GameConfig is required before building HUD.prefab.");
            CleanupTemporaryHudBuildObjects();
            GameObject root = null;
            GameObject sessionObject = null;
            try
            {
                root = new GameObject("HUD");
                var controller = root.AddComponent<GameUIController>();
                sessionObject = new GameObject("HUD Build Session");
                sessionObject.SetActive(false);
                sessionObject.hideFlags = HideFlags.HideAndDontSave;
                var session = sessionObject.AddComponent<GameSession>();
                var serializedSession = new SerializedObject(session);
                serializedSession.FindProperty("config").objectReferenceValue = config;
                serializedSession.ApplyModifiedPropertiesWithoutUndo();

                controller.Initialize(session);
                var safeArea = root.transform.Find("Mobile UI/Safe Area");
                if (safeArea == null)
                    throw new InvalidOperationException("HUD builder did not create its Safe Area hierarchy.");
                SaveAndReplaceScreen(safeArea, "Sign In Screen", "SignInPanel");
                SaveAndReplaceScreen(safeArea, "Account Screen", "AccountPanel");
                SaveAndReplaceScreen(safeArea, "Leaderboard Screen", "LeaderboardPanel");
                SaveAndReplaceScreen(safeArea, "Store Screen", "StorePanel");
                SaveAndReplaceScreen(safeArea, "Main Menu", "MainMenu");
                SaveAndReplaceScreen(safeArea, "Gameplay HUD", "GameplayHUD");
                SaveAndReplaceScreen(safeArea, "Win Screen", "WinPanel");
                SaveAndReplaceScreen(safeArea, "Pause Sheet", "PausePanel");
                SaveAndReplaceScreen(safeArea, "Time Out", "TimeOutPanel");
                SaveAndReplaceScreen(safeArea, "Chapter Select", "ChapterSelect");
                SaveAndReplaceScreen(safeArea, "Level Select", "LevelSelect");
                SaveAndReplaceScreen(safeArea, "Settings Screen", "SettingsPanel");
                SaveAndReplaceScreen(safeArea, "Tutorial Overlay", "TutorialOverlay");
                SaveAndReplaceScreen(safeArea, "Tooltip", "Tooltip");
                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/HUD.prefab");
            }
            finally
            {
                if (sessionObject != null) UnityEngine.Object.DestroyImmediate(sessionObject);
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                CleanupTemporaryHudBuildObjects();
            }
        }

        private static void CleanupTemporaryHudBuildObjects()
        {
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var i = objects.Length - 1; i >= 0; i--)
            {
                var item = objects[i];
                if (item == null || !item.scene.IsValid()) continue;
                var leakedSession = item.name == "HUD Build Session";
                var leakedRoot = item.name == "HUD" && item.transform.parent == null &&
                                 item.GetComponent<GameUIController>() != null;
                if (leakedSession || leakedRoot) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        private static void SaveAndReplaceScreen(Transform parent, string childName, string prefabName)
        {
            var child = parent.Find(childName);
            if (child == null)
                throw new InvalidOperationException($"HUD builder did not create '{childName}'.");
            var siblingIndex = child.GetSiblingIndex();
            if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            var asset = PrefabUtility.SaveAsPrefabAsset(child.gameObject, $"{PrefabRoot}/{prefabName}.prefab");
            UnityEngine.Object.DestroyImmediate(child.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.name = childName;
            instance.transform.SetSiblingIndex(siblingIndex);
        }

        private static void BuildUiPrefabs()
        {
            SaveRectPrefab("UICanvas", go =>
            {
                go.AddComponent<Canvas>();
                go.AddComponent<CanvasScaler>();
                go.AddComponent<ResponsiveCanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            });
            SaveRectPrefab("UISafeArea", go =>
            {
                go.AddComponent<SafeAreaFitter>();
            });
            SaveRectPrefab("UIRect", _ => { });
            SaveRectPrefab("UIPanel", go =>
            {
                go.AddComponent<Image>();
            });
            SaveRectPrefab("UIText", go =>
            {
                go.AddComponent<TextMeshProUGUI>();
            });
            SaveRectPrefab("UIButton", go =>
            {
                go.AddComponent<Image>();
                go.AddComponent<Button>();
                var label = CreateChild("Label", go.transform, typeof(RectTransform), typeof(TextMeshProUGUI));
                var rect = label.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            });
            SaveRectPrefab("UIScroll", go =>
            {
                go.AddComponent<Image>();
                go.AddComponent<Mask>();
                go.AddComponent<ScrollRect>();
                CreateChild("Level Button Content", go.transform, typeof(RectTransform), typeof(GridLayoutGroup));
            });
            SavePrefab("EventSystem", go =>
            {
                go.AddComponent<EventSystem>();
                go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            });
            // Compatibility names retained for designers already using these assets.
            SaveRectPrefab("LevelButton", go =>
            {
                go.AddComponent<Image>();
                go.AddComponent<Button>();
            });
            SaveRectPrefab("WinPanel", go => go.AddComponent<Image>());
            SaveRectPrefab("SettingsPanel", go => go.AddComponent<Image>());
            SaveRectPrefab("PausePanel", go => go.AddComponent<Image>());
        }

        private static void BuildPrefabRegistry()
        {
            var registry = GetOrCreate<RuntimePrefabRegistry>(ConfigRoot + "/RuntimePrefabRegistry.asset");
            registry.cameraPrefab = LoadPrefab("Camera");
            registry.boardPrefab = LoadPrefab("Board");
            registry.gameRootPrefab = LoadPrefab("GameRoot");
            registry.hudPrefab = LoadPrefab("HUD");
            registry.containerPrefab = LoadPrefab("Container");
            registry.spritePiecePrefab = LoadPrefab("SpritePiece");
            registry.boardSurfacePrefab = LoadPrefab("BoardSurface");
            registry.glassPiecePrefab = LoadPrefab("GlassPiece");
            registry.wallPrefab = LoadPrefab("Wall");
            registry.exitPrefab = LoadPrefab("Exit");
            registry.goalGlowPrefab = LoadPrefab("GoalGlow");
            registry.oneWayGatePrefab = LoadPrefab("OneWayGate");
            registry.ballPrefab = LoadPrefab("Ball");
            registry.magnetPrefab = LoadPrefab("Magnet");
            registry.magneticFieldPrefab = LoadPrefab("MagneticField");
            registry.directionArrowPrefab = LoadPrefab("DirectionArrow");
            registry.chapterBackgroundPrefab = LoadPrefab("ChapterBackground");
            registry.feedbackBurstPrefab = LoadPrefab("FeedbackBurst");
            registry.railChallengeMarkerPrefab = LoadPrefab("RailChallengeMarker");
            registry.mechanicWorldIconPrefab = LoadPrefab("MechanicWorldIcon");
            registry.mechanicTutorialIconPrefab = LoadPrefab("MechanicTutorialIcon");
            registry.mechanicButtonIconPrefab = LoadPrefab("MechanicButtonIcon");
            registry.mechanicLegendPrefab = LoadPrefab("MechanicLegend");
            registry.uiCanvasPrefab = LoadPrefab("UICanvas");
            registry.uiSafeAreaPrefab = LoadPrefab("UISafeArea");
            registry.uiRectPrefab = LoadPrefab("UIRect");
            registry.uiPanelPrefab = LoadPrefab("UIPanel");
            registry.uiTextPrefab = LoadPrefab("UIText");
            registry.uiButtonPrefab = LoadPrefab("UIButton");
            registry.uiScrollPrefab = LoadPrefab("UIScroll");
            registry.eventSystemPrefab = LoadPrefab("EventSystem");
            registry.signInPanelPrefab = LoadPrefab("SignInPanel");
            registry.accountPanelPrefab = LoadPrefab("AccountPanel");
            registry.leaderboardPanelPrefab = LoadPrefab("LeaderboardPanel");
            registry.timeOutPanelPrefab = LoadPrefab("TimeOutPanel");
            registry.mainMenuPrefab = LoadPrefab("MainMenu");
            registry.gameplayHudPrefab = LoadPrefab("GameplayHUD");
            registry.winPanelPrefab = LoadPrefab("WinPanel");
            registry.pausePanelPrefab = LoadPrefab("PausePanel");
            registry.chapterSelectPrefab = LoadPrefab("ChapterSelect");
            registry.levelSelectPrefab = LoadPrefab("LevelSelect");
            registry.settingsPanelPrefab = LoadPrefab("SettingsPanel");
            registry.tutorialOverlayPrefab = LoadPrefab("TutorialOverlay");
            registry.tooltipPrefab = LoadPrefab("Tooltip");
            registry.storePanelPrefab = LoadPrefab("StorePanel");
            registry.gemStorePanelPrefab = LoadPrefab("GemStorePanel");
            registry.gemBalanceButtonPrefab = LoadPrefab("GemBalanceButton");
            registry.quitConfirmationPrefab = LoadPrefab("QuitConfirmationPanel");
            registry.offlinePanelPrefab = LoadPrefab("OfflinePanel");
            registry.cloudSyncPanelPrefab = LoadPrefab("CloudSyncPanel");
            registry.loadingOverlayPrefab = LoadPrefab("LoadingOverlay");
            registry.screenTransitionPrefab = LoadPrefab("ScreenTransition");
            registry.levelPipeTransitionPrefab = LoadPrefab("LevelPipeTransition");
            registry.levelPipeTransition3DPrefab = LoadPrefab("LevelPipeTransition3D");
            EditorUtility.SetDirty(registry);
        }

        private static GameObject LoadPrefab(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{name}.prefab");

        private static GameObject CreateChild(string name, Transform parent, params Type[] components)
        {
            var child = new GameObject(name, components);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void SaveRectPrefab(string name, Action<GameObject> configure)
        {
            var go = new GameObject(name, typeof(RectTransform));
            configure(go);
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static Material GetOrCreateSpriteMaterial()
        {
            const string path = Root + "/Materials/MagneticField.mat";
            EnsureFolder(Root + "/Materials");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material GetOrCreateChapterBackgroundMaterial()
        {
            const string path = Root + "/Materials/ChapterLiveWallpaper.mat";
            EnsureFolder(Root + "/Materials");
            var shader = Shader.Find("OutsideTheBox/ChapterLiveWallpaper");
            if (shader == null)
                throw new InvalidOperationException("ChapterLiveWallpaper shader has not been imported yet.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static void SavePrefab(string name, Action<GameObject> configure)
        {
            var go = new GameObject(name);
            configure(go);
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "OutsideTheBox";
            var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Camera.prefab");
            PrefabUtility.InstantiatePrefab(cameraPrefab, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigureProject()
        {
            PlayerSettings.productName = "Outside the Box";
            PlayerSettings.companyName = "Outside the Box Studio";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.outsidethebox.puzzle");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(2, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.gcIncremental = true;
            QualitySettings.vSyncCount = 1;
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigRoot + "/GameConfig.asset");
            PlayerSettings.runInBackground = config != null && config.runInBackgroundOnDevice;
            Application.runInBackground = config == null || config.runInBackgroundInEditor;
            Application.targetFrameRate = config != null ? config.gameplayFrameRate : 60;
        }

        private static void WriteValidationReport(LevelCatalog catalog)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigRoot + "/GameConfig.asset");
            var lines = new List<string>
            {
                "OUTSIDE THE BOX - GENERATED LEVEL VALIDATION",
                $"Generated: {DateTime.UtcNow:O}",
                $"Total levels: {catalog.Count}",
                string.Empty,
                "Level | Size | Wall cells | Visible groups | Gates | Push | Shortest beats | Visited states | Status"
            };
            foreach (var level in catalog.levels)
            {
                var result = LevelSolver.Solve(level);
                var wallGroups = ProceduralLevelGenerator.CountWallGroups(level);
                var chapter = config != null
                    ? config.GetChapter(level.useNormalPhysics ? 1 : level.levelNumber)
                    : null;
                var complexEnough = chapter == null || wallGroups >= chapter.minimumWallGroups;
                var verified = result.solved && complexEnough && SequenceWins(level, level.verifiedSolution);
                lines.Add($"{level.levelNumber:00} | {level.width}x{level.height} | {level.walls.Count} | {wallGroups} | {level.gates.Count} | {level.pushUnlocked} | {result.movementBeats} | {result.visitedStates} | {(verified ? "PASS" : "FAIL")}");
                if (!verified)
                    throw new InvalidOperationException($"Level {level.levelNumber} failed verification " +
                                                        $"(solved={result.solved}, wallGroups={wallGroups}, required={chapter?.minimumWallGroups ?? 0}).");
            }
            File.WriteAllLines(Path.GetFullPath(Root + "/Documentation/LevelValidation.txt"), lines);
        }

        private static bool SequenceWins(LevelDefinition level, IEnumerable<SolutionStep> steps)
        {
            var state = new GameState(level.start, new RailSlot(MagnetSide.Left, level.start.y), ForceMode.Pull);
            foreach (var step in steps)
            {
                state.magnet = step.magnet;
                state.mode = step.mode;
                var result = GameRules.Advance(level, state);
                state = result.state;
                if (result.won) return true;
            }
            return state.ball == level.exit;
        }
    }
}

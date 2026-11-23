using System;
using UnityEngine;

namespace OutsideTheBox
{
    /// <summary>
    /// Central asset reference for every GameObject that may be spawned while the game is running.
    /// Runtime systems instantiate these authored prefabs instead of constructing GameObjects in code.
    /// </summary>
    [CreateAssetMenu(menuName = "Outside The Box/Runtime Prefab Registry", fileName = "RuntimePrefabRegistry")]
    public sealed class RuntimePrefabRegistry : ScriptableObject
    {
        public const string ResourcePath = "OutsideTheBox/Configs/RuntimePrefabRegistry";

        [Header("Roots")]
        public GameObject cameraPrefab;
        public GameObject boardPrefab;
        public GameObject gameRootPrefab;
        public GameObject hudPrefab;

        [Header("Board Pieces")]
        public GameObject containerPrefab;
        public GameObject spritePiecePrefab;
        public GameObject boardSurfacePrefab;
        public GameObject glassPiecePrefab;
        public GameObject wallPrefab;
        public GameObject exitPrefab;
        public GameObject goalGlowPrefab;
        public GameObject oneWayGatePrefab;
        public GameObject ballPrefab;
        public GameObject magnetPrefab;
        public GameObject magneticFieldPrefab;
        public GameObject directionArrowPrefab;
        public GameObject chapterBackgroundPrefab;
        public GameObject feedbackBurstPrefab;
        public GameObject railChallengeMarkerPrefab;
        public GameObject mechanicWorldIconPrefab;
        public GameObject mechanicTutorialIconPrefab;
        public GameObject mechanicLegendPrefab;
        public GameObject mechanicButtonIconPrefab;

        [Header("UI")]
        public GameObject uiCanvasPrefab;
        public GameObject uiSafeAreaPrefab;
        public GameObject uiRectPrefab;
        public GameObject uiPanelPrefab;
        public GameObject uiTextPrefab;
        public GameObject uiButtonPrefab;
        public GameObject uiScrollPrefab;
        public GameObject eventSystemPrefab;

        [Header("UI Screens")]
        public GameObject signInPanelPrefab;
        public GameObject accountPanelPrefab;
        public GameObject leaderboardPanelPrefab;
        public GameObject timeOutPanelPrefab;
        public GameObject mainMenuPrefab;
        public GameObject gameplayHudPrefab;
        public GameObject winPanelPrefab;
        public GameObject pausePanelPrefab;
        public GameObject chapterSelectPrefab;
        public GameObject levelSelectPrefab;
        public GameObject settingsPanelPrefab;
        public GameObject tutorialOverlayPrefab;
        public GameObject tooltipPrefab;
        public GameObject storePanelPrefab;
        public GameObject gemStorePanelPrefab;
        public GameObject gemBalanceButtonPrefab;

        [Header("Service Screens")]
        public GameObject quitConfirmationPrefab;
        public GameObject offlinePanelPrefab;
        public GameObject cloudSyncPanelPrefab;
        public GameObject loadingOverlayPrefab;
        public GameObject screenTransitionPrefab;
        public GameObject levelPipeTransitionPrefab;
        public GameObject levelPipeTransition3DPrefab;

        private static RuntimePrefabRegistry cached;

        public static RuntimePrefabRegistry Current
        {
            get
            {
                if (cached == null) cached = Resources.Load<RuntimePrefabRegistry>(ResourcePath);
                if (cached == null)
                    throw new InvalidOperationException(
                        $"Missing prefab registry at Resources/{ResourcePath}. Run Tools > Outside The Box > Build Project Content.");
                return cached;
            }
        }

        public GameObject Spawn(GameObject prefab, string instanceName, Transform parent = null)
        {
            if (prefab == null)
                throw new InvalidOperationException($"Prefab '{instanceName}' is not assigned in {name}.");
            var instance = Instantiate(prefab, parent, false);
            instance.name = instanceName;
            return instance;
        }
    }
}

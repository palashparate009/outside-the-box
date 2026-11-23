using UnityEditor;
using UnityEngine;

namespace OutsideTheBox.Editor
{
    [InitializeOnLoad]
    internal static class EditorPerformanceSetup
    {
        static EditorPerformanceSetup()
        {
            EditorApplication.delayCall += ApplyLeanImportSettings;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ApplyPlayModeFrameSettings();
                return;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += ApplyLeanImportSettings;
        }

        private static void ApplyPlayModeFrameSettings()
        {
            var config = Resources.Load<GameConfig>("OutsideTheBox/Configs/GameConfig");
            Application.runInBackground = config == null || config.runInBackgroundInEditor;
            Application.targetFrameRate = config != null ? config.gameplayFrameRate : 60;
        }

        private static void ApplyLeanImportSettings()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSettings.enterPlayModeOptionsEnabled)
                EditorSettings.enterPlayModeOptionsEnabled = false;
            if (QualitySettings.vSyncCount != 1)
                QualitySettings.vSyncCount = 1;
            EnableGameViewVSync();
            if (EditorUserSettings.desiredImportWorkerCount != 1)
                EditorUserSettings.desiredImportWorkerCount = 1;
            if (EditorUserSettings.standbyImportWorkerCount != 0)
                EditorUserSettings.standbyImportWorkerCount = 0;
            if (EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds != 5000)
                EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = 5000;
        }

        private static void EnableGameViewVSync()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (var i = 0; i < windows.Length; i++)
            {
                var window = windows[i];
                if (window == null || window.GetType().Name != "GameView") continue;
                var serializedWindow = new SerializedObject(window);
                var vSync = serializedWindow.FindProperty("m_VSyncEnabled");
                if (vSync == null || vSync.boolValue) continue;
                vSync.boolValue = true;
                serializedWindow.ApplyModifiedPropertiesWithoutUndo();
                window.Repaint();
            }
        }
    }
}

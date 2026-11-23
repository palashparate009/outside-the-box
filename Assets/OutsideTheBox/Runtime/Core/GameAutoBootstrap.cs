using UnityEngine;

namespace OutsideTheBox
{
    /// <summary>
    /// Safety net for designers: if Play is pressed from an empty/template scene,
    /// create the same runtime roots used by the generated production scene.
    /// </summary>
    public static class GameAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameExists()
        {
            if (Object.FindFirstObjectByType<GameSession>() != null) return;
            var prefabs = RuntimePrefabRegistry.Current;

            if (Camera.main == null)
                prefabs.Spawn(prefabs.cameraPrefab, "Main Camera");

            // GameSession.Awake is invoked inside Instantiate. Older generated
            // GameRoot prefabs do not contain Board yet, so create the compatibility
            // Board first and let Awake discover it instead of adding it too late.
            var rootPrefabContainsBoard = prefabs.gameRootPrefab != null &&
                                          prefabs.gameRootPrefab.GetComponentInChildren<BoardView>(true) != null;
            GameObject compatibilityBoard = null;
            if (!rootPrefabContainsBoard && Object.FindFirstObjectByType<BoardView>() == null)
                compatibilityBoard = prefabs.Spawn(prefabs.boardPrefab, "Board");

            var gameRoot = prefabs.Spawn(prefabs.gameRootPrefab, "GameRoot (Runtime)");
            if (compatibilityBoard != null)
                compatibilityBoard.transform.SetParent(gameRoot.transform, false);
        }
    }
}

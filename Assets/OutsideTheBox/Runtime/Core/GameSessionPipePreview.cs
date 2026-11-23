using System;
using System.Collections;
using UnityEngine;

namespace OutsideTheBox
{
    // This file and its prefab contain the experiment. One GameConfig flag bypasses
    // both presentation hooks, keeping the original completion/loading behaviour.
    public sealed partial class GameSession
    {
        private bool pipeDepartureReady;
        private bool pipeArrivalRequested;
        private bool pipePreviewPlaying;
        private LevelPipeTransitionBase pipePreview;
        private SpriteRenderer pipeHiddenBall;
        internal bool IsPipePreviewBusy => pipeArrivalRequested || pipePreviewPlaying;
        private bool PipePreviewEnabled => config != null && config.previewPipeLevelTransitions &&
            !(SaveService.Data?.reduceMotion ?? false);

        private LevelPipeTransitionBase GetPipePreview()
        {
            if (ui != null) pipePreview = ui.GetPipePreview();
            if (pipePreview is LevelPipeTransition3D cinematic && boardView != null)
                cinematic.SetBoardSource(boardView.transform, boardView.TransitionBoardLocalSize,
                    boardView.CellWorld(level.exit), config.exitRadius * Mathf.Abs(boardView.transform.lossyScale.y));
            return pipePreview;
        }

        private IEnumerator PreviewPipeDeparture()
        {
            pipeDepartureReady = false;
            if (!PipePreviewEnabled || boardView == null) yield break;
            while (systemModalBlocked) yield return null;
            ui?.PreparePipePreviewLayout();
            var view = GetPipePreview();
            if (view == null) yield break;
            // Completion, currency and suspended-run clearing already happened.
            // Flush them before a test cinematic can be interrupted by app exit.
            SaveService.Save();
            pipePreviewPlaying = true;
            yield return RunPipePreviewSafely(view.Play(false, Camera.main, boardView.TransitionBallRenderer,
                boardView.CellWorld(level.exit), boardView.TransitionBallWorldDiameter, () => systemModalBlocked));
            pipeDepartureReady = view.Completed;
        }

        private void RequestPipeArrival()
        {
            pipeArrivalRequested = pipeDepartureReady && PipePreviewEnabled;
            pipeDepartureReady = false;
        }

        private void RequestMenuPipeArrival()
        {
            // Menu starts have no preceding hole departure. Reuse the authored
            // arrival prefab, including Reduce Motion and the rollback toggle.
            pipeArrivalRequested = PipePreviewEnabled;
            pipeDepartureReady = false;
        }

        private void HidePipeArrivalBall()
        {
            if (!pipeArrivalRequested || boardView == null) return;
            pipeHiddenBall = boardView.TransitionBallRenderer;
            if (pipeHiddenBall != null) pipeHiddenBall.forceRenderingOff = true;
            // Loading fades out before the safe-area/layout wait finishes. Hide
            // the flat board now so it cannot flash before its 3D entrance.
            if (GetPipePreview() is LevelPipeTransition3D cinematic)
                cinematic.HideBoardUntilArrival();
        }

        private IEnumerator PreviewPendingPipeArrival()
        {
            if (!pipeArrivalRequested) yield break;
            pipePreviewPlaying = true;
            pipeArrivalRequested = false;
            try
            {
                while (ui != null && ui.IsPipePreviewLoading) yield return null;
                while (systemModalBlocked) yield return null;
                // Loading dismissal restores the native banner. Let its reserved
                // height settle before projecting the spawn into the UI overlay.
                yield return null;
                ui?.PreparePipePreviewLayout();
                boardView?.RefreshDisplayLayout();
                if (running && PipePreviewEnabled)
                {
                    var view = GetPipePreview();
                    if (view != null)
                    {
                        // Pause physics until the visual ball has reached the exact
                        // authored spawn position. Its coordinates never move here.
                        yield return RunPipePreviewSafely(view.Play(true, Camera.main, boardView.TransitionBallRenderer,
                            boardView.TransitionBallWorldPosition, boardView.TransitionBallWorldDiameter,
                            () => systemModalBlocked, RestorePipeBall));
                    }
                }
            }
            finally
            {
                // Also release the pre-arrival visibility guard if animation was
                // skipped, cancelled, or could not start after loading.
                if (pipePreview != null) pipePreview.Hide();
                pipePreviewPlaying = false;
                RestorePipeBall();
                if (boardView != null)
                {
                    if (allowAttemptRecording && running) ui?.TryShowTutorials(level);
                    var canPlay = running && !paused && !tutorialPaused && !timedOut && !systemModalBlocked;
                    boardView.SetSimulationPaused(!canPlay);
                    if (magnetInput != null) magnetInput.Enabled = canPlay;
                    if (canPlay)
                    {
                        RecordAttemptIfNeeded();
                        // A menu resume already has an attempt; restart its play
                        // clock here so the cinematic is excluded from telemetry.
                        activePlayStartedAt = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private IEnumerator RunPipePreviewSafely(IEnumerator animation)
        {
            try
            {
                while (true)
                {
                    object current;
                    try
                    {
                        if (!animation.MoveNext()) break;
                        current = animation.Current;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"Pipe preview skipped: {exception.Message}");
                        break;
                    }
                    yield return current;
                }
            }
            finally
            {
                (animation as IDisposable)?.Dispose();
                if (pipePreview != null) pipePreview.Hide();
                pipePreviewPlaying = false;
                RestorePipeBall();
            }
        }

        private void RestorePipeBall()
        {
            if (pipeHiddenBall != null) pipeHiddenBall.forceRenderingOff = false;
            pipeHiddenBall = null;
        }

        private void CancelPipePreview(bool preserveArrivalRequest = false)
        {
            if (pipePreview != null) pipePreview.Hide();
            pipePreviewPlaying = false;
            pipeDepartureReady = false;
            if (!preserveArrivalRequest) pipeArrivalRequested = false;
            RestorePipeBall();
        }
    }

    public sealed partial class GameUIController
    {
        private LevelPipeTransitionBase pipePreviewView;
        private PipeTransitionPreviewStyle pipePreviewStyle;
        internal bool IsPipePreviewLoading => loadingOverlay != null && loadingOverlay.activeSelf;
        internal void PreparePipePreviewLayout()
        {
            if (safeRoot != null) safeRoot.GetComponent<SafeAreaFitter>()?.Apply();
            Canvas.ForceUpdateCanvases();
        }
        internal LevelPipeTransitionBase GetPipePreview()
        {
            var style = session.Config.pipeTransitionPreviewStyle;
            var correctViewType = style == PipeTransitionPreviewStyle.ThreeDimensional
                ? pipePreviewView is LevelPipeTransition3D : pipePreviewView is LevelPipeTransition;
            if (pipePreviewView != null && pipePreviewStyle == style && correctViewType) return pipePreviewView;
            if (safeRoot == null || prefabs == null) return null;
            var prefab = style == PipeTransitionPreviewStyle.ThreeDimensional
                ? prefabs.levelPipeTransition3DPrefab : prefabs.levelPipeTransitionPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"Pipe preview {style} prefab is missing. Reimport LevelPipeTransition3D and check the prefab registry.");
                return null;
            }
            if (pipePreviewView != null)
            {
                pipePreviewView.Hide();
                Destroy(pipePreviewView.gameObject);
            }
            var instance = prefabs.Spawn(prefab, "Level Pipe Preview", safeRoot);
            pipePreviewView = instance.GetComponent<LevelPipeTransitionBase>();
            pipePreviewStyle = style;
            return pipePreviewView;
        }
    }

    public partial class BoardViewImplementation
    {
        internal Vector2 TransitionBoardLocalSize => new(BoardWidth + config.glassFramePadding * 2f,
            BoardHeight + config.glassFramePadding * 2f);
        internal SpriteRenderer TransitionBallRenderer => ballTransform != null ? ballTransform.GetComponent<SpriteRenderer>() : null;
        // Resumed runs can be between cells; never animate to a rounded grid cell.
        internal Vector3 TransitionBallWorldPosition => ballTransform != null ? ballTransform.position :
            transform.TransformPoint(BallPosition);
        internal float TransitionBallWorldDiameter => config.ballRadius * 2f *
            (ballArtSprite != null ? config.ballArtScale : 1f) * Mathf.Abs(transform.lossyScale.y) *
            (TransitionBallRenderer?.sprite != null ? TransitionBallRenderer.sprite.bounds.size.y : 1f);
    }
}

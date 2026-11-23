using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace OutsideTheBox
{
    public sealed partial class GameUIController
    {
        private readonly InternetConnectionState connection = new();
        private GameObject offlineDialog;
        private GameObject quitDialog;
        private ServiceScreenView offlineView;
        private ServiceScreenView quitView;
        private string quitPrompt;
        private string offlineReadyMessage;
        private TMP_Text connectionStatus;
        private Button retryConnectionButton;
        private Coroutine connectionMonitor;
        private UnityWebRequest activeConnectionProbe;
        private float nextConnectionCheck;
        private bool systemDialogsReady;
        private bool quitInProgress;
        private bool quitApproved = false;

        private bool OfflineVisible => offlineDialog != null && offlineDialog.activeSelf;
        private bool QuitVisible => quitDialog != null && quitDialog.activeSelf;

        private void InitializeSystemDialogs()
        {
            if (systemDialogsReady) return;
            offlineView = SpawnServiceScreen(prefabs.offlinePanelPrefab, "Internet Connection", safeRoot);
            offlineDialog = offlineView.gameObject;
            connectionStatus = offlineView.status;
            retryConnectionButton = offlineView.primaryButton;
            offlineReadyMessage = connectionStatus != null ? connectionStatus.text : string.Empty;
            BindServiceButton(offlineView.primaryButton, RetryInternet);
            BindServiceButton(offlineView.secondaryButton, ShowQuitConfirmation);
            quitView = SpawnServiceScreen(prefabs.quitConfirmationPrefab, "Quit Confirmation", safeRoot);
            quitDialog = quitView.gameObject;
            quitPrompt = quitView.message.text;
            BindServiceButton(quitView.primaryButton, CancelQuit);
            BindServiceButton(quitView.secondaryButton, ConfirmQuit);
            systemDialogsReady = true;
            if (!Application.isPlaying) return;
            Application.wantsToQuit += InterceptQuit;
            connectionMonitor = StartCoroutine(MonitorInternet());
        }

        // Startup remains under the existing loading cover until an internet check succeeds.
        public async Task WaitForInternetAsync()
        {
            while (this != null && isActiveAndEnabled &&
                   LiveOpsConfigStore.Current.onlineRequired && !connection.IsOnline)
                await Task.Delay(100);
        }

        private IEnumerator MonitorInternet()
        {
            while (true)
            {
                var settings = LiveOpsConfigStore.Current;
                if (!settings.onlineRequired)
                {
                    SetOfflineDialogVisible(false);
                    yield return new WaitForSecondsRealtime(0.25f);
                    continue;
                }

                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    connection.RecordProbe(false, false, settings.connectionFailureThreshold);
                    SetOfflineDialogVisible(true);
                    SetConnectionChecking(false);
                    // A removed network path is handled immediately, including during a request.
                    nextConnectionCheck = 0f;
                    yield return new WaitForSecondsRealtime(0.25f);
                    continue;
                }
                if (Time.realtimeSinceStartup < nextConnectionCheck)
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                    continue;
                }

                SetConnectionChecking(true);
                var success = false;
                // A second endpoint prevents one unavailable probe host from blocking the game.
                foreach (var endpoint in new[] { settings.connectionCheckUrl, settings.connectionFallbackUrl })
                {
                    using (var request = UnityWebRequest.Get(endpoint))
                    {
                        activeConnectionProbe = request;
                        request.timeout = settings.connectionTimeoutSeconds;
                        request.redirectLimit = 0; // A captive portal is not an internet connection.
                        request.SetRequestHeader("Cache-Control", "no-cache");
                        var operation = request.SendWebRequest();
                        while (!operation.isDone)
                        {
                            if (Application.internetReachability == NetworkReachability.NotReachable)
                            {
                                request.Abort();
                                connection.RecordProbe(false, false, settings.connectionFailureThreshold);
                                SetOfflineDialogVisible(true);
                                break;
                            }
                            yield return null;
                        }
                        success = InternetConnectionState.IsSuccessfulProbe(request.responseCode,
                            request.result == UnityWebRequest.Result.Success);
                        activeConnectionProbe = null;
                    }
                    if (success || Application.internetReachability == NetworkReachability.NotReachable) break;
                }
                var wasOnline = connection.IsOnline;
                connection.RecordProbe(success,
                    Application.internetReachability != NetworkReachability.NotReachable,
                    settings.connectionFailureThreshold);
                if (wasOnline != connection.IsOnline)
                    AnalyticsService.Log(connection.IsOnline ? "internet_restored" : "internet_lost");
                SetOfflineDialogVisible(!connection.IsOnline);
                SetConnectionChecking(false);
                nextConnectionCheck = Time.realtimeSinceStartup +
                    (connection.IsOnline ? settings.connectionCheckSeconds : settings.connectionRetrySeconds);
            }
        }

        private void SetConnectionChecking(bool checking)
        {
            if (retryConnectionButton != null) retryConnectionButton.interactable = !checking;
            if (connectionStatus != null) connectionStatus.text = checking
                ? "CHECKING CONNECTION..." : offlineReadyMessage;
        }

        private void RetryInternet()
        {
            nextConnectionCheck = 0f;
            SetConnectionChecking(true);
            AnalyticsService.Log("internet_retry");
        }

        private void SetOfflineDialogVisible(bool visible)
        {
            if (offlineDialog == null || OfflineVisible == visible) return;
            offlineDialog.SetActive(visible);
            if (visible)
            {
                AnimateSystemDialog(offlineView);
                if (UnityEngine.EventSystems.EventSystem.current != null && !QuitVisible)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(retryConnectionButton.gameObject);
            }
            RefreshSystemModalBlock();
        }

        private void ShowQuitConfirmation()
        {
            if (quitInProgress || session == null) return;
            if (quitView == null) return;
            if (QuitVisible) return;
            quitView.message.text = quitPrompt;
            quitDialog.SetActive(true);
            AnimateSystemDialog(quitView);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(
                    quitView.primaryButton.gameObject);
            RefreshSystemModalBlock();
            AnalyticsService.Log("quit_confirmation_opened");
        }

        private void CancelQuit()
        {
            if (quitInProgress || quitDialog == null) return;
            quitDialog.SetActive(false);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(
                    OfflineVisible ? retryConnectionButton.gameObject : null);
            RefreshSystemModalBlock();
            AnalyticsService.Log("quit_cancelled");
        }

        private async void ConfirmQuit()
        {
            if (quitInProgress) return;
            quitInProgress = true;
            if (quitView != null) quitView.overlayGroup.interactable = false;
            var message = quitView != null ? quitView.message : null;
            if (message != null) message.text = "Saving your progress...";
            AnalyticsService.Log("quit_confirmed");
            try { await session.SaveBeforeQuitAsync(); }
            catch (Exception exception) { Debug.LogWarning($"Exit save: {exception.Message}"); }
            if (this == null) return;
#if UNITY_EDITOR
            // Do not terminate the Editor or discard an unsaved prefab editing session.
            quitInProgress = false;
            if (quitView != null) quitView.overlayGroup.interactable = true;
            CancelQuit();
            ShowToast("QUIT CONFIRMED - THE APP CLOSES ON YOUR PHONE");
#else
            quitApproved = true;
            Application.Quit();
#endif
        }

        private bool InterceptQuit()
        {
            if (quitApproved || !Application.isPlaying) return true;
            ShowQuitConfirmation();
            return false;
        }

        private bool HandleSystemDialogBack()
        {
            if (QuitVisible) { CancelQuit(); return true; }
            if (OfflineVisible) { ShowQuitConfirmation(); return true; }
            return quitInProgress;
        }

        private void RefreshSystemModalBlock() => session?.SetSystemModalBlocked(OfflineVisible || QuitVisible);

        private ServiceScreenView SpawnServiceScreen(GameObject prefab, string name, Transform parent)
        {
            var existing = parent.Find(name);
            var overlay = existing != null ? existing.gameObject : prefabs.Spawn(prefab, name, parent);
            overlay.SetActive(false);
            var view = overlay.GetComponent<ServiceScreenView>();
            if (view == null || view.overlayGroup == null || view.content == null || view.contentGroup == null ||
                view.primaryButton == null || view.secondaryButton == null || view.message == null)
                throw new InvalidOperationException($"{name} prefab has incomplete ServiceScreenView bindings. Fix the prefab in the Inspector.");
            return view;
        }

        private static void BindServiceButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(button);
            button.onClick.AddListener(action);
        }

        private void AnimateSystemDialog(ServiceScreenView view)
        {
            var card = view.content;
            StopCoroutineIfRunning(card.gameObject);
            var group = view.contentGroup;
            group.alpha = 1f;
            group.interactable = group.blocksRaycasts = true;
            card.localScale = Vector3.one;
            if (SaveService.Data?.reduceMotion ?? false) return;
            var routine = StartCoroutine(AnimateSystemCard(card, group));
            panelAnimations[card.gameObject.GetInstanceID()] = routine;
        }

        private IEnumerator AnimateSystemCard(Transform card, CanvasGroup group)
        {
            var settings = LiveOpsConfigStore.Current;
            var duration = Mathf.Max(0.01f, settings.uiAnimationSeconds);
            var elapsed = 0f;
            while (card != null && card.gameObject.activeInHierarchy && elapsed < duration)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                group.alpha = eased;
                card.localScale = Vector3.one * Mathf.Lerp(settings.uiPopupStartScale, 1f, eased);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (card == null) yield break;
            group.alpha = 1f;
            card.localScale = Vector3.one;
            panelAnimations.Remove(card.gameObject.GetInstanceID());
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) nextConnectionCheck = 0f;
        }

        private void OnDestroy()
        {
            ReleaseGemStoreBindings();
            Application.wantsToQuit -= InterceptQuit;
            var pendingProbe = activeConnectionProbe;
            activeConnectionProbe = null;
            if (pendingProbe != null) pendingProbe.Abort();
            if (connectionMonitor != null) StopCoroutine(connectionMonitor);
            if (pendingProbe != null)
            {
                pendingProbe.Dispose();
            }
        }
    }
}

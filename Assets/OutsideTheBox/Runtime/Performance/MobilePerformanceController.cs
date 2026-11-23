using UnityEngine;

namespace OutsideTheBox
{
    public enum MobileDeviceTier
    {
        Low,
        Balanced,
        High
    }

    /// <summary>
    /// Selects an appropriate mobile profile and gently adjusts rendering when a device
    /// cannot sustain it. UI remains at native resolution because it uses an overlay canvas.
    /// </summary>
    public sealed class MobilePerformanceController : MonoBehaviour
    {
        private PerformanceMode playerMode;
        private MobileDeviceTier tier;
        private float desiredRenderScale = 1f;
        private float currentRenderScale = 1f;
        private float sampleTime;
        private int sampleFrames;
        private int slowSamples;
        private int healthySamples;
        private int targetFrameRate = 60;
        private Vector2Int lastScreenSize;
        private BoardView boardView;
        private bool initialized;
        private bool memoryPressure;
        private bool suspended;

        public MobileDeviceTier Tier => tier;
        public float RenderScale => currentRenderScale;
        public int TargetFrameRate => targetFrameRate;

        private void OnEnable() => Application.lowMemory += HandleLowMemory;

        private void OnDisable() => Application.lowMemory -= HandleLowMemory;

        public void Apply(PerformanceMode mode)
        {
            playerMode = mode;
            if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
            var settings = LiveOpsConfigStore.Current ?? new LiveOpsConfigData();
            settings.Normalize();

            tier = SelectTier(settings, mode);
            targetFrameRate = tier switch
            {
                MobileDeviceTier.Low => settings.mobileBatteryFrameRate,
                MobileDeviceTier.High => settings.mobileSmoothFrameRate,
                _ => settings.mobileBalancedFrameRate
            };
            desiredRenderScale = tier switch
            {
                MobileDeviceTier.Low => settings.mobileLowEndRenderScale,
                MobileDeviceTier.High => settings.mobileHighEndRenderScale,
                _ => settings.mobileBalancedRenderScale
            };

            if (memoryPressure)
            {
                tier = MobileDeviceTier.Low;
                targetFrameRate = settings.mobileBatteryFrameRate;
                desiredRenderScale = settings.mobileMinimumRenderScale;
            }

            targetFrameRate = ClampToDisplay(targetFrameRate);
            currentRenderScale = Mathf.Clamp(desiredRenderScale,
                settings.mobileMinimumRenderScale, 1f);
            ApplyRuntimeProfile(settings, true);
            ResetSamples();
            initialized = true;
            RefreshDisplayLayout();
        }

        private void Update()
        {
            if (!initialized || suspended || !Application.isMobilePlatform) return;
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
                RefreshDisplayLayout();
            var settings = LiveOpsConfigStore.Current;
            if (settings == null || !settings.mobileAdaptivePerformanceEnabled) return;

            sampleTime += Time.unscaledDeltaTime;
            sampleFrames++;
            if (sampleTime < settings.mobilePerformanceSampleSeconds) return;

            var measuredFps = sampleFrames / Mathf.Max(0.01f, sampleTime);
            var slow = measuredFps < targetFrameRate * settings.mobileSlowFrameThreshold;
            var healthy = measuredFps > targetFrameRate * settings.mobileRecoveryFrameThreshold;
            slowSamples = slow ? slowSamples + 1 : 0;
            healthySamples = healthy ? healthySamples + 1 : 0;

            if (slowSamples >= settings.mobileSlowSampleCount)
            {
                Downshift(settings);
                slowSamples = 0;
                healthySamples = 0;
            }
            else if (!memoryPressure && healthySamples >= settings.mobileRecoverySampleCount)
            {
                Recover(settings);
                healthySamples = 0;
            }

            sampleTime = 0f;
            sampleFrames = 0;
        }

        private MobileDeviceTier SelectTier(LiveOpsConfigData settings, PerformanceMode mode)
        {
            if (mode == PerformanceMode.BatterySaver) return MobileDeviceTier.Low;
            if (mode == PerformanceMode.Smooth) return MobileDeviceTier.High;

            var memory = SystemInfo.systemMemorySize;
            var gpuMemory = SystemInfo.graphicsMemorySize;
            var cores = SystemInfo.processorCount;
            var lowMemory = memory > 0 && memory <= settings.mobileLowMemoryThresholdMb;
            var lowGpu = gpuMemory > 0 && gpuMemory <= settings.mobileLowGpuMemoryThresholdMb;
            var lowCpu = cores > 0 && cores <= settings.mobileLowProcessorCount;
            // Some Android vendors report shared GPU memory inaccurately, so require two
            // low-end signals instead of downgrading a phone from one unreliable value.
            var lowSignals = (lowMemory ? 1 : 0) + (lowGpu ? 1 : 0) + (lowCpu ? 1 : 0);
            if (lowSignals >= 2) return MobileDeviceTier.Low;

            var highMemory = memory <= 0 || memory >= settings.mobileHighMemoryThresholdMb;
            var highGpu = gpuMemory <= 0 || gpuMemory >= settings.mobileHighGpuMemoryThresholdMb;
            var highCpu = cores <= 0 || cores >= settings.mobileHighProcessorCount;
            return highMemory && highGpu && highCpu ? MobileDeviceTier.High : MobileDeviceTier.Balanced;
        }

        private void ApplyRuntimeProfile(LiveOpsConfigData settings, bool applyQuality)
        {
            QualitySettings.vSyncCount = Application.isMobilePlatform ? 0 : 1;
            Application.targetFrameRate = Mathf.Clamp(targetFrameRate, 24, 120);
            Time.fixedDeltaTime = 1f / Mathf.Clamp(settings.mobilePhysicsTickRate, 30, 120);
            Time.maximumDeltaTime = Mathf.Max(Time.fixedDeltaTime * 3f, 0.05f);

            if (applyQuality)
            {
                var quality = tier switch
                {
                    MobileDeviceTier.Low => settings.mobileLowQualityLevel,
                    MobileDeviceTier.High => settings.mobileHighQualityLevel,
                    _ => settings.mobileBalancedQualityLevel
                };
                quality = Mathf.Clamp(quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                if (QualitySettings.GetQualityLevel() != quality)
                    QualitySettings.SetQualityLevel(quality, true);
                QualitySettings.antiAliasing = 0;
                QualitySettings.realtimeReflectionProbes = false;
            }

            currentRenderScale = Mathf.Clamp(currentRenderScale,
                settings.mobileMinimumRenderScale, 1f);
            ScalableBufferManager.ResizeBuffers(currentRenderScale, currentRenderScale);
        }

        private void Downshift(LiveOpsConfigData settings)
        {
            var minimum = settings.mobileMinimumRenderScale;
            if (currentRenderScale > minimum + 0.001f)
            {
                currentRenderScale = Mathf.Max(minimum,
                    currentRenderScale - settings.mobileRenderScaleStep);
                ApplyRuntimeProfile(settings, false);
                return;
            }

            if (playerMode == PerformanceMode.Auto && targetFrameRate > settings.mobileBatteryFrameRate)
            {
                targetFrameRate = ClampToDisplay(settings.mobileBatteryFrameRate);
                ApplyRuntimeProfile(settings, false);
            }
        }

        private void Recover(LiveOpsConfigData settings)
        {
            var profileTarget = tier switch
            {
                MobileDeviceTier.Low => settings.mobileBatteryFrameRate,
                MobileDeviceTier.High => settings.mobileSmoothFrameRate,
                _ => settings.mobileBalancedFrameRate
            };
            profileTarget = ClampToDisplay(profileTarget);
            if (targetFrameRate < profileTarget)
            {
                targetFrameRate = profileTarget;
                ApplyRuntimeProfile(settings, false);
                return;
            }

            if (currentRenderScale >= desiredRenderScale - 0.001f) return;
            currentRenderScale = Mathf.Min(desiredRenderScale,
                currentRenderScale + settings.mobileRenderScaleStep);
            ApplyRuntimeProfile(settings, false);
        }

        private void HandleLowMemory()
        {
            memoryPressure = true;
            var settings = LiveOpsConfigStore.Current ?? new LiveOpsConfigData();
            tier = MobileDeviceTier.Low;
            desiredRenderScale = settings.mobileMinimumRenderScale;
            currentRenderScale = desiredRenderScale;
            targetFrameRate = ClampToDisplay(settings.mobileBatteryFrameRate);
            ApplyRuntimeProfile(settings, true);
            Resources.UnloadUnusedAssets();
        }

        private static int ClampToDisplay(int requested)
        {
            var refresh = (float)Screen.currentResolution.refreshRateRatio.value;
            if (refresh <= 1f) return requested;
            return Mathf.Max(24, Mathf.Min(requested, Mathf.RoundToInt(refresh)));
        }

        private void OnApplicationPause(bool paused)
        {
            suspended = paused;
            if (!paused) ResetSamples();
        }

        private void ResetSamples()
        {
            sampleTime = 0f;
            sampleFrames = 0;
            slowSamples = 0;
            healthySamples = 0;
        }

        private void RefreshDisplayLayout()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            boardView?.RefreshDisplayLayout();
        }
    }
}

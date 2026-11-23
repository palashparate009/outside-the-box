using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    public sealed class FeedbackService : MonoBehaviour
    {
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip moveClip;
        [SerializeField] private AudioClip blockedClip;
        [SerializeField] private AudioClip polarityClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip[] chapterMusic;

        private readonly Dictionary<int, AudioClip> generatedMusic = new();
        private GameConfig config;
        private int activeChapter = -1;

        private void Awake()
        {
            config = Resources.Load<GameConfig>("OutsideTheBox/Configs/GameConfig");
            if (effectsSource == null) effectsSource = GetComponent<AudioSource>();
            if (effectsSource == null) effectsSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            effectsSource.spatialBlend = 0f;
            moveClip ??= config?.moveAudioClip;
            blockedClip ??= config?.collisionAudioClip;
            polarityClip ??= config?.polarityAudioClip;
            winClip ??= config?.winAudioClip;
            if (moveClip == null) moveClip = CreateSweep("Magnet Move", 440f, 620f, 0.055f, 0.16f, 0f);
            if (blockedClip == null) blockedClip = CreateSweep("Glass Impact", 170f, 72f, 0.11f, 0.22f, 0.24f);
            if (polarityClip == null) polarityClip = CreateSweep("Polarity Shift", 360f, 920f, 0.14f, 0.18f, 0.03f);
            if (winClip == null) winClip = CreateSuccessSound();
        }

        public void ApplySettings()
        {
            if (SaveService.Data == null) return;
            if (effectsSource != null) effectsSource.mute = !SaveService.Data.soundEnabled;
            if (musicSource != null) musicSource.mute = !SaveService.Data.musicEnabled;
            if (effectsSource != null) effectsSource.volume = config != null ? config.effectsVolume : 0.72f;
            if (musicSource != null) musicSource.volume = config != null ? config.musicVolume : 0.18f;
        }

        public void SetChapter(int chapterIndex)
        {
            if (musicSource == null || activeChapter == chapterIndex) return;
            activeChapter = chapterIndex;
            AudioClip clip = null;
            if (chapterMusic != null && chapterIndex >= 0 && chapterIndex < chapterMusic.Length)
                clip = chapterMusic[chapterIndex];
            if (clip == null && config?.chapterMusicClips != null && chapterIndex >= 0 &&
                chapterIndex < config.chapterMusicClips.Length)
                clip = config.chapterMusicClips[chapterIndex];
            if (clip == null && (config == null || config.proceduralMusicFallback))
            {
                if (!generatedMusic.TryGetValue(chapterIndex, out clip))
                {
                    clip = CreateAmbientLoop(chapterIndex, config != null ? config.ambientLoopSeconds : 8f);
                    generatedMusic[chapterIndex] = clip;
                }
            }
            musicSource.clip = clip;
            if (clip != null && !musicSource.isPlaying) musicSource.Play();
        }

        public void Move() => Play(moveClip);
        public void Polarity() => Play(polarityClip);
        public void Blocked()
        {
            Play(blockedClip);
            Vibrate();
        }
        public void Win()
        {
            Play(winClip);
            Vibrate();
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && effectsSource != null && (SaveService.Data?.soundEnabled ?? true))
                effectsSource.PlayOneShot(clip);
        }

        private static void Vibrate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (SaveService.Data?.vibrationEnabled ?? false) Handheld.Vibrate();
#endif
        }

        private static AudioClip CreateSweep(string clipName, float startFrequency, float endFrequency,
            float seconds, float volume, float noiseAmount)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(seconds * sampleRate);
            var data = new float[samples];
            var phase = 0f;
            var random = new System.Random(clipName.GetHashCode());
            for (var i = 0; i < samples; i++)
            {
                var progress = i / (float)Mathf.Max(1, samples - 1);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += 2f * Mathf.PI * frequency / sampleRate;
                var envelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.35f);
                var noise = ((float)random.NextDouble() * 2f - 1f) * noiseAmount;
                data[i] = (Mathf.Sin(phase) * (1f - noiseAmount) + noise) * volume * envelope;
            }
            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSuccessSound()
        {
            const int sampleRate = 22050;
            const float seconds = 0.42f;
            var samples = Mathf.CeilToInt(seconds * sampleRate);
            var data = new float[samples];
            var notes = new[] { 523.25f, 659.25f, 783.99f };
            for (var i = 0; i < samples; i++)
            {
                var progress = i / (float)samples;
                var noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(progress * notes.Length));
                var local = progress * notes.Length - noteIndex;
                var envelope = Mathf.Sin(local * Mathf.PI) * (1f - progress * 0.25f);
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * i / sampleRate) * 0.16f * envelope;
            }
            var clip = AudioClip.Create("Pocket Success", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAmbientLoop(int chapterIndex, float requestedSeconds)
        {
            const int sampleRate = 22050;
            var seconds = Mathf.Clamp(requestedSeconds, 4f, 20f);
            var samples = Mathf.CeilToInt(seconds * sampleRate);
            var data = new float[samples];
            var roots = new[] { 55f, 60f, 65f, 70f, 75f, 80f, 50f };
            var root = roots[Mathf.Abs(chapterIndex) % roots.Length];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var loopPhase = t / seconds * Mathf.PI * 2f;
                var breathe = 0.62f + Mathf.Sin(loopPhase) * 0.16f + Mathf.Sin(loopPhase * 2f + 1.2f) * 0.08f;
                var chord = Mathf.Sin(2f * Mathf.PI * root * t) * 0.52f +
                            Mathf.Sin(2f * Mathf.PI * root * 1.5f * t) * 0.28f +
                            Mathf.Sin(2f * Mathf.PI * root * 2f * t) * 0.2f;
                data[i] = chord * breathe * 0.055f;
            }
            var clip = AudioClip.Create($"Chapter {chapterIndex + 1} Ambient", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

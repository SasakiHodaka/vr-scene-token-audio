using System.Collections.Generic;
using UnityEngine;

namespace SceneTokenAudio
{
    [DisallowMultipleComponent]
    public sealed class SpatialAudioRendererAdapter : MonoBehaviour
    {
        public AudioSource audioSource;
        [Min(0.001f)] public float smoothingSeconds = 0.1f;
        private static readonly Dictionary<AudioSource, SpatialAudioRendererAdapter> Owners = new Dictionary<AudioSource, SpatialAudioRendererAdapter>();
        private AudioSource capturedSource;
        private AudioLowPassFilter filter;
        private bool captured, createdFilter, originalFilterEnabled;
        private float originalVolume, originalCutoff;
        private AudioControlParameters target;
        public float AppliedVolume => capturedSource ? capturedSource.volume : audioSource ? audioSource.volume : 0f;
        public float AppliedLowPassHz => filter && filter.enabled ? filter.cutoffFrequency : 22000f;
        public bool IsControlling => captured;

        public bool Apply(AudioControlParameters parameters)
        {
            if (!isActiveAndEnabled) return false;
            if (!Finite(parameters.gainCorrection) || !Finite(parameters.occlusionGain) || !Finite(parameters.lowPassHz)) return false;
            if (!captured && !Capture()) return false;
            target = parameters;
            return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private bool Capture()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
            if (!audioSource) return false;
            if (Owners.TryGetValue(audioSource, out var owner) && owner && owner != this)
            {
                Debug.LogError("Only one Scene Token renderer may own an AudioSource.", this);
                return false;
            }
            capturedSource = audioSource;
            Owners[capturedSource] = this;
            originalVolume = capturedSource.volume;
            filter = capturedSource.GetComponent<AudioLowPassFilter>();
            if (!filter)
            {
                filter = capturedSource.gameObject.AddComponent<AudioLowPassFilter>();
                filter.enabled = false;
                createdFilter = true;
            }
            originalFilterEnabled = filter.enabled;
            originalCutoff = filter.cutoffFrequency;
            filter.cutoffFrequency = originalFilterEnabled ? originalCutoff : 22000f;
            filter.enabled = true;
            captured = true;
            return true;
        }

        private void Update()
        {
            if (!captured || !capturedSource || !filter) return;
            float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.001f, smoothingSeconds));
            float gain = Mathf.Clamp01(target.gainCorrection * target.occlusionGain);
            capturedSource.volume = Mathf.Lerp(capturedSource.volume, originalVolume * gain, blend);
            float cutoff = Mathf.Clamp(target.lowPassHz, 10f, 22000f);
            if (originalFilterEnabled) cutoff = Mathf.Min(cutoff, originalCutoff);
            filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, cutoff, blend);
        }

        public void Restore()
        {
            if (!captured) return;
            if (capturedSource)
            {
                capturedSource.volume = originalVolume;
                Owners.Remove(capturedSource);
            }
            if (filter)
            {
                filter.cutoffFrequency = originalCutoff;
                filter.enabled = originalFilterEnabled;
            }
            captured = false;
        }
        private void OnDisable() => Restore();
        private void OnDestroy()
        {
            Restore();
            if (createdFilter && filter) Destroy(filter);
        }
    }
}

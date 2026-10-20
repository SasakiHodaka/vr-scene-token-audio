using UnityEngine;

namespace SceneTokenAudio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GeneratedTestTone : MonoBehaviour
    {
        private AudioClip generatedClip;
        private void Start()
        {
            AudioSource source = GetComponent<AudioSource>();
            const int rate = 48000;
            float[] samples = new float[rate];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = 0.05f * (Mathf.Sin(2f * Mathf.PI * 440f * i / rate) + 0.3f * Mathf.Sin(2f * Mathf.PI * 4000f * i / rate));
            generatedClip = AudioClip.Create("SceneTokenReferenceTone", rate, 1, rate, false);
            generatedClip.SetData(samples, 0);
            source.clip = generatedClip;
            source.loop = true;
            source.Play();
        }
        private void OnDestroy() { if (generatedClip) Destroy(generatedClip); }
    }
}

namespace SceneTokenAudio
{
    public interface IAudioControlPolicy
    {
        AudioControlParameters Evaluate(SceneToken token);
    }

    public static class AudioPolicyMath
    {
        public static float Clamp01(float value) => float.IsNaN(value) ? 0f : System.Math.Max(0f, System.Math.Min(1f, value));
        public static AudioControlParameters Render(float occlusion, float relief)
        {
            // Unknown occlusion uses a conservative fully occluded fallback.
            float obstruction = occlusion < 0f || float.IsNaN(occlusion) ? 1f : Clamp01(occlusion);
            float remaining = obstruction * (1f - Clamp01(relief));
            return new AudioControlParameters
            {
                gainCorrection = 1f,
                occlusionGain = 1f - 0.8f * remaining,
                lowPassHz = 22000f - 20000f * remaining
            };
        }
    }
}

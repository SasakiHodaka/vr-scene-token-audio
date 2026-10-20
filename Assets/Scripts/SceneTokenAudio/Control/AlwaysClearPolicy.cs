namespace SceneTokenAudio
{
    public sealed class AlwaysClearPolicy : IAudioControlPolicy
    {
        public float Relief { get; }
        public AlwaysClearPolicy(float relief = 0.75f) { Relief = AudioPolicyMath.Clamp01(relief); }
        public AudioControlParameters Evaluate(SceneToken token) => AudioPolicyMath.Render(token.occlusion, Relief);
    }
}

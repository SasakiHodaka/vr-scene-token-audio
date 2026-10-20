namespace SceneTokenAudio
{
    public sealed class BaselinePolicy : IAudioControlPolicy
    {
        public AudioControlParameters Evaluate(SceneToken token) => AudioPolicyMath.Render(token.occlusion, 0f);
    }
}

namespace SceneTokenAudio
{
    public sealed class AdaptivePolicy : IAudioControlPolicy
    {
        public AudioControlParameters Evaluate(SceneToken token)
        {
            float importance = AudioPolicyMath.Clamp01(token.priority);
            if (token.taskPhase == TaskPhase.Emergency) importance = 1f;
            else if (importance >= 0.6f)
            {
                if (token.taskPhase == TaskPhase.Verification) importance += 0.15f;
                else if (token.taskPhase == TaskPhase.Assembly) importance += 0.05f;
                if (token.timeToDeadline >= 0f && token.timeToDeadline <= 10f) importance += 0.15f;
            }
            importance = AudioPolicyMath.Clamp01(importance);
            float relief = importance >= 0.95f ? 1f : importance >= 0.6f ? (importance - 0.6f) / 0.35f : 0f;
            return AudioPolicyMath.Render(token.occlusion, relief);
        }
    }
}

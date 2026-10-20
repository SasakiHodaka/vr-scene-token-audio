namespace SceneTokenAudio
{
    public sealed class AdaptiveAudioController
    {
        private readonly IAudioControlPolicy[] policies = { new BaselinePolicy(), new AlwaysClearPolicy(), new AdaptivePolicy() };
        public AudioControlParameters Evaluate(SceneToken token, AudioControlMode mode)
        {
            int index = (int)mode;
            if (index < 0 || index >= policies.Length) throw new System.ArgumentOutOfRangeException(nameof(mode));
            return policies[index].Evaluate(token);
        }
        public void ReplacePolicy(AudioControlMode mode, IAudioControlPolicy policy)
        {
            if (policy == null) throw new System.ArgumentNullException(nameof(policy));
            int index = (int)mode;
            if (index < 0 || index >= policies.Length) throw new System.ArgumentOutOfRangeException(nameof(mode));
            policies[index] = policy;
        }
    }
}

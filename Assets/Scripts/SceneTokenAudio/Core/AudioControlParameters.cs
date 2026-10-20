namespace SceneTokenAudio
{
    public struct AudioControlParameters
    {
        // Multipliers relative to the captured AudioSource gain; native distance rolloff remains active.
        public float gainCorrection;
        public float occlusionGain;
        public float lowPassHz;
    }
}

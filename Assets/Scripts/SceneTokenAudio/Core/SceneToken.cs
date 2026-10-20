using System;

namespace SceneTokenAudio
{
    // Wire DTO: no Unity types or renderer output. See docs/architecture.md for units.
    [Serializable]
    public struct TokenDirection
    {
        public float x, y, z;
        public TokenDirection(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }

    [Serializable]
    public struct SceneToken
    {
        public double timestamp;
        public string sourceId;
        public float distance;
        public TokenDirection relativeDirection;
        public float occlusion;
        public TaskPhase taskPhase;
        public float priority;
        public float timeToDeadline;
    }
}

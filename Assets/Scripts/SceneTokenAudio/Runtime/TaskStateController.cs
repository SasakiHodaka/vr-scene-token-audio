using UnityEngine;

namespace SceneTokenAudio
{
    public sealed class TaskStateController : MonoBehaviour
    {
        public TaskPhase taskPhase = TaskPhase.Assembly;
        [Range(0f, 1f)] public float priority = 0.2f;
        [Min(0f)] public float deadlineSeconds = 60f;
        public bool deadlineEnabled;
        private double deadline;
        private void OnEnable() => RestartDeadline();
        public void RestartDeadline() => deadline = Time.realtimeSinceStartupAsDouble + deadlineSeconds;
        public float TimeToDeadline => deadlineEnabled ? (float)System.Math.Max(0d, deadline - Time.realtimeSinceStartupAsDouble) : -1f;
    }
}

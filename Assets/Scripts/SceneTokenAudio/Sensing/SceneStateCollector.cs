using UnityEngine;

namespace SceneTokenAudio
{
    public struct SceneState
    {
        public double timestamp;
        public string sourceId;
        public Vector3 sourcePosition, listenerPosition, relativeDirection;
        public float distance, occlusion, priority, timeToDeadline;
        public TaskPhase taskPhase;
    }

    public sealed class SceneStateCollector : MonoBehaviour
    {
        public string sourceId = "source-1";
        public Transform listener;
        public AudioSource audioSource;
        // Optional pose source for tracked worker heads; no synthetic audio playback is required.
        public Transform sourceTransform;
        public TaskStateController task;
        public OcclusionDetector occlusionDetector;
        public SpatialAudioRendererAdapter rendererAdapter;

        public bool TryCollect(out SceneState state)
        {
            state = default;
            Transform origin = sourceTransform ? sourceTransform : audioSource ? audioSource.transform : null;
            if (!listener || !origin || string.IsNullOrWhiteSpace(sourceId)) return false;
            Vector3 delta = origin.position - listener.position;
            state = new SceneState
            {
                timestamp = Time.realtimeSinceStartupAsDouble,
                sourceId = sourceId,
                sourcePosition = origin.position,
                listenerPosition = listener.position,
                distance = delta.magnitude,
                relativeDirection = delta.magnitude <= 0.0001f ? Vector3.zero : listener.InverseTransformDirection(delta.normalized),
                occlusion = occlusionDetector ? occlusionDetector.Measure(listener, origin) : -1f,
                taskPhase = task ? task.taskPhase : TaskPhase.Unknown,
                priority = task ? task.priority : 0f,
                timeToDeadline = task ? task.TimeToDeadline : -1f
            };
            return true;
        }
    }
}

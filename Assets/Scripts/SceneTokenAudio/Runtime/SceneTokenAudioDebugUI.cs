using UnityEngine;

namespace SceneTokenAudio
{
    public sealed class SceneTokenAudioDebugUI : MonoBehaviour
    {
        public SceneTokenAudioManager manager;
        public TaskStateController task;
        public Transform wall;
        public Transform source;
        public bool show = true;
        private void OnGUI()
        {
            if (!show || !manager) return;
            GUILayout.BeginArea(new Rect(12, 12, 470, 650), GUI.skin.box);
            GUILayout.Label("Scene Token Audio — local test (desktop controls)");
            foreach (AudioControlMode candidate in System.Enum.GetValues(typeof(AudioControlMode)))
                if (GUILayout.Toggle(manager.mode == candidate, candidate.ToString(), GUI.skin.button)) manager.mode = candidate;
            bool enabledValue = GUILayout.Toggle(manager.controlEnabled, "Audio control enabled");
            if (enabledValue != manager.controlEnabled) manager.SetControlEnabled(enabledValue);
            if (task)
            {
                GUILayout.Label("Priority: " + task.priority.ToString("F2"));
                task.priority = GUILayout.HorizontalSlider(task.priority, 0f, 1f);
                foreach (TaskPhase phase in System.Enum.GetValues(typeof(TaskPhase)))
                    if (GUILayout.Toggle(task.taskPhase == phase, phase.ToString(), GUI.skin.button)) task.taskPhase = phase;
                task.deadlineEnabled = GUILayout.Toggle(task.deadlineEnabled, "Deadline enabled");
                if (GUILayout.Button("Restart deadline")) task.RestartDeadline();
            }
            if (wall)
            {
                GUILayout.Label("Wall X (0 blocks path)");
                Vector3 position = wall.position;
                position.x = GUILayout.HorizontalSlider(position.x, -4f, 4f);
                wall.position = position;
            }
            if (source)
            {
                GUILayout.Label("Source Z (distance)");
                Vector3 position = source.position;
                position.z = GUILayout.HorizontalSlider(position.z, 2f, 12f);
                source.position = position;
            }
            SceneToken token = manager.LatestToken;
            GUILayout.Label("Source: " + token.sourceId + " | distance: " + token.distance.ToString("F2") + "m");
            GUILayout.Label($"Direction: ({token.relativeDirection.x:F2}, {token.relativeDirection.y:F2}, {token.relativeDirection.z:F2})");
            GUILayout.Label($"Occlusion: {token.occlusion:F2} | remaining: {token.timeToDeadline:F1}s");
            GUILayout.Label($"Gain: {manager.LatestParameters.occlusionGain:F3} | LPF: {manager.LatestParameters.lowPassHz:F0}Hz");
            GUILayout.Label($"Processing: {manager.LatestProcessingMs:F3}ms | samples: {manager.SamplesProcessed}");
            if (!string.IsNullOrEmpty(manager.LastError)) GUILayout.Label(manager.LastError);
            if (manager.logger) GUILayout.Label("CSV: " + manager.logger.FilePath + "\nRows: " + manager.logger.RowsWritten);
            GUILayout.EndArea();
        }
    }
}

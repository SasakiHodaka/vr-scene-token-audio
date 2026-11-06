using UnityEngine;

namespace SceneTokenAudio.Research
{
    public sealed class AssemblyCaptureDebugUI : MonoBehaviour
    {
        public AssemblyCaptureRecorder recorder;
        public bool show = true;
        private void OnGUI()
        {
            if (!show || !recorder) return;
            GUILayout.BeginArea(new Rect(12, 12, 650, 200), GUI.skin.box);
            GUILayout.Label("Drone assembly capture — desktop status");
            GUILayout.Label("Events: " + recorder.RecordsWritten);
            var binder = GetComponent<DroneCaptureRuntimeBinder>();
            if (binder) GUILayout.Label("Runtime bindings: " + binder.BoundParts + " parts / " + binder.BoundActivities + " activities");
            GUILayout.Label("Listener: " + (recorder.audioListener ? recorder.audioListener.name : "not available"));
            GUILayout.Label("Output: " + (recorder.DirectoryPath ?? "Waiting for an observed event"));
            GUILayout.Label("CSV: events-audio.csv (callback observations, not independent ground truth)");
            if (!string.IsNullOrEmpty(recorder.LastError)) GUILayout.Label(recorder.LastError);
            GUILayout.EndArea();
        }
    }
}

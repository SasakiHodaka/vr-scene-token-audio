using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SceneTokenAudio
{
    public sealed class AudioEvaluationLogger : MonoBehaviour
    {
        public bool record = true;
        [Min(0.1f)] public float flushIntervalSeconds = 1f;
        public string FilePath { get; private set; }
        public int RowsWritten { get; private set; }
        public string LastError { get; private set; }
        private StreamWriter writer;
        private double nextFlush;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        public const string Header = "schema_version,timestamp,source_id,source_x,source_y,source_z,listener_x,listener_y,listener_z,distance_m,direction_x,direction_y,direction_z,occlusion,task_phase,priority,time_to_deadline_s,mode,gain_correction,occlusion_gain,target_lpf_hz,applied_volume,applied_lpf_hz,collection_ms,control_ms,total_ms,frame_ms";
        public static string Escape(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        private static string Number(double value) => value.ToString("R", Invariant);
        public static string FormatRow(SceneState state, SceneToken token, AudioControlMode mode,
            AudioControlParameters parameters, float volume, float cutoff, double collectionMs, double controlMs, double totalMs, float frameMs,
            bool observationOnly = false)
        {
            return string.Join(",", new[] {
                "1", Number(token.timestamp), Escape(token.sourceId),
                Number(state.sourcePosition.x), Number(state.sourcePosition.y), Number(state.sourcePosition.z),
                Number(state.listenerPosition.x), Number(state.listenerPosition.y), Number(state.listenerPosition.z),
                Number(token.distance), Number(token.relativeDirection.x), Number(token.relativeDirection.y), Number(token.relativeDirection.z),
                Number(token.occlusion), token.taskPhase.ToString(), Number(token.priority), Number(token.timeToDeadline), observationOnly ? "ObservationOnly" : mode.ToString(),
                observationOnly ? "" : Number(parameters.gainCorrection), observationOnly ? "" : Number(parameters.occlusionGain),
                observationOnly ? "" : Number(parameters.lowPassHz), observationOnly ? "" : Number(volume), observationOnly ? "" : Number(cutoff),
                Number(collectionMs), observationOnly ? "" : Number(controlMs), Number(totalMs), Number(frameMs)
            });
        }
        public void Write(SceneState state, SceneToken token, AudioControlMode mode, AudioControlParameters parameters,
            SpatialAudioRendererAdapter renderer, double collectionMs, double controlMs, double totalMs)
        {
            WriteRow(FormatRow(state, token, mode, parameters, renderer.AppliedVolume, renderer.AppliedLowPassHz,
                collectionMs, controlMs, totalMs, Time.unscaledDeltaTime * 1000f));
        }
        // Preserve the existing columns. Empty audio columns mean no control was applied.
        public static string FormatObservationRow(SceneState state, SceneToken token, double collectionMs, double totalMs, float frameMs)
        {
            return FormatRow(state, token, AudioControlMode.Baseline, default, 0f, 0f,
                collectionMs, 0d, totalMs, frameMs, observationOnly: true);
        }
        public void WriteObservation(SceneState state, SceneToken token, double collectionMs, double totalMs)
        {
            WriteRow(FormatObservationRow(state, token, collectionMs, totalMs, Time.unscaledDeltaTime * 1000f));
        }
        private void WriteRow(string row)
        {
            if (!record || !isActiveAndEnabled) { Close(); return; }
            try
            {
                if (writer == null)
                {
                    string directory = Path.Combine(Application.persistentDataPath, "SceneTokenLogs");
                    Directory.CreateDirectory(directory);
                    FilePath = Path.Combine(directory, "scene_token_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", Invariant) + "_" + Guid.NewGuid().ToString("N") + ".csv");
                    writer = new StreamWriter(FilePath, false, new UTF8Encoding(false));
                    writer.WriteLine(Header);
                    nextFlush = 0d;
                }
                writer.WriteLine(row);
                RowsWritten++;
                if (Time.realtimeSinceStartupAsDouble >= nextFlush)
                {
                    writer.Flush();
                    nextFlush = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.1f, flushIntervalSeconds);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                LastError = exception.Message;
                record = false;
                Debug.LogError("Scene Token logging stopped: " + LastError, this);
                Close();
            }
        }
        private void Update() { if (!record) Close(); }
        public void Close()
        {
            StreamWriter current = writer;
            writer = null;
            try { current?.Dispose(); }
            catch (IOException exception) { LastError = exception.Message; }
        }
        private void OnApplicationPause(bool paused) { if (paused) Close(); }
        private void OnDisable() => Close();
        private void OnDestroy() => Close();
    }
}

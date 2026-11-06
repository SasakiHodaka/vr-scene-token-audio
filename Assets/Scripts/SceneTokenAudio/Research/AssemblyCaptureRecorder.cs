using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SceneTokenAudio.Research
{
    [Serializable]
    public sealed class CapturedAudioContext
    {
        public string listenerId;
        public string sourceKind;
        public Vector3 sourcePosition, listenerPosition;
        public SceneToken token;
    }

    [Serializable]
    public sealed class AssemblyObservation
    {
        public int schemaVersion = 1;
        public string sessionId, recorderId, eventId, clockId, origin, eventKind;
        public string actorId, actorEvidence, actorAudioSourceId, objectId, targetId, taskId, attemptId, utteranceId;
        public string evidenceDetail;
        public long sequence;
        public double observedAt;
        public bool hasObjectPose;
        public Vector3 objectPosition;
        public Quaternion objectRotation;
        public CapturedAudioContext[] audioContexts;
    }

    public sealed class AssemblyCaptureRecorder : MonoBehaviour
    {
        public string sessionId = "";
        public string recorderId = "observer-1", listenerId = "local-listener";
        public SceneStateCollector[] audioCollectors = new SceneStateCollector[0];
        public Transform audioListener;
        public RelationUdpEndpoint udpSender;
        public Representation representation;
        public bool record = true;
        public string DirectoryPath { get; private set; }
        public string LastError { get; private set; }
        public int RecordsWritten { get; private set; }
        public readonly RelationReceiver Receiver = new RelationReceiver();
        public event Action<AssemblyObservation, RelationFrame> Captured;
        private StreamWriter raw, frames, csv;
        private readonly Dictionary<string, SceneStateCollector> poseCollectors = new Dictionary<string, SceneStateCollector>();
        private long sequence;
        private string activeSession;
        private string clockId;

        private void EnsureOpen()
        {
            if (raw != null) return;
            if (string.IsNullOrWhiteSpace(recorderId) || string.IsNullOrWhiteSpace(listenerId))
                throw new InvalidOperationException("Recorder and listener IDs are required");
            if (activeSession == null) activeSession = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;
            if (clockId == null) clockId = activeSession + "/" + recorderId + "/" + Guid.NewGuid().ToString("N");
            DirectoryPath = Path.Combine(Application.persistentDataPath, "SceneTokenAssemblyCaptures", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
            raw = new StreamWriter(Path.Combine(DirectoryPath, "observations.ndjson"), false, new System.Text.UTF8Encoding(false));
            frames = new StreamWriter(Path.Combine(DirectoryPath, "sender-frames.ndjson"), false, new System.Text.UTF8Encoding(false));
            csv = new StreamWriter(Path.Combine(DirectoryPath, "events-audio.csv"), false, new System.Text.UTF8Encoding(false));
            csv.WriteLine(CsvHeader); csv.Flush();
            File.WriteAllText(Path.Combine(DirectoryPath, "capture-info.json"), JsonUtility.ToJson(new CaptureInfo {
                sessionId = activeSession, recorderId = recorderId, clockId = clockId, unityVersion = Application.unityVersion,
                createdUtc = DateTime.UtcNow.ToString("O"), note = "Unity callback capture; not independent ground truth. Local monotonic callback times, not synchronized event times."
            }, true));
        }

        [Serializable]
        private sealed class CaptureInfo { public string sessionId, recorderId, clockId, unityVersion, createdUtc, note; }

        public void Record(string eventKind, string actorId, string actorEvidence, string objectId,
            string targetId, string taskId, string attemptId, string utteranceId,
            string origin, string detail, Transform objectTransform = null, string actorAudioSourceId = "unknown")
        {
            if (!record || !isActiveAndEnabled) return;
            try
            {
                EnsureOpen();
                double now = Time.realtimeSinceStartupAsDouble;
                var contexts = new System.Collections.Generic.List<CapturedAudioContext>();
                foreach (var collector in (audioCollectors ?? new SceneStateCollector[0]).Concat(poseCollectors.Values).Distinct())
                {
                    if (collector && poseCollectors.Values.Contains(collector)) collector.listener = audioListener;
                    if (collector && collector.TryCollect(out var state))
                        contexts.Add(new CapturedAudioContext { listenerId = listenerId, sourcePosition = state.sourcePosition,
                            listenerPosition = state.listenerPosition, token = SceneTokenGenerator.Generate(state),
                            sourceKind = poseCollectors.Values.Contains(collector) ? "tracked_head_pose" : "configured_collector" });
                }
                var observation = new AssemblyObservation {
                    sessionId = activeSession, recorderId = recorderId, eventId = clockId + "/" + (++sequence),
                    clockId = clockId, sequence = sequence, observedAt = now, eventKind = Known(eventKind),
                    actorId = Known(actorId), actorEvidence = Known(actorEvidence), actorAudioSourceId = Known(actorAudioSourceId),
                    objectId = Known(objectId), targetId = Known(targetId), taskId = Known(taskId), attemptId = Known(attemptId),
                    utteranceId = Known(utteranceId), origin = Known(origin), evidenceDetail = detail ?? "",
                    hasObjectPose = objectTransform, objectPosition = objectTransform ? objectTransform.position : Vector3.zero,
                    objectRotation = objectTransform ? objectTransform.rotation : Quaternion.identity, audioContexts = contexts.ToArray()
                };
                // Persist observation before representation generation. Never use it as an automatic oracle.
                raw.WriteLine(JsonUtility.ToJson(observation)); raw.Flush();
                if (observation.audioContexts.Length == 0) csv.WriteLine(FormatCsv(observation, null));
                else foreach (var context in observation.audioContexts) csv.WriteLine(FormatCsv(observation, context));
                csv.Flush();
                var relations = new System.Collections.Generic.List<Relation> {
                    MakeRelation(observation, "object", observation.objectId),
                    MakeRelation(observation, "target", observation.targetId),
                    MakeRelation(observation, "attempt", observation.attemptId)
                };
                var frame = new RelationFrame { sessionId = activeSession, availableAt = Time.realtimeSinceStartupAsDouble,
                    relations = relations.ToArray() };
                string json = RelationWire.Send(frame, representation, Reduction.Full, int.MaxValue);
                frames.WriteLine(JsonUtility.ToJson(frame)); frames.Flush();
                RecordsWritten++;
                Receiver.Receive(json);
                Captured?.Invoke(observation, frame);
                if (udpSender) udpSender.Send(json);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is System.Net.Sockets.SocketException)
            { LastError = e.Message; record = false; Close(); Debug.LogError("Assembly capture stopped: " + LastError, this); }
        }

        private static string Known(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
        public string RegisterActorPose(string actorId, Transform head)
        {
            if (string.IsNullOrWhiteSpace(actorId) || actorId == "unknown" || !head || !audioListener) return "unknown";
            if (!poseCollectors.TryGetValue(actorId, out var collector) || !collector)
            {
                var item = new GameObject("ResearchActorPose"); item.transform.SetParent(transform, false);
                collector = item.AddComponent<SceneStateCollector>();
                collector.sourceId = actorId;
                poseCollectors[actorId] = collector;
            }
            collector.sourceTransform = head;
            collector.listener = audioListener;
            return collector.sourceId;
        }

        public const string CsvHeader = "schema_version,session_id,recorder_id,event_id,clock_id,sequence,observed_at_s,event_kind,actor_id,actor_evidence,actor_audio_source_id,object_id,target_id,task_id,attempt_id,utterance_id,origin,evidence_detail,object_x,object_y,object_z,audio_available,actor_audio_match,listener_id,audio_source_kind,audio_timestamp_s,audio_source_id,distance_m,direction_x,direction_y,direction_z,occlusion,task_phase,priority,time_to_deadline_s";
        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Q(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        public static string FormatCsv(AssemblyObservation o, CapturedAudioContext context)
        {
            var fields = new List<string> { "1", Q(o.sessionId), Q(o.recorderId), Q(o.eventId), Q(o.clockId), o.sequence.ToString(CultureInfo.InvariantCulture),
                N(o.observedAt), Q(o.eventKind), Q(o.actorId), Q(o.actorEvidence), Q(o.actorAudioSourceId), Q(o.objectId), Q(o.targetId), Q(o.taskId),
                Q(o.attemptId), Q(o.utteranceId), Q(o.origin), Q(o.evidenceDetail),
                o.hasObjectPose ? N(o.objectPosition.x) : "", o.hasObjectPose ? N(o.objectPosition.y) : "", o.hasObjectPose ? N(o.objectPosition.z) : "",
                context != null ? "true" : "false",
                context != null && o.actorAudioSourceId != "unknown" && o.actorAudioSourceId == context.token.sourceId ? "true" : "false" };
            if (context == null) fields.AddRange(Enumerable.Repeat("", 12));
            else
            {
                var t = context.token;
                fields.AddRange(new[] { Q(context.listenerId), Q(context.sourceKind), N(t.timestamp), Q(t.sourceId), N(t.distance),
                    N(t.relativeDirection.x), N(t.relativeDirection.y), N(t.relativeDirection.z), N(t.occlusion), Q(t.taskPhase.ToString()), N(t.priority), N(t.timeToDeadline) });
            }
            return string.Join(",", fields);
        }
        private static Relation MakeRelation(AssemblyObservation o, string role, string value)
            => new Relation { id = o.eventId + "/" + role, utteranceId = o.utteranceId, actorId = o.actorId,
                kind = o.eventKind + ":" + role, status = "evaluated", observedAt = o.observedAt,
                values = new[] { value }, priority = 1f };

        // Call only from an actual operation failure branch, not from absence of an attachment.
        public void RecordExplicitFailure(string actor, string part, string attempt, string detail)
            => Record("operation_failed", actor, "explicit_failure_callback", part, "unknown", "unknown", attempt,
                "unknown", "instrumented_operation_failure", detail);

        // Emits text separately: a supplied target/ref is annotated, never inferred from the string.
        public void RecordUtterance(string utterance, string actor, string addressee, string text)
            => Record("utterance_observed", actor, "explicit_utterance_input", "not_applicable", addressee,
                "not_applicable", "not_applicable", utterance, "utterance_input", text);

        public void Close()
        {
            var previousRaw = raw; var previousFrames = frames; var previousCsv = csv; raw = null; frames = null; csv = null;
            try { previousRaw?.Dispose(); } finally { try { previousFrames?.Dispose(); } finally { previousCsv?.Dispose(); } }
        }
        private void OnApplicationPause(bool paused) { if (paused) Close(); }
        private void OnDisable() => Close();
        private void OnDestroy() => Close();
    }
}

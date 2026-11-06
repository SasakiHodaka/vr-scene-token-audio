using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SceneTokenAudio.Research
{
    // Experimental DTOs, independent of the existing eight-field audio token.
    [Serializable]
    public sealed class Relation
    {
        public string id, utteranceId, actorId, kind, status;
        public double observedAt;
        public int revision;
        public string[] values = new string[0];
        public float priority;
    }

    [Serializable]
    public sealed class RelationFrame
    {
        public string sessionId;
        public double availableAt;
        public Relation[] relations = new Relation[0];
    }

    [Serializable]
    public sealed class RelationFrames { public RelationFrame[] frames; }

    [Serializable]
    public sealed class Edge { public string subject, predicate, target; }

    [Serializable]
    public sealed class WireRelation
    {
        public string id, utteranceId, actorId, kind, status;
        public double observedAt;
        public int revision, declaredCount;
        public string[] values;
        public Edge[] edges;
    }

    [Serializable]
    public sealed class Envelope
    {
        public int schemaVersion = 1;
        public string sessionId, format;
        public double availableAt;
        public WireRelation[] relations;
    }

    public enum Representation { StructuredMessage, RelationGraph }
    public enum Reduction { Full, RankedItems, GenericClosed, TokenClosed }

    public static class RelationWire
    {
        public static void Validate(RelationFrame frame)
        {
            if (frame == null || string.IsNullOrWhiteSpace(frame.sessionId) || frame.relations == null ||
                double.IsNaN(frame.availableAt) || double.IsInfinity(frame.availableAt) || frame.availableAt < 0)
                throw new ArgumentException("Invalid frame");
            var ids = new HashSet<string>();
            foreach (var r in frame.relations)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.id) || !ids.Add(r.id) ||
                    string.IsNullOrWhiteSpace(r.actorId) || string.IsNullOrWhiteSpace(r.kind) ||
                    string.IsNullOrWhiteSpace(r.utteranceId) || r.values == null || r.revision < 0 ||
                    double.IsNaN(r.observedAt) || double.IsInfinity(r.observedAt) || r.observedAt < 0 || r.observedAt > frame.availableAt ||
                    float.IsNaN(r.priority) || float.IsInfinity(r.priority) ||
                    (r.status != "evaluated" && r.status != "insufficient") ||
                    (r.status == "insufficient" && r.values.Length != 0) ||
                    r.values.Any(string.IsNullOrWhiteSpace) || r.values.Distinct().Count() != r.values.Length)
                    throw new ArgumentException("Invalid relation");
            }
        }

        private static WireRelation Encode(Relation r, string[] selected, Representation format)
        {
            return new WireRelation {
                id = r.id, utteranceId = r.utteranceId, actorId = r.actorId, kind = r.kind,
                status = r.status, observedAt = r.observedAt, revision = r.revision,
                declaredCount = r.values.Length,
                values = format == Representation.StructuredMessage ? selected : new string[0],
                edges = format == Representation.RelationGraph ? selected.Select(v => new Edge {
                    subject = r.id, predicate = "value", target = v }).ToArray() : new Edge[0]
            };
        }

        private static string Pack(RelationFrame frame, List<WireRelation> relations, Representation format)
            => JsonUtility.ToJson(new Envelope { sessionId = frame.sessionId, availableAt = frame.availableAt,
                format = format.ToString(), relations = relations.ToArray() });

        public static int Bytes(string json) => Encoding.UTF8.GetByteCount(json);

        // RankedItems is an illustrative priority-based reduction, NOT a Park & Yoon reproduction.
        // GenericClosed and TokenClosed intentionally use the SAME algorithm: a strong fair control.
        public static string Send(RelationFrame frame, Representation format, Reduction reduction, int byteBudget)
        {
            Validate(frame);
            var selected = new List<WireRelation>();
            if (Bytes(Pack(frame, selected, format)) > byteBudget)
                throw new ArgumentException("Budget cannot hold envelope");
            foreach (var r in frame.relations.OrderByDescending(v => v.priority).ThenBy(v => v.id, StringComparer.Ordinal))
            {
                var wire = Encode(r, r.values.OrderBy(v => v, StringComparer.Ordinal).ToArray(), format);
                selected.Add(wire);
                if (reduction == Reduction.Full) continue;
                if (Bytes(Pack(frame, selected, format)) <= byteBudget) continue;
                selected.RemoveAt(selected.Count - 1);
                if (reduction != Reduction.RankedItems || r.values.Length == 0) continue;
                // Preserve declaredCount/status/time/references even when some edges are omitted.
                // The receiver must reject incomplete results rather than invent a single candidate.
                var partial = new List<string>();
                foreach (string value in r.values.OrderBy(v => v, StringComparer.Ordinal))
                {
                    partial.Add(value);
                    selected.Add(Encode(r, partial.ToArray(), format));
                    if (Bytes(Pack(frame, selected, format)) > byteBudget)
                    {
                        selected.RemoveAt(selected.Count - 1);
                        partial.RemoveAt(partial.Count - 1);
                        break;
                    }
                    selected.RemoveAt(selected.Count - 1);
                }
                if (partial.Count > 0) selected.Add(Encode(r, partial.ToArray(), format));
            }
            return Pack(frame, selected, format);
        }
    }

    public sealed class RelationReceiver
    {
        // A local byte-string loopback; no network reliability or cross-device clock claim.
        private readonly Dictionary<string, Relation> history = new Dictionary<string, Relation>();
        public int IncompleteRejected { get; private set; }
        public int StaleIgnored { get; private set; }
        private static string Key(string session, string id) => session + "\u001f" + id;

        public void Receive(string json)
        {
            var packet = JsonUtility.FromJson<Envelope>(json);
            if (packet == null || packet.schemaVersion != 1 || string.IsNullOrWhiteSpace(packet.sessionId) ||
                packet.relations == null || !Enum.TryParse(packet.format, out Representation format) || !Enum.IsDefined(typeof(Representation), format))
                throw new ArgumentException("Invalid envelope");
            var commits = new List<Relation>();
            foreach (var w in packet.relations)
            {
                if (w == null || w.declaredCount < 0) throw new ArgumentException("Invalid count");
                string[] values;
                if (format == Representation.RelationGraph)
                {
                    if (w.edges == null || w.edges.Any(e => e == null || e.subject != w.id || e.predicate != "value"))
                        throw new ArgumentException("Invalid edge");
                    values = w.edges.Select(e => e.target).ToArray();
                }
                else values = w.values;
                if (values == null || values.Length > w.declaredCount || values.Distinct().Count() != values.Length)
                    throw new ArgumentException("Invalid values");
                if (values.Length != w.declaredCount) { IncompleteRejected++; continue; }
                commits.Add(new Relation { id = w.id, utteranceId = w.utteranceId, actorId = w.actorId,
                    kind = w.kind, status = w.status, observedAt = w.observedAt, revision = w.revision, values = values });
            }
            RelationWire.Validate(new RelationFrame { sessionId = packet.sessionId, availableAt = packet.availableAt, relations = commits.ToArray() });
            foreach (var r in commits)
            {
                string key = Key(packet.sessionId, r.id);
                if (history.TryGetValue(key, out var previous))
                {
                    if (r.revision < previous.revision) { StaleIgnored++; continue; }
                    if (r.revision == previous.revision)
                    {
                        if (!Equivalent(previous, r)) throw new ArgumentException("Conflicting same-version result");
                        continue;
                    }
                    if (r.observedAt != previous.observedAt || r.actorId != previous.actorId ||
                        r.kind != previous.kind || r.utteranceId != previous.utteranceId)
                        throw new ArgumentException("A revision cannot change result identity");
                }
                history[key] = r;
            }
        }

        public Relation Get(string session, string id) => history.TryGetValue(Key(session, id), out var r) ? r : null;

        public Relation Latest(string session, string utterance, string actor, string kind)
            => history.Where(pair => pair.Key.StartsWith(session + "\u001f", StringComparison.Ordinal))
                .Select(pair => pair.Value).Where(r => r.utteranceId == utterance && r.actorId == actor && r.kind == kind)
                .OrderByDescending(r => r.observedAt).ThenByDescending(r => r.revision).FirstOrDefault();

        public static bool Equivalent(Relation a, Relation b)
            => a != null && b != null && a.id == b.id && a.utteranceId == b.utteranceId && a.actorId == b.actorId &&
                a.kind == b.kind && a.observedAt == b.observedAt && a.revision == b.revision && a.status == b.status &&
                a.values.OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(b.values.OrderBy(v => v, StringComparer.Ordinal));
    }
}

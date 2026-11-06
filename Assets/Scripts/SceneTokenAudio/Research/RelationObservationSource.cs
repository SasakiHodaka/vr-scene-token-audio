using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneTokenAudio.Research
{
    [Serializable]
    public sealed class ObservedPart { public string id; public Collider collider; }

    public sealed class RelationObservationSource : MonoBehaviour
    {
        public string sessionId = "local-demo", actorId = "A";
        public Transform pointer;
        public ObservedPart[] parts = new ObservedPart[0];
        public string LatestSent { get; private set; }
        public string LatestReceived { get; private set; }
        public readonly RelationReceiver Receiver = new RelationReceiver();
        public Representation representation;
        public RelationUdpEndpoint udpSender;
        public event Action<RelationFrame> Observed;

        // The caller supplies the utterance ID at its observation time. No ASR/intent inference.
        public RelationFrame ObservePointing(string utteranceId, string observationId, double observedAt, double availableAt)
        {
            bool usable = pointer && pointer.gameObject.activeInHierarchy && parts != null && parts.Length > 0;
            var candidates = new List<string>();
            var ids = new HashSet<string>();
            if (usable)
            {
                Physics.SyncTransforms();
                var ray = new Ray(pointer.position, pointer.forward);
                foreach (var part in parts)
                {
                    if (part == null || string.IsNullOrWhiteSpace(part.id) || !ids.Add(part.id) ||
                        !part.collider || !part.collider.enabled || !part.collider.gameObject.activeInHierarchy)
                    { usable = false; break; }
                    if (part.collider.Raycast(ray, out _, 100f)) candidates.Add(part.id);
                }
            }
            return Publish(new Relation { id = observationId, utteranceId = utteranceId, actorId = actorId,
                kind = "pointing_candidates", observedAt = observedAt, status = usable ? "evaluated" : "insufficient",
                values = usable ? candidates.ToArray() : new string[0], priority = 1f }, availableAt);
        }

        // Hook this into the existing grasp/install callback. The object ID is observed, not inferred from proximity.
        public RelationFrame ObserveOperation(string utteranceId, string observationId, string objectId,
            string action, string outcome, double observedAt, double availableAt)
        {
            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(outcome) || string.IsNullOrWhiteSpace(objectId))
                throw new ArgumentException("Operation fields are required");
            return Publish(new Relation { id = observationId, utteranceId = utteranceId, actorId = actorId,
                kind = "operation:" + action + ":" + outcome, observedAt = observedAt,
                status = "evaluated", values = new[] { objectId }, priority = 1f }, availableAt);
        }

        private RelationFrame Publish(Relation relation, double availableAt)
        {
            var frame = new RelationFrame { sessionId = sessionId, availableAt = availableAt, relations = new[] { relation } };
            LatestSent = RelationWire.Send(frame, representation, Reduction.Full, int.MaxValue);
            Receiver.Receive(LatestSent);
            LatestReceived = JsonUtility.ToJson(Receiver.Get(sessionId, relation.id));
            if (udpSender) udpSender.Send(LatestSent);
            Observed?.Invoke(frame);
            return frame;
        }
    }
}

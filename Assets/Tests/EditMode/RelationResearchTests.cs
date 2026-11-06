using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SceneTokenAudio.Research;
using UnityEngine;

namespace SceneTokenAudio.Tests
{
    public sealed class RelationResearchTests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static RelationFrames Input() => JsonUtility.FromJson<RelationFrames>(File.ReadAllText(Path.Combine(Root, "Evaluation/relation-probe/input.json")));
        public static RelationFrames Expected() => JsonUtility.FromJson<RelationFrames>(File.ReadAllText(Path.Combine(Root, "Evaluation/relation-probe/expected.json")));

        [TestCase(Representation.StructuredMessage)]
        [TestCase(Representation.RelationGraph)]
        public void FullRoundTripMatchesIndependentOracle(Representation format)
        {
            var input = Input();
            var expected = Expected();
            var receiver = new RelationReceiver();
            Assert.That(input.frames.Length, Is.EqualTo(expected.frames.Length));
            for (int i = 0; i < input.frames.Length; i++)
            {
                receiver.Receive(RelationWire.Send(input.frames[i], format, Reduction.Full, int.MaxValue));
                foreach (var relation in expected.frames[i].relations)
                    Assert.That(RelationReceiver.Equivalent(receiver.Get(expected.frames[i].sessionId, relation.id), relation), Is.True, relation.id);
            }
            Assert.That(receiver.Latest("S4-S5", "U1", "A", "pointing_candidates").id, Is.EqualTo("Q3"), "Old correction cannot replace latest observation");
            receiver.Receive(RelationWire.Send(input.frames[1], format, Reduction.Full, int.MaxValue));
            Assert.That(receiver.Get("S4-S5", "Q2").revision, Is.EqualTo(1));
            Assert.That(receiver.Get("S4-S5", "Q4").status, Is.EqualTo("evaluated"));
            Assert.That(receiver.Get("S4-S5", "Q1").status, Is.EqualTo("insufficient"));
        }

        [TestCase(Representation.StructuredMessage)]
        [TestCase(Representation.RelationGraph)]
        public void GenericClosureAndTokenClosureAreIdenticalAtEveryBudget(Representation format)
        {
            foreach (var frame in Input().frames)
                foreach (int budget in new[] { 256, 384, 512, 768, 1024, 2048 })
                {
                    string generic = RelationWire.Send(frame, format, Reduction.GenericClosed, budget);
                    string token = RelationWire.Send(frame, format, Reduction.TokenClosed, budget);
                    Assert.That(token, Is.EqualTo(generic));
                    Assert.That(RelationWire.Bytes(token), Is.LessThanOrEqualTo(budget));
                }
        }

        [TestCase(Representation.StructuredMessage)]
        [TestCase(Representation.RelationGraph)]
        public void PartialCandidateSetIsNeverPublishedAsSingleCandidate(Representation format)
        {
            var frame = Input().frames[1];
            bool observedPartial = false;
            int fullBytes = RelationWire.Bytes(RelationWire.Send(frame, format, Reduction.Full, int.MaxValue));
            for (int budget = 256; budget < fullBytes; budget++)
            {
                var receiver = new RelationReceiver();
                receiver.Receive(RelationWire.Send(frame, format, Reduction.RankedItems, budget));
                if (receiver.IncompleteRejected > 0)
                {
                    observedPartial = true;
                    Assert.That(receiver.Get(frame.sessionId, "Q2"), Is.Null);
                    break;
                }
            }
            Assert.That(observedPartial, Is.True, "Exercise an actual partial-result budget");
        }

        [Test]
        public void GeometryUsesLiveCollidersAndDoesNotRewritePastResults()
        {
            var system = new GameObject("Relation observation source");
            var pointer = new GameObject("Pointer");
            var part1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var part2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                part1.transform.position = Vector3.forward * 2f;
                part2.transform.position = Vector3.forward * 3f;
                part1.transform.localScale = part2.transform.localScale = Vector3.one * 0.1f;
                var source = system.AddComponent<RelationObservationSource>();
                source.pointer = pointer.transform;
                source.parts = new[] {
                    new ObservedPart { id = "M1", collider = part1.GetComponent<Collider>() },
                    new ObservedPart { id = "M2", collider = part2.GetComponent<Collider>() }
                };
                source.ObservePointing("U1", "Q2", 1.2, 1.2);
                Assert.That(source.Receiver.Get(source.sessionId, "Q2").values, Is.EquivalentTo(new[] { "M1", "M2" }));
                part2.transform.position = Vector3.right * 3f;
                source.ObservePointing("U1", "Q3", 1.4, 1.4);
                Assert.That(source.Receiver.Get(source.sessionId, "Q3").values, Is.EqualTo(new[] { "M1" }));
                part1.transform.position = Vector3.left * 3f;
                source.ObservePointing("U1", "Q4", 2.0, 2.0);
                Assert.That(source.Receiver.Get(source.sessionId, "Q4").values, Is.Empty);
                Assert.That(source.Receiver.Get(source.sessionId, "Q4").status, Is.EqualTo("evaluated"));
                Assert.That(source.Receiver.Get(source.sessionId, "Q3").values, Is.EqualTo(new[] { "M1" }));
                source.pointer = null;
                source.ObservePointing("U1", "Q5", 2.1, 2.1);
                Assert.That(source.Receiver.Get(source.sessionId, "Q5").status, Is.EqualTo("insufficient"));
                source.ObserveOperation("not_applicable", "O2", "M2", "Install", "Succeeded", 2.2, 2.2);
                Assert.That(source.Receiver.Get(source.sessionId, "O2").values, Is.EqualTo(new[] { "M2" }));
                Assert.That(source.Receiver.Get(source.sessionId, "O2").utteranceId, Is.EqualTo("not_applicable"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(system); UnityEngine.Object.DestroyImmediate(pointer);
                UnityEngine.Object.DestroyImmediate(part1); UnityEngine.Object.DestroyImmediate(part2);
            }
        }

        [Test]
        public void ConflictingRevisionAndMalformedObservationAreRejected()
        {
            var frame = Input().frames[2];
            var receiver = new RelationReceiver();
            receiver.Receive(RelationWire.Send(frame, Representation.StructuredMessage, Reduction.Full, int.MaxValue));
            frame.relations[0].values = new[] { "M2" };
            Assert.Throws<ArgumentException>(() => receiver.Receive(RelationWire.Send(frame, Representation.StructuredMessage, Reduction.Full, int.MaxValue)));
            frame.relations[0].observedAt = double.NaN;
            Assert.Throws<ArgumentException>(() => RelationWire.Send(frame, Representation.StructuredMessage, Reduction.Full, int.MaxValue));
        }

        [Test]
        public void CrossSessionAndCrossUtteranceResultsDoNotOverwrite()
        {
            var frame = Input().frames[0];
            var receiver = new RelationReceiver();
            receiver.Receive(RelationWire.Send(frame, Representation.StructuredMessage, Reduction.Full, int.MaxValue));
            frame.sessionId = "other-session";
            frame.relations[0].utteranceId = "U2";
            receiver.Receive(RelationWire.Send(frame, Representation.StructuredMessage, Reduction.Full, int.MaxValue));
            Assert.That(receiver.Get("S4-S5", "Q1").utteranceId, Is.EqualTo("U1"));
            Assert.That(receiver.Get("other-session", "Q1").utteranceId, Is.EqualTo("U2"));
        }
    }
}

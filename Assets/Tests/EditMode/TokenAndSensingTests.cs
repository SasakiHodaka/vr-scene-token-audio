using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace SceneTokenAudio.Tests
{
    public sealed class TokenAndSensingTests
    {
        private GameObject listener, source, wall, system;
        [SetUp]
        public void SetUp()
        {
            listener = new GameObject("Listener");
            source = new GameObject("Source", typeof(AudioSource));
            source.transform.position = Vector3.forward * 4f;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = Vector3.forward * 2f;
            system = new GameObject("System", typeof(OcclusionDetector));
            Physics.SyncTransforms();
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(listener); Object.DestroyImmediate(source);
            Object.DestroyImmediate(wall); Object.DestroyImmediate(system);
        }
        private SceneStateCollector Collector()
        {
            var collector = source.AddComponent<SceneStateCollector>();
            collector.listener = listener.transform;
            collector.audioSource = source.GetComponent<AudioSource>();
            collector.occlusionDetector = system.GetComponent<OcclusionDetector>();
            return collector;
        }
        [Test]
        public void MovingWallChangesOcclusion()
        {
            var detector = system.GetComponent<OcclusionDetector>();
            Assert.That(detector.Measure(listener.transform, source.transform), Is.EqualTo(1f));
            wall.transform.position = Vector3.right * 10f;
            Physics.SyncTransforms();
            Assert.That(detector.Measure(listener.transform, source.transform), Is.EqualTo(0f));
        }
        [Test]
        public void ListenerInsideWallIsOccluded()
        {
            wall.transform.position = listener.transform.position;
            Physics.SyncTransforms();
            Assert.That(system.GetComponent<OcclusionDetector>().Measure(listener.transform, source.transform), Is.EqualTo(1f));
        }
        [Test]
        public void DirectionIsListenerRelativeAndIndependentOfScale()
        {
            listener.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            listener.transform.localScale = Vector3.one * 3f;
            Assert.That(Collector().TryCollect(out var state), Is.True);
            Assert.That(state.distance, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(state.relativeDirection.x, Is.EqualTo(-1f).Within(0.0001f));
        }
        [Test]
        public void CoincidentSourceHasZeroDirection()
        {
            source.transform.position = listener.transform.position;
            Collector().TryCollect(out var state);
            SceneTokenSerializer.Validate(SceneTokenGenerator.Generate(state));
            Assert.That(state.relativeDirection, Is.EqualTo(Vector3.zero));
        }
        [Test]
        public void VeryCloseSourceUsesZeroDirectionConvention()
        {
            source.transform.position = listener.transform.position + Vector3.forward * 0.00005f;
            Collector().TryCollect(out var state);
            SceneTokenSerializer.Validate(SceneTokenGenerator.Generate(state));
            Assert.That(state.relativeDirection, Is.EqualTo(Vector3.zero));
        }
        [Test]
        public void SerializationRoundTripsAndTransportDeliversOnce()
        {
            Collector().TryCollect(out var state);
            var token = SceneTokenGenerator.Generate(state);
            var copy = SceneTokenSerializer.Deserialize(SceneTokenSerializer.Serialize(token));
            Assert.That(copy.distance, Is.EqualTo(token.distance));
            Assert.That(copy.relativeDirection.z, Is.EqualTo(token.relativeDirection.z));
            var transport = new LocalSceneTokenTransport();
            int received = 0;
            transport.Received += value => { Assert.That(value.sourceId, Is.EqualTo(token.sourceId)); received++; };
            transport.Publish(copy);
            Assert.That(received, Is.EqualTo(1));
        }
        [TestCase("{}")]
        [TestCase("")]
        [TestCase("null")]
        public void InvalidJsonIsRejected(string json) => Assert.Throws<System.ArgumentException>(() => SceneTokenSerializer.Deserialize(json));
        [Test]
        public void NonFiniteObservationIsRejected()
        {
            Collector().TryCollect(out var state);
            var token = SceneTokenGenerator.Generate(state);
            token.distance = float.NaN;
            Assert.Throws<System.ArgumentException>(() => SceneTokenSerializer.Serialize(token));
        }
        [Test]
        public void CsvUsesInvariantNumbersAndEscapesIdentifiers()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Collector().TryCollect(out var state);
                var token = SceneTokenGenerator.Generate(state);
                token.sourceId = "speaker,\"one\"";
                token.distance = 1.25f;
                string row = AudioEvaluationLogger.FormatRow(state, token, AudioControlMode.Baseline, default, 0.5f, 22000f, 0.1, 0.2, 0.3, 16f);
                StringAssert.Contains("1.25", row);
                StringAssert.Contains("\"speaker,\"\"one\"\"\"", row);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
    }
}

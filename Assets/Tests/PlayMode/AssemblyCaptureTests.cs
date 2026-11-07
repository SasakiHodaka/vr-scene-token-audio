using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SceneTokenAudio.Research;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace SceneTokenAudio.Tests
{
    public sealed class CaptureTestGrabbable : MonoBehaviour
    {
        public bool IsGrabbed;
        public Component CurrentGrabber;
        public UnityEvent<Component> onDidGrab = new UnityEvent<Component>();
        public UnityEvent onDidUngrab = new UnityEvent();
    }

    public sealed class CaptureTestAttachment : MonoBehaviour
    {
        public Component RelatedPoint;
        public UnityEvent onRegisterAttachment = new UnityEvent();
        public UnityEvent onRegisterReverseAttachment = new UnityEvent();
        public UnityEvent onUnregisterAttachment = new UnityEvent();
        public UnityEvent onUnregisterReverseAttachment = new UnityEvent();
    }

    public sealed class AssemblyCaptureTests
    {
        [Test]
        public void LateAttachmentPointsAreSubscribedOnceWithoutInterruptingGrabEvents()
        {
            var root = new GameObject("Late capture test");
            var other = new GameObject("Late mount");
            string directory = null;
            try
            {
                var recorder = root.AddComponent<AssemblyCaptureRecorder>();
                var identity = root.AddComponent<ResearchEntityIdentity>(); identity.entityId = "M1";
                var callbacks = root.AddComponent<CaptureTestGrabbable>();
                var bridge = root.AddComponent<DroneAssemblyEventBridge>();
                bridge.recorder = recorder; bridge.partIdentity = identity; bridge.networkGrabbable = callbacks;
                bridge.Bind();
                Assert.That(bridge.Subscriptions, Is.EqualTo(2));
                other.AddComponent<ResearchEntityIdentity>().entityId = "Slot1";
                var point = root.AddComponent<CaptureTestAttachment>();
                point.RelatedPoint = other.transform;
                bridge.RefreshAttachmentPoints(new Component[] { point, point });
                Assert.That(bridge.Subscriptions, Is.EqualTo(6));
                Assert.That(recorder.RecordsWritten, Is.EqualTo(1), "Late existing attachment is one snapshot");
                directory = recorder.DirectoryPath;
                bridge.RefreshAttachmentPoints(new Component[] { point });
                Assert.That(bridge.Subscriptions, Is.EqualTo(6));
                Assert.That(recorder.RecordsWritten, Is.EqualTo(1));
                point.onUnregisterAttachment.Invoke();
                callbacks.onDidGrab.Invoke(other.transform);
                callbacks.onDidUngrab.Invoke();
                recorder.Close();
                var observations = File.ReadAllLines(Path.Combine(directory, "observations.ndjson"))
                    .Select(json => JsonUtility.FromJson<AssemblyObservation>(json)).ToArray();
                Assert.That(observations.Select(o => o.eventKind), Is.EqualTo(new[] {
                    "attachment_snapshot", "attachment_unregistered", "grab_confirmed", "release_observed" }));
                bridge.Unbind();
                point.onRegisterAttachment.Invoke();
                Assert.That(recorder.RecordsWritten, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(other);
                DeleteCapture(directory);
            }
        }

        [Test]
        public void InvalidLatePointRollsBackItsSubscriptionsAndKeepsExistingGrabBinding()
        {
            var root = new GameObject("Partial subscription test");
            string directory = null;
            try
            {
                var recorder = root.AddComponent<AssemblyCaptureRecorder>();
                var identity = root.AddComponent<ResearchEntityIdentity>(); identity.entityId = "M1";
                var callbacks = root.AddComponent<CaptureTestGrabbable>();
                var bridge = root.AddComponent<DroneAssemblyEventBridge>();
                bridge.recorder = recorder; bridge.partIdentity = identity; bridge.networkGrabbable = callbacks;
                bridge.Bind();
                var point = root.AddComponent<CaptureTestAttachment>();
                point.onRegisterReverseAttachment = null;
                bridge.RefreshAttachmentPoints(new Component[] { point });
                Assert.That(bridge.LastError, Is.Not.Empty);
                Assert.That(bridge.Subscriptions, Is.EqualTo(2));
                point.onRegisterAttachment.Invoke();
                Assert.That(recorder.RecordsWritten, Is.Zero);
                callbacks.onDidUngrab.Invoke();
                directory = recorder.DirectoryPath;
                Assert.That(recorder.RecordsWritten, Is.EqualTo(1));
                point.onRegisterReverseAttachment = new UnityEvent();
                bridge.RefreshAttachmentPoints(new Component[] { point });
                Assert.That(bridge.LastError, Is.Null);
                Assert.That(bridge.Subscriptions, Is.EqualTo(6));
            }
            finally { Object.DestroyImmediate(root); DeleteCapture(directory); }
        }

        [Test]
        public void InitialGrabSnapshotDoesNotClaimAConfirmedGrabCallback()
        {
            var root = new GameObject("Snapshot provenance test");
            var actor = new GameObject("Snapshot actor");
            string directory = null;
            try
            {
                var recorder = root.AddComponent<AssemblyCaptureRecorder>();
                var identity = root.AddComponent<ResearchEntityIdentity>(); identity.entityId = "M1";
                actor.AddComponent<ResearchEntityIdentity>().entityId = "B";
                var callbacks = root.AddComponent<CaptureTestGrabbable>();
                callbacks.IsGrabbed = true; callbacks.CurrentGrabber = actor.transform;
                var bridge = root.AddComponent<DroneAssemblyEventBridge>();
                bridge.recorder = recorder; bridge.partIdentity = identity; bridge.networkGrabbable = callbacks;
                bridge.Bind(); bridge.CaptureInitialState(); callbacks.onDidUngrab.Invoke();
                directory = recorder.DirectoryPath;
                recorder.Close();
                var observations = File.ReadAllLines(Path.Combine(directory, "observations.ndjson"))
                    .Select(json => JsonUtility.FromJson<AssemblyObservation>(json)).ToArray();
                Assert.That(observations[0].eventKind, Is.EqualTo("grab_snapshot"));
                Assert.That(observations[0].actorEvidence, Is.EqualTo("initial_grabber_identity"));
                Assert.That(observations[1].actorEvidence, Is.EqualTo("previous_grabber_snapshot"));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(actor); DeleteCapture(directory); }
        }

        private static void DeleteCapture(string directory)
        {
            if (directory == null) return;
            foreach (string file in new[] { "observations.ndjson", "sender-frames.ndjson", "events-audio.csv", "capture-info.json" })
                File.Delete(Path.Combine(directory, file));
            Directory.Delete(directory);
        }

        [UnityTest]
        public IEnumerator CallbacksWriteCsvWithAudioAndNeverInferFailedInstall()
        {
            var system = new GameObject("Capture test recorder");
            var part = new GameObject("Part");
            var actor = new GameObject("Worker");
            var listener = new GameObject("Listener");
            var other = new GameObject("Mount");
            string directory = null;
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                var recorder = system.AddComponent<AssemblyCaptureRecorder>();
                recorder.audioListener = listener.transform;
                actor.transform.position = Vector3.forward * 4f;
                string sourceId = recorder.RegisterActorPose("B", actor.transform);
                var actorId = actor.AddComponent<ResearchEntityIdentity>(); actorId.entityId = "B"; actorId.actorAudioSourceId = sourceId;
                var partId = part.AddComponent<ResearchEntityIdentity>(); partId.entityId = "M1";
                other.AddComponent<ResearchEntityIdentity>().entityId = "Slot1";
                var callbacks = part.AddComponent<CaptureTestGrabbable>();
                var point = part.AddComponent<CaptureTestAttachment>(); point.RelatedPoint = other.transform;
                var bridge = part.AddComponent<DroneAssemblyEventBridge>();
                bridge.recorder = recorder; bridge.partIdentity = partId; bridge.networkGrabbable = callbacks;
                bridge.attachmentPoints = new Component[] { point };
                bridge.Bind(); Assert.That(bridge.LastError, Is.Null);
                callbacks.onDidGrab.Invoke(actor.transform);
                callbacks.onDidUngrab.Invoke();
                point.onRegisterAttachment.Invoke();
                point.onRegisterReverseAttachment.Invoke(); // Duplicate notification is ignored.
                point.onUnregisterAttachment.Invoke();
                point.onUnregisterReverseAttachment.Invoke();
                directory = recorder.DirectoryPath;
                recorder.Close();
                var observations = File.ReadAllLines(Path.Combine(directory, "observations.ndjson"))
                    .Select(json => JsonUtility.FromJson<AssemblyObservation>(json)).ToArray();
                Assert.That(observations.Select(o => o.eventKind), Is.EqualTo(new[] {
                    "grab_confirmed", "release_observed", "attachment_registered", "attachment_unregistered" }));
                Assert.That(observations[0].actorId, Is.EqualTo("B"));
                Assert.That(observations[0].audioContexts[0].token.sourceId, Is.EqualTo("B"));
                Assert.That(observations[0].audioContexts[0].token.distance, Is.EqualTo(4f));
                Assert.That(observations[0].audioContexts[0].token.timestamp, Is.GreaterThanOrEqualTo(observations[0].observedAt));
                Assert.That(observations[2].actorId, Is.EqualTo("unknown"), "Do not infer installer from last holder");
                Assert.That(observations[2].targetId, Is.EqualTo("Slot1"));
                string[] csv = File.ReadAllLines(Path.Combine(directory, "events-audio.csv"));
                Assert.That(csv.Length, Is.EqualTo(5));
                Assert.That(csv[0], Is.EqualTo(AssemblyCaptureRecorder.CsvHeader));
                StringAssert.Contains(",true,true,", csv[1]);
                Assert.That(csv[1].Split(',').Length, Is.EqualTo(csv[0].Split(',').Length));
                bridge.Unbind(); callbacks.onDidGrab.Invoke(actor.transform);
                Assert.That(recorder.RecordsWritten, Is.EqualTo(4));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                Object.DestroyImmediate(part); Object.DestroyImmediate(system); Object.DestroyImmediate(actor);
                Object.DestroyImmediate(listener); Object.DestroyImmediate(other);
                if (directory != null)
                {
                    // Delete only the fixed files produced by this test, never recurse through a computed path.
                    foreach (string file in new[] { "observations.ndjson", "sender-frames.ndjson", "events-audio.csv", "capture-info.json" })
                        File.Delete(Path.Combine(directory, file));
                    Directory.Delete(directory);
                }
            }
            yield return null;
        }

        [Test]
        public void MissingAudioHasEmptyColumnsAndCsvEscapesText()
        {
            var observation = new AssemblyObservation { eventId = "event,\"quoted\"", eventKind = "release_observed", observedAt = 1.25 };
            string csv = AssemblyCaptureRecorder.FormatCsv(observation, null);
            StringAssert.Contains("\"event,\"\"quoted\"\"\"", csv);
            StringAssert.Contains(",1.25,", csv);
            string simple = AssemblyCaptureRecorder.FormatCsv(new AssemblyObservation(), null);
            Assert.That(simple.Split(',').Length, Is.EqualTo(AssemblyCaptureRecorder.CsvHeader.Split(',').Length));
            StringAssert.Contains(",false,false,", simple);
        }
    }
}

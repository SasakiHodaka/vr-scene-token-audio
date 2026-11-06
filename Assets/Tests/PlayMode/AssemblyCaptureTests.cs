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

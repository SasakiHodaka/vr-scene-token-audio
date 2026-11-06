using System.Collections;
using NUnit.Framework;
using SceneTokenAudio.Research;
using UnityEngine;
using UnityEngine.TestTools;

namespace SceneTokenAudio.Tests
{
    public sealed class RelationUdpTests
    {
        [UnityTest]
        public IEnumerator SeparateUdpReceiverReconstructsObservedOperation()
        {
            var senderObject = new GameObject("UDP sender");
            var receiverObject = new GameObject("UDP receiver");
            senderObject.SetActive(false); receiverObject.SetActive(false);
            try
            {
                var receiver = receiverObject.AddComponent<RelationUdpEndpoint>();
                receiver.listenPort = 0; // OS-assigned port, independent of other tests.
                receiverObject.SetActive(true);
                Assert.That(receiver.LocalPort, Is.GreaterThan(0), receiver.LastError);
                var sender = senderObject.AddComponent<RelationUdpEndpoint>();
                sender.listen = false; sender.remotePort = receiver.LocalPort;
                var source = senderObject.AddComponent<RelationObservationSource>();
                source.udpSender = sender;
                source.actorId = "B";
                senderObject.SetActive(true);
                source.ObserveOperation("not_applicable", "O1", "M1", "Install", "Succeeded", 1.0, 1.0);
                double deadline = Time.realtimeSinceStartupAsDouble + 2d;
                while (receiver.PacketsReceived == 0 && Time.realtimeSinceStartupAsDouble < deadline)
                { receiver.Pump(); yield return null; }
                var reconstructed = receiver.Receiver.Get("local-demo", "O1");
                Assert.That(reconstructed, Is.Not.Null, receiver.LastError);
                Assert.That(reconstructed.actorId, Is.EqualTo("B"));
                Assert.That(reconstructed.values, Is.EqualTo(new[] { "M1" }));
                Assert.That(reconstructed.kind, Is.EqualTo("operation:Install:Succeeded"));
            }
            finally
            {
                Object.DestroyImmediate(senderObject); Object.DestroyImmediate(receiverObject);
            }
        }
    }
}

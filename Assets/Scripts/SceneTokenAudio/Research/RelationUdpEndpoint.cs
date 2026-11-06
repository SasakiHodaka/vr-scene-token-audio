using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SceneTokenAudio.Research
{
    // Optional experimental transport. No delivery guarantee, fragmentation or retransmission.
    public sealed class RelationUdpEndpoint : MonoBehaviour
    {
        public bool listen = true;
        public int listenPort = 19071;
        public string remoteHost = "127.0.0.1";
        public int remotePort = 19071;
        public int LocalPort { get; private set; }
        public int PacketsReceived { get; private set; }
        public string LastError { get; private set; }
        public string LatestReceivedJson { get; private set; }
        public readonly RelationReceiver Receiver = new RelationReceiver();
        private UdpClient client;

        private void OnEnable()
        {
            try
            {
                client = listen ? new UdpClient(listenPort) : new UdpClient();
                if (listen) LocalPort = ((IPEndPoint)client.Client.LocalEndPoint).Port;
            }
            catch (SocketException e) { LastError = e.Message; Close(); }
        }

        public void Send(string json)
        {
            if (client == null) throw new InvalidOperationException("UDP endpoint is not enabled");
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            if (bytes.Length > 60000) throw new ArgumentException("Probe payload exceeds single-datagram limit");
            client.Send(bytes, bytes.Length, remoteHost, remotePort);
        }

        public void Pump()
        {
            if (!listen || client == null) return;
            int remaining = 64; // Bound work per Unity frame.
            while (remaining-- > 0 && client.Available > 0)
            {
                try
                {
                    IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                    string json = Encoding.UTF8.GetString(client.Receive(ref peer));
                    Receiver.Receive(json);
                    LatestReceivedJson = json;
                    PacketsReceived++;
                }
                catch (ArgumentException e) { LastError = e.Message; }
                catch (SocketException e) { LastError = e.Message; break; }
            }
        }

        private void Update() => Pump();
        private void Close() { client?.Close(); client = null; LocalPort = 0; }
        private void OnDisable() => Close();
        private void OnDestroy() => Close();
    }
}

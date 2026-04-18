// Assets/Network/UdpTransport.cs
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RTS.Network
{
    public class UdpTransport : IDisposable
    {
        private UdpClient _socket;
        private Thread _recvThread;
        private volatile bool _running;
        private IPEndPoint _serverEndpoint;

        public readonly ConcurrentQueue<byte[]> IncomingPackets = new();

        public void Connect(string host, int port)
        {
            _serverEndpoint = new IPEndPoint(IPAddress.Parse(host), port);
            _socket = new UdpClient();
            _socket.Connect(_serverEndpoint);
            _running = true;
            _recvThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "UdpRecv"
            };
            _recvThread.Start();
        }

        public void SendRaw(byte[] data)
        {
            _socket?.Send(data, data.Length);
        }

        private void ReceiveLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    byte[] data = _socket.Receive(ref remote);
                    if (data != null && data.Length >= Packet.HeaderSize)
                        IncomingPackets.Enqueue(data);
                }
                catch (SocketException)
                {
                    if (!_running) break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            _running = false;
            _socket?.Close();
            _socket?.Dispose();
        }
    }
}

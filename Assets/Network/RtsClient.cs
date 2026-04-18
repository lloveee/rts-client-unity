// Assets/Network/RtsClient.cs
using System;
using System.Collections.Generic;

namespace RTS.Network
{
    public class RtsClient : IDisposable
    {
        public enum ClientState { Disconnected, Connecting, Connected, InRoom }

        public ClientState State { get; private set; } = ClientState.Disconnected;
        public byte PlayerID { get; private set; }
        public ulong Seed { get; private set; }
        public int MapW { get; private set; }
        public int MapH { get; private set; }
        public byte CurrentN { get; private set; } = 3;

        public event Action<FrameBundle> OnFrame;
        public event Action<NPub> OnNPub;
        public event Action<string> OnError;

        private UdpTransport _transport;
        private Conn _conn;
        private string _playerName;
        private string _roomID;

        public void Connect(string host, int port, string playerName, string roomID)
        {
            _playerName = playerName;
            _roomID = roomID;
            _transport = new UdpTransport();
            _transport.Connect(host, port);
            _conn = new Conn(0, data => _transport.SendRaw(data));

            // Send Hello
            var hello = new Hello { ProtocolVersion = 1, PlayerName = playerName };
            _conn.Send(WireCodec.Encode(hello));
            State = ClientState.Connecting;
        }

        /// <summary>
        /// Call from main thread every frame. Processes incoming packets.
        /// </summary>
        public void Update()
        {
            if (_transport == null) return;

            // Process all queued raw packets
            while (_transport.IncomingPackets.TryDequeue(out byte[] raw))
            {
                var pkt = Packet.Decode(raw);
                if (pkt == null) continue;

                var delivered = _conn.HandleReceive(pkt);
                if (delivered == null) continue;

                foreach (var payload in delivered)
                    ProcessMessage(payload);
            }
        }

        public void Tick()
        {
            _conn?.Tick();
        }

        public void SendCmd(uint tick, byte op, uint unitID, int targetX, int targetY, uint targetID = 0)
        {
            var cmd = new WireCmd
            {
                Tick = tick, Player = PlayerID, Op = op,
                UnitID = unitID, TargetX = targetX, TargetY = targetY, TargetID = targetID
            };
            _conn.Send(WireCodec.Encode(cmd));
        }

        public void SendHashAck(uint tick, ulong hash)
        {
            var ack = new HashAck { Tick = tick, Hash = hash };
            _conn.Send(WireCodec.Encode(ack));
        }

        private void ProcessMessage(byte[] data)
        {
            try
            {
                var (type, msg) = WireCodec.Decode(data);
                switch (type)
                {
                    case MsgType.HelloAck:
                        HandleHelloAck((HelloAck)msg);
                        break;
                    case MsgType.JoinAck:
                        HandleJoinAck((JoinAck)msg);
                        break;
                    case MsgType.FrameBundle:
                        OnFrame?.Invoke((FrameBundle)msg);
                        break;
                    case MsgType.NPub:
                        var np = (NPub)msg;
                        CurrentN = np.N;
                        OnNPub?.Invoke(np);
                        break;
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        private void HandleHelloAck(HelloAck ack)
        {
            if (!ack.Accepted)
            {
                OnError?.Invoke("Server rejected Hello");
                return;
            }
            State = ClientState.Connected;
            // Send JoinRoom
            var join = new JoinRoom { RoomID = _roomID };
            _conn.Send(WireCodec.Encode(join));
        }

        private void HandleJoinAck(JoinAck ack)
        {
            if (!ack.Accepted)
            {
                OnError?.Invoke("Server rejected JoinRoom");
                return;
            }
            PlayerID = ack.PlayerID;
            Seed = ack.Seed;
            MapW = ack.MapW;
            MapH = ack.MapH;
            State = ClientState.InRoom;
        }

        public void Dispose()
        {
            _transport?.Dispose();
            State = ClientState.Disconnected;
        }
    }
}

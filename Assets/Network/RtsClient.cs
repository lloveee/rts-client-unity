// Assets/Network/RtsClient.cs
using System;
using System.Collections.Generic;

namespace RTS.Network
{
    public class RtsClient : IDisposable
    {
        public enum ClientState { Disconnected, AwaitingSynAck, Connecting, Connected, InRoom }

        public ClientState State { get; private set; } = ClientState.Disconnected;
        public byte PlayerID { get; private set; }
        public ulong Seed { get; private set; }
        public int MapW { get; private set; }
        public int MapH { get; private set; }
        public byte CurrentN { get; private set; } = 3;

        public event Action<FrameBundle> OnFrame;
        public event Action<NPub> OnNPub;
        public event Action<string> OnError;
        public event Action<string> OnLog;

        private UdpTransport _transport;
        private Conn _conn;
        private string _playerName;
        private string _roomID;

        // SYN retx (before Conn exists).
        private double _synSentAtSec;
        private int _synRetries;
        private const float SynRetxIntervalSec = 0.5f;
        private const int SynMaxRetries = 10;

        public void Connect(string host, int port, string playerName, string roomID)
        {
            _playerName = playerName;
            _roomID = roomID;
            _transport = new UdpTransport();
            _transport.Connect(host, port);

            SendSyn();
            _synSentAtSec = NowSec();
            _synRetries = 0;
            State = ClientState.AwaitingSynAck;
            OnLog?.Invoke($"[RtsClient] SYN sent to {host}:{port}, waiting for SYN-ACK...");
        }

        private void SendSyn()
        {
            var syn = new Packet { Flags = Packet.FlagSYN, ConnID = 0 };
            _transport.SendRaw(syn.Encode());
        }

        public void Update()
        {
            if (_transport == null) return;

            while (_transport.IncomingPackets.TryDequeue(out byte[] raw))
            {
                var pkt = Packet.Decode(raw);
                if (pkt == null)
                {
                    OnLog?.Invoke($"[RtsClient] dropped malformed packet ({raw.Length} bytes)");
                    continue;
                }

                if (_conn == null)
                {
                    if (pkt.IsSYN && pkt.IsACK && pkt.ConnID != 0)
                    {
                        HandleSynAck(pkt);
                    }
                    else
                    {
                        OnLog?.Invoke($"[RtsClient] unexpected packet before SYN-ACK: flags=0x{pkt.Flags:x2} conn_id={pkt.ConnID}");
                    }
                    continue;
                }

                var delivered = _conn.HandleReceive(pkt);
                if (delivered == null) continue;

                foreach (var payload in delivered)
                    ProcessMessage(payload);
            }
        }

        public void Tick()
        {
            if (_conn == null && State == ClientState.AwaitingSynAck)
            {
                if (NowSec() - _synSentAtSec >= SynRetxIntervalSec)
                {
                    if (_synRetries >= SynMaxRetries)
                    {
                        OnError?.Invoke($"No SYN-ACK after {SynMaxRetries} retries — is the server reachable?");
                        State = ClientState.Disconnected;
                        return;
                    }
                    _synRetries++;
                    SendSyn();
                    _synSentAtSec = NowSec();
                    OnLog?.Invoke($"[RtsClient] SYN retx #{_synRetries}");
                }
                return;
            }

            _conn?.Tick();
        }

        public void SendCmd(uint tick, byte op, uint unitID, int targetX, int targetY, uint targetID = 0)
        {
            if (_conn == null) return;
            var cmd = new WireCmd
            {
                Tick = tick, Player = PlayerID, Op = op,
                UnitID = unitID, TargetX = targetX, TargetY = targetY, TargetID = targetID
            };
            _conn.Send(WireCodec.Encode(cmd));
        }

        public void SendHashAck(uint tick, ulong hash)
        {
            if (_conn == null) return;
            var ack = new HashAck { Tick = tick, Hash = hash };
            _conn.Send(WireCodec.Encode(ack));
        }

        private void HandleSynAck(Packet pkt)
        {
            ushort connID = pkt.ConnID;
            _conn = new Conn(connID, data => _transport.SendRaw(data));
            State = ClientState.Connecting;
            OnLog?.Invoke($"[RtsClient] SYN-ACK received, conn_id={connID}, sending Hello");

            var hello = new Hello { ProtocolVersion = 1, PlayerName = _playerName };
            _conn.Send(WireCodec.Encode(hello));
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
                OnError?.Invoke("Server rejected Hello (protocol version mismatch?)");
                return;
            }
            State = ClientState.Connected;
            OnLog?.Invoke($"[RtsClient] HelloAck accepted (tick_rate={ack.ServerTickRate}), sending JoinRoom room='{_roomID}'");
            var join = new JoinRoom { RoomID = _roomID };
            _conn.Send(WireCodec.Encode(join));
        }

        private void HandleJoinAck(JoinAck ack)
        {
            if (!ack.Accepted)
            {
                OnError?.Invoke("Server rejected JoinRoom (room full?)");
                return;
            }
            PlayerID = ack.PlayerID;
            Seed = ack.Seed;
            MapW = ack.MapW;
            MapH = ack.MapH;
            State = ClientState.InRoom;
            OnLog?.Invoke($"[RtsClient] JoinAck: player_id={PlayerID} seed={Seed} map={MapW}x{MapH}");
        }

        public void Dispose()
        {
            _transport?.Dispose();
            State = ClientState.Disconnected;
        }

        private static double NowSec() =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }
}

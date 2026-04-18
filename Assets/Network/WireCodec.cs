using System;

namespace RTS.Network
{
    public static class WireCodec
    {
        private const int CmdSize = 23;
        private const int CmdPayloadSize = 22;

        public static byte[] Encode(object msg)
        {
            switch (msg)
            {
                case Hello h: return EncodeHello(h);
                case HelloAck h: return EncodeHelloAck(h);
                case JoinRoom j: return EncodeJoinRoom(j);
                case JoinAck j: return EncodeJoinAck(j);
                case WireCmd c: return EncodeCmd(c);
                case FrameBundle f: return EncodeFrameBundle(f);
                case HashAck h: return EncodeHashAck(h);
                case RTTReport r: return EncodeRTTReport(r);
                case NPub n: return EncodeNPub(n);
                case FeedbackHint f: return EncodeFeedbackHint(f);
                case WireResume r: return EncodeResume(r);
                default: throw new ArgumentException($"Unknown message type: {msg.GetType()}");
            }
        }

        public static (MsgType type, object msg) Decode(byte[] data)
        {
            if (data == null || data.Length < 1)
                throw new ArgumentException("Empty message");

            var msgType = (MsgType)data[0];
            var payload = new byte[data.Length - 1];
            Array.Copy(data, 1, payload, 0, payload.Length);

            object decoded = msgType switch
            {
                MsgType.Hello => DecodeHello(payload),
                MsgType.HelloAck => DecodeHelloAck(payload),
                MsgType.JoinRoom => DecodeJoinRoom(payload),
                MsgType.JoinAck => DecodeJoinAck(payload),
                MsgType.Cmd => DecodeCmd(payload),
                MsgType.FrameBundle => DecodeFrameBundle(payload),
                MsgType.HashAck => DecodeHashAck(payload),
                MsgType.RTTReport => DecodeRTTReport(payload),
                MsgType.NPub => DecodeNPub(payload),
                MsgType.FeedbackHint => DecodeFeedbackHint(payload),
                MsgType.Resume => DecodeResume(payload),
                _ => throw new ArgumentException($"Unknown message type: {msgType}")
            };

            return (msgType, decoded);
        }

        private static byte[] EncodeHello(Hello m)
        {
            var name = TruncStr(m.PlayerName, 32);
            var buf = new byte[1 + 2 + 1 + name.Length];
            buf[0] = (byte)MsgType.Hello;
            WriteU16(buf, 1, m.ProtocolVersion);
            buf[3] = (byte)name.Length;
            System.Text.Encoding.UTF8.GetBytes(name, 0, name.Length, buf, 4);
            return buf;
        }

        private static byte[] EncodeHelloAck(HelloAck m)
        {
            var buf = new byte[1 + 2 + 2 + 1];
            buf[0] = (byte)MsgType.HelloAck;
            WriteU16(buf, 1, m.ProtocolVersion);
            WriteU16(buf, 3, m.ServerTickRate);
            buf[5] = m.Accepted ? (byte)1 : (byte)0;
            return buf;
        }

        private static byte[] EncodeJoinRoom(JoinRoom m)
        {
            var rid = TruncStr(m.RoomID, 32);
            var buf = new byte[1 + 1 + rid.Length];
            buf[0] = (byte)MsgType.JoinRoom;
            buf[1] = (byte)rid.Length;
            System.Text.Encoding.UTF8.GetBytes(rid, 0, rid.Length, buf, 2);
            return buf;
        }

        private static byte[] EncodeJoinAck(JoinAck m)
        {
            var rid = TruncStr(m.RoomID, 32);
            var buf = new byte[1 + 1 + rid.Length + 1 + 8 + 4 + 4 + 1];
            int off = 0;
            buf[off++] = (byte)MsgType.JoinAck;
            buf[off++] = (byte)rid.Length;
            System.Text.Encoding.UTF8.GetBytes(rid, 0, rid.Length, buf, off);
            off += rid.Length;
            buf[off++] = m.PlayerID;
            WriteU64(buf, off, m.Seed); off += 8;
            WriteU32(buf, off, (uint)m.MapW); off += 4;
            WriteU32(buf, off, (uint)m.MapH); off += 4;
            buf[off] = m.Accepted ? (byte)1 : (byte)0;
            return buf;
        }

        private static byte[] EncodeCmd(WireCmd m)
        {
            var buf = new byte[CmdSize];
            buf[0] = (byte)MsgType.Cmd;
            WriteCmdPayload(buf, 1, m);
            return buf;
        }

        private static void WriteCmdPayload(byte[] buf, int off, WireCmd m)
        {
            WriteU32(buf, off, m.Tick); off += 4;
            buf[off++] = m.Player;
            buf[off++] = m.Op;
            WriteU32(buf, off, m.UnitID); off += 4;
            WriteU32(buf, off, (uint)m.TargetX); off += 4;
            WriteU32(buf, off, (uint)m.TargetY); off += 4;
            WriteU32(buf, off, m.TargetID);
        }

        private static byte[] EncodeFrameBundle(FrameBundle m)
        {
            int cmdCount = m.Cmds?.Length ?? 0;
            var buf = new byte[1 + 4 + 1 + 2 + cmdCount * CmdPayloadSize];
            buf[0] = (byte)MsgType.FrameBundle;
            WriteU32(buf, 1, m.Tick);
            buf[5] = m.NCurrent;
            WriteU16(buf, 6, (ushort)cmdCount);
            int off = 8;
            for (int i = 0; i < cmdCount; i++)
            {
                WriteCmdPayload(buf, off, m.Cmds[i]);
                off += CmdPayloadSize;
            }
            return buf;
        }

        private static byte[] EncodeHashAck(HashAck m)
        {
            var buf = new byte[1 + 4 + 8];
            buf[0] = (byte)MsgType.HashAck;
            WriteU32(buf, 1, m.Tick);
            WriteU64(buf, 5, m.Hash);
            return buf;
        }

        private static byte[] EncodeRTTReport(RTTReport m)
        {
            var buf = new byte[1 + 6];
            buf[0] = (byte)MsgType.RTTReport;
            for (int i = 0; i < 3; i++)
                WriteU16(buf, 1 + i * 2, m.Samples[i]);
            return buf;
        }

        private static byte[] EncodeNPub(NPub m)
        {
            var buf = new byte[1 + 4 + 1];
            buf[0] = (byte)MsgType.NPub;
            WriteU32(buf, 1, m.EffectiveFromTick);
            buf[5] = m.N;
            return buf;
        }

        private static byte[] EncodeFeedbackHint(FeedbackHint m)
        {
            var buf = new byte[1 + 4 + 1 + 2];
            buf[0] = (byte)MsgType.FeedbackHint;
            WriteU32(buf, 1, m.Tick);
            buf[5] = m.Player;
            WriteU16(buf, 6, m.HintID);
            return buf;
        }

        private static byte[] EncodeResume(WireResume m)
        {
            var buf = new byte[1 + 2 + 4 + 16];
            buf[0] = (byte)MsgType.Resume;
            WriteU16(buf, 1, m.ConnID);
            WriteU32(buf, 3, m.LastExecutedTick);
            Array.Copy(m.Token, 0, buf, 7, 16);
            return buf;
        }

        private static Hello DecodeHello(byte[] d)
        {
            var m = new Hello { ProtocolVersion = ReadU16(d, 0) };
            int nameLen = d[2];
            m.PlayerName = System.Text.Encoding.UTF8.GetString(d, 3, nameLen);
            return m;
        }

        private static HelloAck DecodeHelloAck(byte[] d) => new()
        {
            ProtocolVersion = ReadU16(d, 0),
            ServerTickRate = ReadU16(d, 2),
            Accepted = d[4] != 0
        };

        private static JoinRoom DecodeJoinRoom(byte[] d)
        {
            int ridLen = d[0];
            return new JoinRoom { RoomID = System.Text.Encoding.UTF8.GetString(d, 1, ridLen) };
        }

        private static JoinAck DecodeJoinAck(byte[] d)
        {
            int ridLen = d[0];
            int off = 1 + ridLen;
            return new JoinAck
            {
                RoomID = System.Text.Encoding.UTF8.GetString(d, 1, ridLen),
                PlayerID = d[off++],
                Seed = ReadU64(d, off),
                MapW = (int)ReadU32(d, off + 8),
                MapH = (int)ReadU32(d, off + 12),
                Accepted = d[off + 16] != 0
            };
        }

        private static WireCmd DecodeCmd(byte[] d) => DecodeCmdPayload(d, 0);

        private static WireCmd DecodeCmdPayload(byte[] d, int off) => new()
        {
            Tick = ReadU32(d, off),
            Player = d[off + 4],
            Op = d[off + 5],
            UnitID = ReadU32(d, off + 6),
            TargetX = (int)ReadU32(d, off + 10),
            TargetY = (int)ReadU32(d, off + 14),
            TargetID = ReadU32(d, off + 18)
        };

        private static FrameBundle DecodeFrameBundle(byte[] d)
        {
            var m = new FrameBundle
            {
                Tick = ReadU32(d, 0),
                NCurrent = d[4]
            };
            int cmdCount = ReadU16(d, 5);
            m.Cmds = new WireCmd[cmdCount];
            int off = 7;
            for (int i = 0; i < cmdCount; i++)
            {
                m.Cmds[i] = DecodeCmdPayload(d, off);
                off += CmdPayloadSize;
            }
            return m;
        }

        private static HashAck DecodeHashAck(byte[] d) => new()
        {
            Tick = ReadU32(d, 0),
            Hash = ReadU64(d, 4)
        };

        private static RTTReport DecodeRTTReport(byte[] d)
        {
            var m = new RTTReport();
            for (int i = 0; i < 3; i++)
                m.Samples[i] = ReadU16(d, i * 2);
            return m;
        }

        private static NPub DecodeNPub(byte[] d) => new()
        {
            EffectiveFromTick = ReadU32(d, 0),
            N = d[4]
        };

        private static FeedbackHint DecodeFeedbackHint(byte[] d) => new()
        {
            Tick = ReadU32(d, 0),
            Player = d[4],
            HintID = ReadU16(d, 5)
        };

        private static WireResume DecodeResume(byte[] d)
        {
            var m = new WireResume
            {
                ConnID = ReadU16(d, 0),
                LastExecutedTick = ReadU32(d, 2)
            };
            Array.Copy(d, 6, m.Token, 0, 16);
            return m;
        }

        private static void WriteU16(byte[] b, int o, ushort v)
        { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); }

        private static void WriteU32(byte[] b, int o, uint v)
        { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24); }

        private static void WriteU64(byte[] b, int o, ulong v)
        { WriteU32(b, o, (uint)v); WriteU32(b, o + 4, (uint)(v >> 32)); }

        private static ushort ReadU16(byte[] b, int o) =>
            (ushort)(b[o] | (b[o + 1] << 8));

        private static uint ReadU32(byte[] b, int o) =>
            (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));

        private static ulong ReadU64(byte[] b, int o) =>
            ReadU32(b, o) | ((ulong)ReadU32(b, o + 4) << 32);

        private static string TruncStr(string s, int max) =>
            s != null && s.Length > max ? s.Substring(0, max) : s ?? "";
    }
}

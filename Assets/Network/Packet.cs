using System;

namespace RTS.Network
{
    public class Packet
    {
        public const byte MagicByte0 = 0x52; // 'R'
        public const byte MagicByte1 = 0x31; // '1'

        public const byte FlagSYN = 1 << 0;
        public const byte FlagACK = 1 << 1;
        public const byte FlagFIN = 1 << 2;
        public const byte FlagReliable = 1 << 3;
        public const byte FlagPING = 1 << 4;

        // magic(2) + flags(1) + conn_id(2) + seq(4) + ack(4) + ack_bitmask(4) + payload_len(2) = 19
        public const int HeaderSize = 19;
        public const int MaxPayloadSize = 1200;

        public byte Flags;
        public ushort ConnID;
        public uint Seq;
        public uint Ack;
        public uint AckBitmask;
        public byte[] Payload;

        public bool IsSYN => (Flags & FlagSYN) != 0;
        public bool IsACK => (Flags & FlagACK) != 0;
        public bool IsFIN => (Flags & FlagFIN) != 0;
        public bool IsReliable => (Flags & FlagReliable) != 0;
        public bool IsPING => (Flags & FlagPING) != 0;

        public byte[] Encode()
        {
            int payloadLen = Payload?.Length ?? 0;
            var buf = new byte[HeaderSize + payloadLen];
            buf[0] = MagicByte0;
            buf[1] = MagicByte1;
            buf[2] = Flags;
            WriteU16LE(buf, 3, ConnID);
            WriteU32LE(buf, 5, Seq);
            WriteU32LE(buf, 9, Ack);
            WriteU32LE(buf, 13, AckBitmask);
            WriteU16LE(buf, 17, (ushort)payloadLen);
            if (payloadLen > 0)
                Array.Copy(Payload, 0, buf, HeaderSize, payloadLen);
            return buf;
        }

        public static Packet Decode(byte[] data)
        {
            if (data == null || data.Length < HeaderSize) return null;
            if (data[0] != MagicByte0 || data[1] != MagicByte1) return null;

            ushort payloadLen = ReadU16LE(data, 17);
            if (payloadLen > data.Length - HeaderSize) return null;

            var pkt = new Packet
            {
                Flags = data[2],
                ConnID = ReadU16LE(data, 3),
                Seq = ReadU32LE(data, 5),
                Ack = ReadU32LE(data, 9),
                AckBitmask = ReadU32LE(data, 13)
            };

            if (payloadLen > 0)
            {
                pkt.Payload = new byte[payloadLen];
                Array.Copy(data, HeaderSize, pkt.Payload, 0, payloadLen);
            }

            return pkt;
        }

        private static void WriteU16LE(byte[] buf, int off, ushort v)
        {
            buf[off] = (byte)(v & 0xFF);
            buf[off + 1] = (byte)((v >> 8) & 0xFF);
        }

        private static void WriteU32LE(byte[] buf, int off, uint v)
        {
            buf[off] = (byte)(v & 0xFF);
            buf[off + 1] = (byte)((v >> 8) & 0xFF);
            buf[off + 2] = (byte)((v >> 16) & 0xFF);
            buf[off + 3] = (byte)((v >> 24) & 0xFF);
        }

        private static ushort ReadU16LE(byte[] buf, int off)
        {
            return (ushort)(buf[off] | (buf[off + 1] << 8));
        }

        private static uint ReadU32LE(byte[] buf, int off)
        {
            return (uint)(buf[off] | (buf[off + 1] << 8) | (buf[off + 2] << 16) | (buf[off + 3] << 24));
        }
    }
}

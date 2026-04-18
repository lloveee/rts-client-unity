using System;

namespace RTS.Core
{
    public class ByteReader
    {
        private readonly byte[] _buf;
        private int _pos;

        public ByteReader(byte[] data)
        {
            _buf = data;
            _pos = 0;
        }

        public int Position => _pos;
        public int Remaining => _buf.Length - _pos;

        public byte ReadU8()
        {
            return _buf[_pos++];
        }

        public ushort ReadU16()
        {
            ushort v = (ushort)(_buf[_pos] | (_buf[_pos + 1] << 8));
            _pos += 2;
            return v;
        }

        public uint ReadU32()
        {
            uint v = (uint)(_buf[_pos]
                | (_buf[_pos + 1] << 8)
                | (_buf[_pos + 2] << 16)
                | (_buf[_pos + 3] << 24));
            _pos += 4;
            return v;
        }

        public ulong ReadU64()
        {
            ulong lo = ReadU32();
            ulong hi = ReadU32();
            return lo | (hi << 32);
        }

        public int ReadI32()
        {
            return (int)ReadU32();
        }

        public byte[] ReadBytes(int count)
        {
            var result = new byte[count];
            Array.Copy(_buf, _pos, result, 0, count);
            _pos += count;
            return result;
        }

        public string ReadString(int maxLen)
        {
            int len = ReadU8();
            if (len > maxLen) len = maxLen;
            var s = System.Text.Encoding.UTF8.GetString(_buf, _pos, len);
            _pos += len;
            return s;
        }
    }
}

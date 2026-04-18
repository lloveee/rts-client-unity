using System;

namespace RTS.Core
{
    public class ByteWriter
    {
        private byte[] _buf;
        private int _pos;

        public ByteWriter(int capacity)
        {
            _buf = new byte[capacity];
            _pos = 0;
        }

        public int Position => _pos;

        public void WriteU8(byte v)
        {
            EnsureCapacity(1);
            _buf[_pos++] = v;
        }

        public void WriteU16(ushort v)
        {
            EnsureCapacity(2);
            _buf[_pos++] = (byte)(v & 0xFF);
            _buf[_pos++] = (byte)((v >> 8) & 0xFF);
        }

        public void WriteU32(uint v)
        {
            EnsureCapacity(4);
            _buf[_pos++] = (byte)(v & 0xFF);
            _buf[_pos++] = (byte)((v >> 8) & 0xFF);
            _buf[_pos++] = (byte)((v >> 16) & 0xFF);
            _buf[_pos++] = (byte)((v >> 24) & 0xFF);
        }

        public void WriteU64(ulong v)
        {
            EnsureCapacity(8);
            _buf[_pos++] = (byte)(v & 0xFF);
            _buf[_pos++] = (byte)((v >> 8) & 0xFF);
            _buf[_pos++] = (byte)((v >> 16) & 0xFF);
            _buf[_pos++] = (byte)((v >> 24) & 0xFF);
            _buf[_pos++] = (byte)((v >> 32) & 0xFF);
            _buf[_pos++] = (byte)((v >> 40) & 0xFF);
            _buf[_pos++] = (byte)((v >> 48) & 0xFF);
            _buf[_pos++] = (byte)((v >> 56) & 0xFF);
        }

        public void WriteI32(int v)
        {
            WriteU32((uint)v);
        }

        public void WriteBytes(byte[] data)
        {
            EnsureCapacity(data.Length);
            Array.Copy(data, 0, _buf, _pos, data.Length);
            _pos += data.Length;
        }

        public byte[] ToArray()
        {
            var result = new byte[_pos];
            Array.Copy(_buf, result, _pos);
            return result;
        }

        private void EnsureCapacity(int needed)
        {
            if (_pos + needed <= _buf.Length) return;
            int newSize = Math.Max(_buf.Length * 2, _pos + needed);
            var newBuf = new byte[newSize];
            Array.Copy(_buf, newBuf, _pos);
            _buf = newBuf;
        }
    }
}

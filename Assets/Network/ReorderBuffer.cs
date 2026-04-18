using System.Collections.Generic;

namespace RTS.Network
{
    public class ReorderBuffer
    {
        private uint _nextExpected;
        private readonly Dictionary<uint, byte[]> _buffer = new();
        private readonly int _maxBuffered;

        public ReorderBuffer(uint startSeq, int maxBuffered)
        {
            _nextExpected = startSeq;
            _maxBuffered = maxBuffered;
        }

        public uint NextExpected => _nextExpected;

        /// <summary>
        /// Insert a received packet. Returns payloads ready for in-order delivery (may be empty).
        /// </summary>
        public List<byte[]> Insert(uint seq, byte[] payload)
        {
            if (SeqLT(seq, _nextExpected)) return null;
            if ((int)(seq - _nextExpected) > _maxBuffered) return null;

            if (seq == _nextExpected)
            {
                var delivered = new List<byte[]> { payload };
                _nextExpected++;
                while (_buffer.TryGetValue(_nextExpected, out var p))
                {
                    delivered.Add(p);
                    _buffer.Remove(_nextExpected);
                    _nextExpected++;
                }
                return delivered;
            }

            if (!_buffer.ContainsKey(seq))
                _buffer[seq] = payload;
            return null;
        }

        public static bool SeqLT(uint a, uint b) => (int)(a - b) < 0;
    }
}

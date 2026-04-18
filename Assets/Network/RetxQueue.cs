using System.Collections.Generic;

namespace RTS.Network
{
    public class RetxQueue
    {
        public const float InitialRTOSec = 0.2f;
        public const float MaxRTOSec = 2.0f;
        public const int RTOMultiplier = 2;
        public const int MaxRetries = 10;

        private class PendingPacket
        {
            public Packet Pkt;
            public double SentAt;
            public double NextRetx;
            public float RTO;
            public int Retries;
        }

        private readonly Dictionary<uint, PendingPacket> _pending = new();

        public void Add(Packet pkt, double nowSec, float rtoSec)
        {
            if (rtoSec < InitialRTOSec) rtoSec = InitialRTOSec;
            _pending[pkt.Seq] = new PendingPacket
            {
                Pkt = pkt,
                SentAt = nowSec,
                NextRetx = nowSec + rtoSec,
                RTO = rtoSec,
                Retries = 0
            };
        }

        /// <summary>
        /// Returns RTT sample in seconds if this was a first-transmission ack.
        /// rttSample = -1 means no usable RTT.
        /// </summary>
        public float Ack(uint seq, double nowSec)
        {
            if (!_pending.TryGetValue(seq, out var pp)) return -1;
            _pending.Remove(seq);
            if (pp.Retries == 0) return (float)(nowSec - pp.SentAt);
            return -1;
        }

        public void AckBitmask(uint ackSeq, uint bitmask, double nowSec)
        {
            Ack(ackSeq, nowSec);
            for (uint i = 0; i < 32; i++)
            {
                if ((bitmask & (1u << (int)i)) != 0)
                {
                    uint seq = ackSeq - 1 - i;
                    Ack(seq, nowSec);
                }
            }
        }

        public void CollectRetransmissions(double nowSec, List<Packet> retx, List<uint> expired)
        {
            retx.Clear();
            expired.Clear();
            foreach (var kvp in _pending)
            {
                var pp = kvp.Value;
                if (nowSec < pp.NextRetx) continue;
                pp.Retries++;
                if (pp.Retries > MaxRetries)
                {
                    expired.Add(kvp.Key);
                    continue;
                }
                pp.RTO *= RTOMultiplier;
                if (pp.RTO > MaxRTOSec) pp.RTO = MaxRTOSec;
                pp.NextRetx = nowSec + pp.RTO;
                retx.Add(pp.Pkt);
            }
            foreach (var seq in expired)
                _pending.Remove(seq);
        }

        public int PendingCount => _pending.Count;
    }
}

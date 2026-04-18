// Assets/Network/Conn.cs
using System;
using System.Collections.Generic;

namespace RTS.Network
{
    public class Conn
    {
        public ushort ConnID;
        public uint SendSeq;
        public uint RecvAck;
        public uint RecvBitmask;
        public readonly RetxQueue Retx = new();
        public readonly ReorderBuffer Reorder;

        // RTT estimation (Jacobson's algorithm)
        public float SRTT = 0.1f; // 100ms initial
        public float RTTVar = 0.05f;

        private readonly Action<byte[]> _sendFunc;

        // Reusable lists for retx collection
        private readonly List<Packet> _retxBuf = new();
        private readonly List<uint> _expiredBuf = new();

        public Conn(ushort connID, Action<byte[]> sendFunc)
        {
            ConnID = connID;
            Reorder = new ReorderBuffer(1, 512);
            _sendFunc = sendFunc;
        }

        public void Send(byte[] payload)
        {
            SendSeq++;
            var pkt = new Packet
            {
                Flags = Packet.FlagReliable | Packet.FlagACK,
                ConnID = ConnID,
                Seq = SendSeq,
                Ack = RecvAck,
                AckBitmask = RecvBitmask,
                Payload = payload
            };
            _sendFunc(pkt.Encode());
            Retx.Add(pkt, NowSec(), RTO());
        }

        public void SendACK()
        {
            var pkt = new Packet
            {
                Flags = Packet.FlagACK,
                ConnID = ConnID,
                Ack = RecvAck,
                AckBitmask = RecvBitmask
            };
            _sendFunc(pkt.Encode());
        }

        /// <summary>
        /// Process an incoming packet. Returns list of in-order payloads, or null.
        /// </summary>
        public List<byte[]> HandleReceive(Packet pkt)
        {
            double now = NowSec();

            // Process ACK
            if (pkt.IsACK)
            {
                float rtt = Retx.Ack(pkt.Ack, now);
                if (rtt >= 0) UpdateRTT(rtt);
                Retx.AckBitmask(pkt.Ack, pkt.AckBitmask, now);
            }

            // Process reliable data
            if (pkt.IsReliable && pkt.Payload != null && pkt.Payload.Length > 0)
            {
                UpdateRecvAck(pkt.Seq);
                return Reorder.Insert(pkt.Seq, pkt.Payload);
            }

            // Non-reliable data
            if (!pkt.IsReliable && pkt.Payload != null && pkt.Payload.Length > 0 && !pkt.IsPING)
            {
                return new List<byte[]> { pkt.Payload };
            }

            return null;
        }

        public void Tick()
        {
            double now = NowSec();
            Retx.CollectRetransmissions(now, _retxBuf, _expiredBuf);
            foreach (var pkt in _retxBuf)
            {
                pkt.Ack = RecvAck;
                pkt.AckBitmask = RecvBitmask;
                _sendFunc(pkt.Encode());
            }
        }

        private void UpdateRecvAck(uint seq)
        {
            if (ReorderBuffer.SeqLT(RecvAck, seq))
            {
                uint diff = seq - RecvAck;
                if (diff <= 32)
                    RecvBitmask = (RecvBitmask << (int)diff) | (1u << (int)(diff - 1));
                else
                    RecvBitmask = 0;
                RecvAck = seq;
            }
            else if (seq < RecvAck)
            {
                uint diff = RecvAck - seq;
                if (diff <= 32)
                    RecvBitmask |= 1u << (int)(diff - 1);
            }
        }

        private void UpdateRTT(float sample)
        {
            if (sample <= 0) return;
            if (SRTT == 0)
            {
                SRTT = sample;
                RTTVar = sample / 2;
            }
            else
            {
                float diff = Math.Abs(SRTT - sample);
                RTTVar = (3 * RTTVar + diff) / 4;
                SRTT = (7 * SRTT + sample) / 8;
            }
        }

        private float RTO()
        {
            float rto = SRTT + 4 * RTTVar;
            if (rto < RetxQueue.InitialRTOSec) rto = RetxQueue.InitialRTOSec;
            if (rto > RetxQueue.MaxRTOSec) rto = RetxQueue.MaxRTOSec;
            return rto;
        }

        private static double NowSec() =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }
}

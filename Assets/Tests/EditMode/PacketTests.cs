using NUnit.Framework;
using RTS.Network;

namespace RTS.Tests
{
    public class PacketTests
    {
        [Test]
        public void Roundtrip_WithPayload()
        {
            var pkt = new Packet
            {
                Flags = Packet.FlagReliable | Packet.FlagACK,
                ConnID = 42,
                Seq = 100,
                Ack = 99,
                AckBitmask = 0xAAAAAAAA,
                Payload = new byte[] { 1, 2, 3, 4, 5 }
            };

            byte[] data = pkt.Encode();
            Assert.AreEqual(Packet.HeaderSize + 5, data.Length);

            var decoded = Packet.Decode(data);
            Assert.AreEqual(pkt.Flags, decoded.Flags);
            Assert.AreEqual(pkt.ConnID, decoded.ConnID);
            Assert.AreEqual(pkt.Seq, decoded.Seq);
            Assert.AreEqual(pkt.Ack, decoded.Ack);
            Assert.AreEqual(pkt.AckBitmask, decoded.AckBitmask);
            Assert.AreEqual(pkt.Payload, decoded.Payload);
        }

        [Test]
        public void Roundtrip_NoPayload()
        {
            var pkt = new Packet
            {
                Flags = Packet.FlagACK,
                ConnID = 1,
                Seq = 0,
                Ack = 5,
                AckBitmask = 0,
                Payload = null
            };
            byte[] data = pkt.Encode();
            Assert.AreEqual(Packet.HeaderSize, data.Length);

            var decoded = Packet.Decode(data);
            Assert.AreEqual(0, decoded.Payload?.Length ?? 0);
        }

        [Test]
        public void Magic_Bytes()
        {
            var pkt = new Packet { Flags = 0, ConnID = 0, Seq = 0, Ack = 0, AckBitmask = 0 };
            byte[] data = pkt.Encode();
            Assert.AreEqual(0x52, data[0]);
            Assert.AreEqual(0x31, data[1]);
        }

        [Test]
        public void Decode_TooShort_ReturnsNull()
        {
            Assert.IsNull(Packet.Decode(new byte[5]));
        }

        [Test]
        public void Decode_BadMagic_ReturnsNull()
        {
            var data = new byte[Packet.HeaderSize];
            data[0] = 0xFF;
            Assert.IsNull(Packet.Decode(data));
        }

        [Test]
        public void FlagHelpers()
        {
            var pkt = new Packet { Flags = Packet.FlagSYN | Packet.FlagReliable };
            Assert.IsTrue(pkt.IsSYN);
            Assert.IsTrue(pkt.IsReliable);
            Assert.IsFalse(pkt.IsACK);
            Assert.IsFalse(pkt.IsFIN);
            Assert.IsFalse(pkt.IsPING);
        }
    }
}

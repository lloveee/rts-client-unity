using NUnit.Framework;
using RTS.Core;

namespace RTS.Tests
{
    public class ByteRWTests
    {
        [Test]
        public void Roundtrip_AllTypes()
        {
            var w = new ByteWriter(64);
            w.WriteU8(0xAB);
            w.WriteU16(0x1234);
            w.WriteU32(0xDEADBEEF);
            w.WriteU64(0x0102030405060708UL);
            w.WriteI32(-42);
            w.WriteBytes(new byte[] { 1, 2, 3 });

            var data = w.ToArray();
            var r = new ByteReader(data);

            Assert.AreEqual(0xAB, r.ReadU8());
            Assert.AreEqual(0x1234, r.ReadU16());
            Assert.AreEqual(0xDEADBEEF, r.ReadU32());
            Assert.AreEqual(0x0102030405060708UL, r.ReadU64());
            Assert.AreEqual(-42, r.ReadI32());
            Assert.AreEqual(new byte[] { 1, 2, 3 }, r.ReadBytes(3));
        }

        [Test]
        public void LittleEndian_U16()
        {
            var w = new ByteWriter(2);
            w.WriteU16(0x0102);
            var data = w.ToArray();
            Assert.AreEqual(0x02, data[0]);
            Assert.AreEqual(0x01, data[1]);
        }

        [Test]
        public void LittleEndian_U32()
        {
            var w = new ByteWriter(4);
            w.WriteU32(0x01020304);
            var data = w.ToArray();
            Assert.AreEqual(0x04, data[0]);
            Assert.AreEqual(0x03, data[1]);
            Assert.AreEqual(0x02, data[2]);
            Assert.AreEqual(0x01, data[3]);
        }
    }
}

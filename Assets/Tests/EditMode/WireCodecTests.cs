// Assets/Tests/EditMode/WireCodecTests.cs
using NUnit.Framework;
using RTS.Network;

namespace RTS.Tests
{
    public class WireCodecTests
    {
        [Test]
        public void Hello_Roundtrip()
        {
            var msg = new Hello { ProtocolVersion = 1, PlayerName = "TestPlayer" };
            byte[] data = WireCodec.Encode(msg);
            var (type, decoded) = WireCodec.Decode(data);
            Assert.AreEqual(MsgType.Hello, type);
            var h = (Hello)decoded;
            Assert.AreEqual(1, h.ProtocolVersion);
            Assert.AreEqual("TestPlayer", h.PlayerName);
        }

        [Test]
        public void Cmd_Roundtrip()
        {
            var msg = new WireCmd
            {
                Tick = 100, Player = 0, Op = 1, UnitID = 42,
                TargetX = 3276800, TargetY = -6553600, TargetID = 7
            };
            byte[] data = WireCodec.Encode(msg);
            var (type, decoded) = WireCodec.Decode(data);
            Assert.AreEqual(MsgType.Cmd, type);
            var c = (WireCmd)decoded;
            Assert.AreEqual(100u, c.Tick);
            Assert.AreEqual(0, c.Player);
            Assert.AreEqual(1, c.Op);
            Assert.AreEqual(42u, c.UnitID);
            Assert.AreEqual(3276800, c.TargetX);
            Assert.AreEqual(-6553600, c.TargetY);
            Assert.AreEqual(7u, c.TargetID);
        }

        [Test]
        public void FrameBundle_Roundtrip()
        {
            var msg = new FrameBundle
            {
                Tick = 200,
                NCurrent = 3,
                Cmds = new WireCmd[]
                {
                    new() { Tick = 200, Player = 0, Op = 1, UnitID = 1, TargetX = 100, TargetY = 200 },
                    new() { Tick = 200, Player = 1, Op = 3, UnitID = 5 }
                }
            };
            byte[] data = WireCodec.Encode(msg);
            var (type, decoded) = WireCodec.Decode(data);
            Assert.AreEqual(MsgType.FrameBundle, type);
            var fb = (FrameBundle)decoded;
            Assert.AreEqual(200u, fb.Tick);
            Assert.AreEqual(3, fb.NCurrent);
            Assert.AreEqual(2, fb.Cmds.Length);
            Assert.AreEqual(1u, fb.Cmds[0].UnitID);
            Assert.AreEqual(5u, fb.Cmds[1].UnitID);
        }

        [Test]
        public void HashAck_Roundtrip()
        {
            var msg = new HashAck { Tick = 500, Hash = 0xDEADBEEF12345678UL };
            byte[] data = WireCodec.Encode(msg);
            var (type, decoded) = WireCodec.Decode(data);
            Assert.AreEqual(MsgType.HashAck, type);
            var ha = (HashAck)decoded;
            Assert.AreEqual(500u, ha.Tick);
            Assert.AreEqual(0xDEADBEEF12345678UL, ha.Hash);
        }

        [Test]
        public void JoinAck_Roundtrip()
        {
            var msg = new JoinAck
            {
                RoomID = "test-room", PlayerID = 1, Seed = 42,
                MapW = 100, MapH = 100, Accepted = true
            };
            byte[] data = WireCodec.Encode(msg);
            var (type, decoded) = WireCodec.Decode(data);
            Assert.AreEqual(MsgType.JoinAck, type);
            var ja = (JoinAck)decoded;
            Assert.AreEqual("test-room", ja.RoomID);
            Assert.AreEqual(1, ja.PlayerID);
            Assert.AreEqual(42UL, ja.Seed);
            Assert.AreEqual(100, ja.MapW);
            Assert.AreEqual(100, ja.MapH);
            Assert.IsTrue(ja.Accepted);
        }
    }
}

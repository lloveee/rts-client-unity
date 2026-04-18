using NUnit.Framework;
using RTS.Sim;

namespace RTS.Tests
{
    public class Vec2Tests
    {
        [Test]
        public void Add()
        {
            var a = Vec2.FromInt(1, 2);
            var b = Vec2.FromInt(3, 4);
            var r = a + b;
            Assert.AreEqual(Fixed32.FromInt(4).Raw, r.X.Raw);
            Assert.AreEqual(Fixed32.FromInt(6).Raw, r.Y.Raw);
        }

        [Test]
        public void Sub()
        {
            var a = Vec2.FromInt(5, 7);
            var b = Vec2.FromInt(2, 3);
            var r = a - b;
            Assert.AreEqual(Fixed32.FromInt(3).Raw, r.X.Raw);
            Assert.AreEqual(Fixed32.FromInt(4).Raw, r.Y.Raw);
        }

        [Test]
        public void Scale()
        {
            var a = Vec2.FromInt(3, 4);
            var r = a.Scale(Fixed32.FromInt(2));
            Assert.AreEqual(Fixed32.FromInt(6).Raw, r.X.Raw);
            Assert.AreEqual(Fixed32.FromInt(8).Raw, r.Y.Raw);
        }

        [Test]
        public void Dot()
        {
            var a = Vec2.FromInt(3, 4);
            var b = Vec2.FromInt(1, 2);
            Assert.AreEqual(Fixed32.FromInt(11).Raw, a.Dot(b).Raw);
        }

        [Test]
        public void Len_3_4_5()
        {
            var v = Vec2.FromInt(3, 4);
            Assert.AreEqual(Fixed32.FromInt(5).Raw, v.Len().Raw);
        }

        [Test]
        public void Dist()
        {
            var a = Vec2.FromInt(0, 0);
            var b = Vec2.FromInt(3, 4);
            Assert.AreEqual(Fixed32.FromInt(5).Raw, a.Dist(b).Raw);
        }

        [Test]
        public void Normalize_ZeroVector()
        {
            var v = new Vec2(Fixed32.Zero, Fixed32.Zero);
            var n = v.Normalize();
            Assert.AreEqual(0, n.X.Raw);
            Assert.AreEqual(0, n.Y.Raw);
        }

        [Test]
        public void MoveToward_Arrives()
        {
            var from = Vec2.FromInt(0, 0);
            var target = Vec2.FromInt(1, 0);
            var result = Vec2.MoveToward(from, target, Fixed32.FromInt(10));
            Assert.AreEqual(target.X.Raw, result.X.Raw);
            Assert.AreEqual(target.Y.Raw, result.Y.Raw);
        }

        [Test]
        public void MoveToward_Partial()
        {
            var from = Vec2.FromInt(0, 0);
            var target = Vec2.FromInt(10, 0);
            var result = Vec2.MoveToward(from, target, Fixed32.FromInt(3));
            Assert.AreEqual(Fixed32.FromInt(3).Raw, result.X.Raw);
            Assert.AreEqual(0, result.Y.Raw);
        }
    }
}

using NUnit.Framework;
using RTS.Sim;

namespace RTS.Tests
{
    public class Fixed32Tests
    {
        [Test]
        public void FromInt_One()
        {
            var one = Fixed32.FromInt(1);
            Assert.AreEqual(65536, one.Raw);
        }

        [Test]
        public void FromInt_Negative()
        {
            var neg = Fixed32.FromInt(-3);
            Assert.AreEqual(-3 << 16, neg.Raw);
        }

        [Test]
        public void ToInt_Positive()
        {
            var v = Fixed32.FromInt(7);
            Assert.AreEqual(7, v.ToInt());
        }

        [Test]
        public void ToInt_Negative()
        {
            var v = Fixed32.FromInt(-5);
            Assert.AreEqual(-5, v.ToInt());
        }

        [Test]
        public void Add()
        {
            var a = Fixed32.FromInt(3);
            var b = Fixed32.FromInt(4);
            Assert.AreEqual(Fixed32.FromInt(7).Raw, (a + b).Raw);
        }

        [Test]
        public void Sub()
        {
            var a = Fixed32.FromInt(10);
            var b = Fixed32.FromInt(3);
            Assert.AreEqual(Fixed32.FromInt(7).Raw, (a - b).Raw);
        }

        [Test]
        public void Mul_Integers()
        {
            var a = Fixed32.FromInt(3);
            var b = Fixed32.FromInt(4);
            Assert.AreEqual(Fixed32.FromInt(12).Raw, (a * b).Raw);
        }

        [Test]
        public void Mul_Fractions()
        {
            var half = Fixed32.Half;
            Assert.AreEqual(16384, (half * half).Raw);
        }

        [Test]
        public void Div()
        {
            var a = Fixed32.FromInt(6);
            var b = Fixed32.FromInt(2);
            Assert.AreEqual(Fixed32.FromInt(3).Raw, (a / b).Raw);
        }

        [Test]
        public void Div_Fractional()
        {
            var a = Fixed32.FromInt(1);
            var b = Fixed32.FromInt(3);
            var result = a / b;
            Assert.AreEqual(21845, result.Raw);
        }

        [Test]
        public void Sqrt_Perfect()
        {
            var four = Fixed32.FromInt(4);
            Assert.AreEqual(Fixed32.FromInt(2).Raw, Fixed32.Sqrt(four).Raw);
        }

        [Test]
        public void Sqrt_Nine()
        {
            var nine = Fixed32.FromInt(9);
            Assert.AreEqual(Fixed32.FromInt(3).Raw, Fixed32.Sqrt(nine).Raw);
        }

        [Test]
        public void Sqrt_Zero()
        {
            Assert.AreEqual(0, Fixed32.Sqrt(Fixed32.Zero).Raw);
        }

        [Test]
        public void Sqrt_Negative()
        {
            Assert.AreEqual(0, Fixed32.Sqrt(Fixed32.FromInt(-1)).Raw);
        }

        [Test]
        public void Neg()
        {
            var v = Fixed32.FromInt(5);
            Assert.AreEqual(Fixed32.FromInt(-5).Raw, (-v).Raw);
        }

        [Test]
        public void Abs_Positive()
        {
            var v = Fixed32.FromInt(5);
            Assert.AreEqual(v.Raw, Fixed32.Abs(v).Raw);
        }

        [Test]
        public void Abs_Negative()
        {
            var v = Fixed32.FromInt(-5);
            Assert.AreEqual(Fixed32.FromInt(5).Raw, Fixed32.Abs(v).Raw);
        }

        [Test]
        public void Floor()
        {
            var v = new Fixed32(2 * 65536 + 45875);
            Assert.AreEqual(Fixed32.FromInt(2).Raw, v.Floor().Raw);
        }

        [Test]
        public void Ceil()
        {
            var v = new Fixed32(2 * 65536 + 19661);
            Assert.AreEqual(Fixed32.FromInt(3).Raw, v.Ceil().Raw);
        }

        [Test]
        public void Ceil_Exact()
        {
            var v = Fixed32.FromInt(5);
            Assert.AreEqual(Fixed32.FromInt(5).Raw, v.Ceil().Raw);
        }

        [Test]
        public void Clamp()
        {
            var lo = Fixed32.FromInt(0);
            var hi = Fixed32.FromInt(10);
            Assert.AreEqual(lo.Raw, Fixed32.FromInt(-5).Clamp(lo, hi).Raw);
            Assert.AreEqual(hi.Raw, Fixed32.FromInt(15).Clamp(lo, hi).Raw);
            Assert.AreEqual(Fixed32.FromInt(5).Raw, Fixed32.FromInt(5).Clamp(lo, hi).Raw);
        }
    }
}

using NUnit.Framework;
using RTS.Sim;
using System.IO;

namespace RTS.Tests
{
    public class TrigTests
    {
        private static GoldenData _golden;

        [OneTimeSetUp]
        public void LoadGolden()
        {
            var path = Path.Combine(
                UnityEngine.Application.dataPath,
                "Tests", "EditMode", "GoldenData.json");
            var json = File.ReadAllText(path);
            _golden = UnityEngine.JsonUtility.FromJson<GoldenData>(json);
            Trig.InitSinTable(_golden.sinTableRaw);
        }

        [Test]
        public void SinTable_AllValues()
        {
            Assert.AreEqual(1024, _golden.sinTableRaw.Length);
            for (int i = 0; i < 1024; i++)
            {
                Assert.AreEqual(_golden.sinTableRaw[i], Trig.GetSinTableEntry(i),
                    $"sinTable[{i}] mismatch");
            }
        }

        [Test]
        public void Sin_Zero()
        {
            Assert.AreEqual(0, Trig.Sin(Fixed32.Zero).Raw);
        }

        [Test]
        public void Sin_HalfPi()
        {
            var halfPi = new Fixed32(Fixed32.TwoPi.Raw / 4);
            Assert.AreEqual(Fixed32.One.Raw, Trig.Sin(halfPi).Raw, 1);
        }

        [Test]
        public void Cos_Zero()
        {
            Assert.AreEqual(Fixed32.One.Raw, Trig.Cos(Fixed32.Zero).Raw, 1);
        }

        [Test]
        public void Atan2_Quadrant1()
        {
            var result = Trig.Atan2(Fixed32.One, Fixed32.One);
            var quarterPi = Fixed32.Pi.Raw / 4;
            Assert.AreEqual(quarterPi, result.Raw, 500);
        }
    }
}

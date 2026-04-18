using NUnit.Framework;
using RTS.Sim;
using System.IO;

namespace RTS.Tests
{
    public class SplitMix64Tests
    {
        [Test]
        public void First100_MatchGolden()
        {
            var path = Path.Combine(
                UnityEngine.Application.dataPath,
                "Tests", "EditMode", "GoldenData.json");
            var json = File.ReadAllText(path);
            var golden = UnityEngine.JsonUtility.FromJson<GoldenData>(json);

            var rng = new SplitMix64(42);
            for (int i = 0; i < 100; i++)
            {
                ulong expected = (ulong)golden.splitMixSequence[i];
                ulong actual = rng.Next();
                Assert.AreEqual(expected, actual, $"Mismatch at index {i}");
            }
        }

        [Test]
        public void Intn_InRange()
        {
            var rng = new SplitMix64(123);
            for (int i = 0; i < 1000; i++)
            {
                int v = rng.Intn(10);
                Assert.IsTrue(v >= 0 && v < 10, $"Out of range: {v}");
            }
        }

        [Test]
        public void Intn_Zero()
        {
            var rng = new SplitMix64(0);
            Assert.AreEqual(0, rng.Intn(0));
        }
    }
}

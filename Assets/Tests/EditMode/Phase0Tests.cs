using NUnit.Framework;
using RTS.Sim;
using System.IO;
using UnityEngine;

namespace RTS.Tests
{
    public class Phase0Tests
    {
        [Test]
        public void EmptyWorldHash_MatchesGoldenAnchor()
        {
            var path = Path.Combine(
                Application.dataPath,
                "Tests", "EditMode", "GoldenData.json");
            var json = File.ReadAllText(path);
            var golden = JsonUtility.FromJson<Phase0Golden>(json);

            var w = new World((ulong)golden.seed, golden.mapW, golden.mapH);
            string actual = SimHash.Hash(w).ToString("x16");

            Assert.AreEqual(golden.emptyWorldHash, actual,
                $"C# empty-world hash {actual} != golden anchor {golden.emptyWorldHash}");
        }

        [System.Serializable]
        private class Phase0Golden
        {
            public long seed;
            public int mapW;
            public int mapH;
            public string emptyWorldHash;
        }
    }
}

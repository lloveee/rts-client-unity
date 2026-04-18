using NUnit.Framework;
using RTS.Sim;

namespace RTS.Tests
{
    public class HashTests
    {
        [Test]
        public void EmptyWorld_DeterministicHash()
        {
            var w1 = new World(42, 100, 100);
            w1.Tick = 1;
            var w2 = new World(42, 100, 100);
            w2.Tick = 1;
            Assert.AreEqual(SimHash.Hash(w1), SimHash.Hash(w2));
        }

        [Test]
        public void DifferentTick_DifferentHash()
        {
            var w1 = new World(42, 100, 100);
            w1.Tick = 1;
            var w2 = new World(42, 100, 100);
            w2.Tick = 2;
            Assert.AreNotEqual(SimHash.Hash(w1), SimHash.Hash(w2));
        }

        [Test]
        public void WithUnits_DeterministicHash()
        {
            var w = new World(42, 100, 100);
            w.Tick = 5;
            w.SpawnUnit(0, Vec2.FromInt(10, 20), Fixed32.One, Fixed32.Half);
            w.SpawnUnit(1, Vec2.FromInt(50, 50), Fixed32.One, Fixed32.Half);
            ulong h1 = SimHash.Hash(w);
            ulong h2 = SimHash.Hash(w);
            Assert.AreEqual(h1, h2);
        }
    }
}

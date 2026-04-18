using NUnit.Framework;
using RTS.Sim;
using System.IO;

namespace RTS.Tests
{
    public class StepTests
    {
        private GoldenData _golden;

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
        public void GoldenTest_300Ticks_AllHashesMatch()
        {
            var w = new World((ulong)_golden.seed, _golden.mapW, _golden.mapH);

            for (int i = 0; i < _golden.unitsPerPlayer; i++)
            {
                w.SpawnUnit(0,
                    new Vec2(Fixed32.FromInt(10 + i * 5), Fixed32.FromInt(50)),
                    Fixed32.One, new Fixed32(32768));
                w.SpawnUnit(1,
                    new Vec2(Fixed32.FromInt(90 - i * 5), Fixed32.FromInt(50)),
                    Fixed32.One, new Fixed32(32768));
            }

            var cmdsByTick = new System.Collections.Generic.Dictionary<uint, System.Collections.Generic.List<Cmd>>();
            foreach (var gc in _golden.commands)
            {
                uint tick = (uint)gc.tick;
                if (!cmdsByTick.ContainsKey(tick))
                    cmdsByTick[tick] = new System.Collections.Generic.List<Cmd>();
                cmdsByTick[tick].Add(new Cmd
                {
                    Player = (byte)gc.player,
                    Op = (CmdOp)gc.op,
                    UnitID = (uint)gc.unitID,
                    TargetPos = new Vec2(new Fixed32(gc.targetX), new Fixed32(gc.targetY)),
                    TargetID = (uint)gc.targetID
                });
            }

            for (int t = 0; t < _golden.ticks; t++)
            {
                uint nextTick = (uint)(t + 1);
                Cmd[] cmds = null;
                if (cmdsByTick.TryGetValue(nextTick, out var list))
                    cmds = list.ToArray();

                SimStep.Step(w, cmds ?? System.Array.Empty<Cmd>());
                ulong hash = SimHash.Hash(w);
                string expected = _golden.tickHashes[t];
                string actual = hash.ToString("x16");

                Assert.AreEqual(expected, actual,
                    $"Hash mismatch at tick {w.Tick}");
            }
        }
    }
}

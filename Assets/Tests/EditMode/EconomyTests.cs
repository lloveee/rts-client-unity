using NUnit.Framework;
using RTS.Sim;
using System.Collections.Generic;

namespace RTS.Tests
{
    public class EconomyTests
    {
        [Test]
        public void CrystalRightClick_EntersMining()
        {
            var w = NewEconomyWorld();
            uint workerID = FindFirstWorker(w, 0);
            var cmd = new Cmd
            {
                Player = 0,
                Op = CmdOp.Move,
                UnitID = workerID,
                TargetPos = Vec2.FromInt(5, 10),
            };
            SimStep.Step(w, new[] { cmd });
            int idx = w.FindUnitIndex(workerID);
            Assert.IsTrue(idx >= 0);
            Assert.AreEqual(UnitState.Mining, w.Units[idx].State);
        }

        [Test]
        public void CmdTrain_EnqueuesWorker()
        {
            var w = NewEconomyWorld();
            var p0 = w.Players[0];
            p0.Crystal = Fixed32.FromInt(200);
            w.Players[0] = p0;

            uint hqID = FindFirstHQ(w, 0);
            var cmd = new Cmd
            {
                Player = 0,
                Op = CmdOp.Train,
                UnitID = hqID,
                TargetID = (uint)UnitType.Worker,
            };
            SimStep.Step(w, new[] { cmd });

            int bIdx = w.FindBuildingIndex(hqID);
            Assert.IsTrue(bIdx >= 0);
            var b = w.Buildings[bIdx];
            Assert.IsNotNull(b.ProductionQueue);
            Assert.AreEqual(1, b.ProductionQueue.Length);
            Assert.AreEqual(UnitType.Worker, b.ProductionQueue[0].UnitType);
            Assert.AreEqual(Fixed32.FromInt(150).Raw, w.Players[0].Crystal.Raw);
        }

        [Test]
        public void CmdTrain_InsufficientResource_Dropped()
        {
            var w = NewEconomyWorld();
            uint hqID = FindFirstHQ(w, 0);
            var cmd = new Cmd
            {
                Player = 0,
                Op = CmdOp.Train,
                UnitID = hqID,
                TargetID = (uint)UnitType.Worker,
            };
            SimStep.Step(w, new[] { cmd });

            int bIdx = w.FindBuildingIndex(hqID);
            Assert.IsTrue(bIdx >= 0);
            Assert.IsTrue(w.Buildings[bIdx].ProductionQueue == null
                      || w.Buildings[bIdx].ProductionQueue.Length == 0);
        }

        [Test]
        public void MiningDepletesCrystal()
        {
            var w = NewEconomyWorld();
            uint workerID = FindFirstWorker(w, 0);
            int workerIdx = w.FindUnitIndex(workerID);

            var u = w.Units[workerIdx];
            u.State = UnitState.Mining;
            u.TargetID = w.Crystals[0].ID;
            u.Pos = w.Crystals[0].Pos;
            w.Units[workerIdx] = u;

            var initialRemaining = w.Crystals[0].Remaining;

            for (int t = 0; t < SimConstants.MiningTicksPerTrip; t++)
                SimStep.Step(w, null);

            workerIdx = w.FindUnitIndex(workerID);
            Assert.IsTrue(workerIdx >= 0);
            Assert.AreEqual(UnitState.Returning, w.Units[workerIdx].State);
            Assert.AreEqual(Fixed32.FromInt(SimConstants.CarryCapacity).Raw,
                w.Units[workerIdx].CarryAmount.Raw);
            Assert.AreEqual(
                (initialRemaining - Fixed32.FromInt(SimConstants.CarryCapacity)).Raw,
                w.Crystals[0].Remaining.Raw);
        }

        private static World NewEconomyWorld()
        {
            var w = new World(42, 100, 100);
            w.Players = new List<Player> { new Player { ID = 0 }, new Player { ID = 1 } };
            w.SpawnBuilding(0, BuildingType.HQ, Vec2.FromInt(10, 45));
            w.SpawnBuilding(1, BuildingType.HQ, Vec2.FromInt(90, 45));
            foreach (var pos in SimConstants.CrystalPositions(0)) w.SpawnCrystal(pos);
            foreach (var pos in SimConstants.CrystalPositions(1)) w.SpawnCrystal(pos);

            var stats = SimConstants.UnitStats[UnitType.Worker];
            var speed = new Fixed32((int)(stats.Speed * 65536f));
            for (int i = 0; i < 3; i++)
            {
                w.SpawnUnit(0, Vec2.FromInt(13 + i, 45 + i),
                    Fixed32.FromInt(stats.MaxHP), speed);
                var u = w.Units[w.Units.Count - 1];
                u.Type = UnitType.Worker;
                w.Units[w.Units.Count - 1] = u;

                w.SpawnUnit(1, Vec2.FromInt(93 - i, 45 + i),
                    Fixed32.FromInt(stats.MaxHP), speed);
                u = w.Units[w.Units.Count - 1];
                u.Type = UnitType.Worker;
                w.Units[w.Units.Count - 1] = u;
            }
            return w;
        }

        private static uint FindFirstWorker(World w, byte owner)
        {
            for (int i = 0; i < w.Units.Count; i++)
                if (w.Units[i].Owner == owner && w.Units[i].Type == UnitType.Worker)
                    return w.Units[i].ID;
            return 0;
        }

        private static uint FindFirstHQ(World w, byte owner)
        {
            for (int i = 0; i < w.Buildings.Count; i++)
                if (w.Buildings[i].Owner == owner && w.Buildings[i].Type == BuildingType.HQ)
                    return w.Buildings[i].ID;
            return 0;
        }
    }
}

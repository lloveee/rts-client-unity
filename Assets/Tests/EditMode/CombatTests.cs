using NUnit.Framework;
using RTS.Sim;
using System.Collections.Generic;

namespace RTS.Tests
{
    public class CombatTests
    {
        [Test]
        public void ResolveCombat_DamagesEnemy()
        {
            var w = new World(42, 100, 100);
            w.Players = new List<Player> { new Player { ID = 0 }, new Player { ID = 1 } };
            var s = SimConstants.UnitStats[UnitType.Soldier];
            var rng = new Fixed32((int)(s.Range * 65536f));
            var dmg = new Fixed32((int)(s.Damage * 65536f));
            var spd = new Fixed32((int)(s.Speed * 65536f));

            uint id1 = w.SpawnUnit(0, Vec2.FromInt(10, 10), Fixed32.FromInt(s.MaxHP), spd);
            w.Units[w.Units.Count - 1] = SetSoldier(w.Units[w.Units.Count - 1], rng, dmg);
            uint id2 = w.SpawnUnit(1, Vec2.FromInt(11, 10), Fixed32.FromInt(s.MaxHP), spd);
            w.Units[w.Units.Count - 1] = SetSoldier(w.Units[w.Units.Count - 1], rng, dmg);

            int i0 = w.FindUnitIndex(id1), i1 = w.FindUnitIndex(id2);
            var u0 = w.Units[i0]; u0.State = UnitState.Attacking; u0.TargetID = id2; w.Units[i0] = u0;
            var u1 = w.Units[i1]; u1.State = UnitState.Attacking; u1.TargetID = id1; w.Units[i1] = u1;

            SimStep.Step(w, null);
            var expected = Fixed32.FromInt(s.MaxHP) - dmg;
            Assert.AreEqual(expected.Raw, w.Units[i0].HP.Raw);
            Assert.AreEqual(expected.Raw, w.Units[i1].HP.Raw);
        }

        [Test]
        public void CmdAttackMove_Moves()
        {
            var w = new World(42, 100, 100);
            w.Players = new List<Player> { new Player { ID = 0 }, new Player { ID = 1 } };
            var s = SimConstants.UnitStats[UnitType.Soldier];
            var rng = new Fixed32((int)(s.Range * 65536f));
            var dmg = new Fixed32((int)(s.Damage * 65536f));
            var spd = new Fixed32((int)(s.Speed * 65536f));

            uint id = w.SpawnUnit(0, Vec2.FromInt(10, 10), Fixed32.FromInt(s.MaxHP), spd);
            w.Units[w.Units.Count - 1] = SetSoldier(w.Units[w.Units.Count - 1], rng, dmg);

            int i = w.FindUnitIndex(id);
            var startX = w.Units[i].Pos.X.Raw;
            var cmd = new Cmd { Player = 0, Op = CmdOp.AttackMove, UnitID = id, TargetPos = Vec2.FromInt(50, 10) };
            SimStep.Step(w, new[] { cmd });
            Assert.AreEqual(UnitState.Attacking, w.Units[i].State);
            Assert.IsTrue(w.Units[i].Pos.X.Raw > startX, "unit should move right");
        }

        [Test]
        public void CmdSurrender_GameOver()
        {
            var w = new World(42, 100, 100);
            w.Players = new List<Player> { new Player { ID = 0 }, new Player { ID = 1 } };
            w.SpawnBuilding(0, BuildingType.HQ, Vec2.FromInt(10, 45));
            w.SpawnBuilding(1, BuildingType.HQ, Vec2.FromInt(90, 45));
            SimStep.Step(w, new[] { new Cmd { Player = 0, Op = CmdOp.Surrender } });
            Assert.IsTrue(w.Players[0].Surrendered);
            Assert.IsTrue(w.GameOver);
        }

        private static Unit SetSoldier(Unit u, Fixed32 rng, Fixed32 dmg)
        { u.Type = UnitType.Soldier; u.Range = rng; u.Damage = dmg; return u; }
    }
}

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
            var w = NewCombatWorld();
            var s = SimConstants.UnitStats[UnitType.Soldier];
            var dmg = new Fixed32((int)(s.Damage * 65536f));
            var expected = Fixed32.FromInt(s.MaxHP) - dmg;

            int i0 = w.FindUnitIndex(7), i1 = w.FindUnitIndex(12);
            var u0 = w.Units[i0]; u0.State = UnitState.Attacking; u0.TargetID = w.Units[i1].ID; w.Units[i0] = u0;
            var u1 = w.Units[i1]; u1.State = UnitState.Attacking; u1.TargetID = w.Units[i0].ID; w.Units[i1] = u1;

            SimStep.Step(w, null);
            Assert.AreEqual(expected.Raw, w.Units[i0].HP.Raw);
            Assert.AreEqual(expected.Raw, w.Units[i1].HP.Raw);
        }

        [Test]
        public void CmdAttackMove_Moves()
        {
            var w = NewCombatWorld();
            int i = w.FindUnitIndex(7);
            var cmd = new Cmd { Player = 0, Op = CmdOp.AttackMove, UnitID = w.Units[i].ID, TargetPos = Vec2.FromInt(90, 45) };
            var startX = w.Units[i].Pos.X.Raw;
            SimStep.Step(w, new[] { cmd });
            Assert.AreEqual(UnitState.Attacking, w.Units[i].State);
            Assert.IsTrue(w.Units[i].Pos.X.Raw > startX);
        }

        [Test]
        public void CmdSurrender_GameOver()
        {
            var w = NewCombatWorld();
            SimStep.Step(w, new[] { new Cmd { Player = 0, Op = CmdOp.Surrender } });
            Assert.IsTrue(w.Players[0].Surrendered);
            Assert.IsTrue(w.GameOver);
        }

        private static World NewCombatWorld()
        {
            var w = new World(42, 100, 100);
            w.Players = new List<Player> { new Player { ID = 0 }, new Player { ID = 1 } };
            w.SpawnBuilding(0, BuildingType.HQ, Vec2.FromInt(10, 45));
            w.SpawnBuilding(1, BuildingType.HQ, Vec2.FromInt(90, 45));
            var s = SimConstants.UnitStats[UnitType.Soldier];
            var spd = new Fixed32((int)(s.Speed * 65536f));
            var rng = new Fixed32((int)(s.Range * 65536f));
            var dmg = new Fixed32((int)(s.Damage * 65536f));
            for (int i = 0; i < 5; i++)
            {
                w.SpawnUnit(0, Vec2.FromInt(40 + i, 48 + i), Fixed32.FromInt(s.MaxHP), spd);
                var u = w.Units[w.Units.Count - 1]; u.Type = UnitType.Soldier; u.Range = rng; u.Damage = dmg; w.Units[w.Units.Count - 1] = u;
                w.SpawnUnit(1, Vec2.FromInt(60 - i, 48 + i), Fixed32.FromInt(s.MaxHP), spd);
                u = w.Units[w.Units.Count - 1]; u.Type = UnitType.Soldier; u.Range = rng; u.Damage = dmg; w.Units[w.Units.Count - 1] = u;
            }
            return w;
        }
    }
}

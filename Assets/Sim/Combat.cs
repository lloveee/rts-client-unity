using System.Collections.Generic;

namespace RTS.Sim
{
    public static class SimCombat
    {
        private struct DmgEvent { public uint Target; public Fixed32 Dmg; }

        public static void ResolveCombat(World w)
        {
            var events = new List<DmgEvent>();
            for (int i = 0; i < w.Units.Count; i++)
            {
                var u = w.Units[i];
                if (u.State == UnitState.Dead || u.State != UnitState.Attacking) continue;
                if (u.Range <= Fixed32.Zero || u.TargetID == 0) continue;
                var target = w.FindEntityAny(u.TargetID);
                if (target == null || target.IsDead()) { u.TargetID = 0; w.Units[i] = u; continue; }
                var distSq = u.Pos.DistSq(target.GetPos());
                if (distSq <= u.Range * u.Range)
                    events.Add(new DmgEvent { Target = u.TargetID, Dmg = u.Damage });
                w.Units[i] = u;
            }
            foreach (var e in events)
            {
                var ent = w.FindEntityAny(e.Target);
                if (ent == null || ent.IsDead()) continue;
                ent.SetHP(ent.GetHP() - e.Dmg);
                if (ent.GetHP() <= Fixed32.Zero) ent.SetDead();
            }
        }

        public static void ApplyPushAway(World w)
        {
            var unitRadius = new Fixed32(1 << 15);
            var pushStep = new Fixed32(6554);
            var minDistSq = (unitRadius * Fixed32.FromInt(2)) * (unitRadius * Fixed32.FromInt(2));
            for (int i = 0; i < w.Units.Count; i++)
            {
                var ui = w.Units[i];
                if (ui.State == UnitState.Dead) continue;
                for (int j = i + 1; j < w.Units.Count; j++)
                {
                    var uj = w.Units[j];
                    if (uj.State == UnitState.Dead) continue;
                    var dSq = ui.Pos.DistSq(uj.Pos);
                    if (dSq >= minDistSq) continue;
                    var dir = ui.Pos - uj.Pos;
                    if (dir.X.Raw == 0 && dir.Y.Raw == 0) dir = Vec2.FromInt(1, 0);
                    else dir = dir.Normalize();
                    ui.Pos = ui.Pos + dir.Scale(pushStep);
                    ui.Pos = new Vec2(ui.Pos.X.Clamp(Fixed32.Zero, w.MapSizeX),
                                       ui.Pos.Y.Clamp(Fixed32.Zero, w.MapSizeY));
                }
                w.Units[i] = ui;
            }
        }

        public static void StepAttackOrAdvance(World w, ref Unit u)
        {
            if (u.Range <= Fixed32.Zero) { u.State = UnitState.Idle; return; }
            if (u.TargetID != 0)
            {
                var target = w.FindEntityAny(u.TargetID);
                if (target != null && !target.IsDead())
                {
                    var distSq = u.Pos.DistSq(target.GetPos());
                    if (distSq > u.Range * u.Range)
                    {
                        var np = Vec2.MoveToward(u.Pos, target.GetPos(), u.Speed);
                        u.Pos = new Vec2(np.X.Clamp(Fixed32.Zero, w.MapSizeX),
                                          np.Y.Clamp(Fixed32.Zero, w.MapSizeY));
                    }
                    return;
                }
                u.TargetID = 0;
            }
            if (u.AttackMoveTarget.X.Raw != 0 || u.AttackMoveTarget.Y.Raw != 0)
            {
                var scanRange = u.Range * u.Range * new Fixed32((int)(2.25f * 65536f));
                var nearest = FindNearestEnemy(w, u.Pos, u.Owner, scanRange);
                if (nearest != null) { u.TargetID = nearest.GetID(); return; }
                var np = Vec2.MoveToward(u.Pos, u.AttackMoveTarget, u.Speed);
                u.Pos = new Vec2(np.X.Clamp(Fixed32.Zero, w.MapSizeX),
                                  np.Y.Clamp(Fixed32.Zero, w.MapSizeY));
                if (u.Pos.DistSq(u.AttackMoveTarget) <= Fixed32.Eps)
                { u.AttackMoveTarget = new Vec2(Fixed32.Zero, Fixed32.Zero); u.State = UnitState.Idle; }
                return;
            }
            u.State = UnitState.Idle;
        }

        private static IEntity FindNearestEnemy(World w, Vec2 pos, byte owner, Fixed32 max)
        {
            IEntity best = null; Fixed32 bestD = Fixed32.Zero;
            for (int i = 0; i < w.Units.Count; i++)
            {
                var u = w.Units[i];
                if (u.State == UnitState.Dead || u.Owner == owner) continue;
                var d = pos.DistSq(u.Pos);
                if (d <= max && (best == null || d < bestD))
                { best = new UnitRef { u = u, w = w, idx = i }; bestD = d; }
            }
            return best;
        }
    }
}

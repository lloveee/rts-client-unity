using System;

namespace RTS.Sim
{
    /// <summary>
    /// Single-tick deterministic advance. Ported from Go internal/sim/step.go.
    /// MUST be called single-threaded.
    /// </summary>
    public static class SimStep
    {
        public static readonly Fixed32 AttackRange = Fixed32.FromInt(2);
        public static readonly Fixed32 AttackDamage = Fixed32.Half;

        public static void Step(World w, Cmd[] cmds)
        {
            w.Tick++;

            ApplyCommands(w, cmds);

            SortUnitsByID(w);

            for (int i = 0; i < w.Units.Count; i++)
            {
                var u = w.Units[i];
                if (u.State == UnitState.Dead) continue;

                switch (u.State)
                {
                    case UnitState.Moving:
                        StepMove(w, ref u);
                        break;
                    case UnitState.Idle:
                        StepAttack(w, ref u);
                        break;
                }
                w.Units[i] = u;
            }

            w.RemoveDead();
        }

        private static void ApplyCommands(World w, Cmd[] cmds)
        {
            for (int c = 0; c < cmds.Length; c++)
            {
                ref readonly var cmd = ref cmds[c];
                int idx = w.FindUnitIndex(cmd.UnitID);
                if (idx < 0) continue;

                var u = w.Units[idx];
                if (u.State == UnitState.Dead) continue;
                if (u.Owner != cmd.Player) continue;

                switch (cmd.Op)
                {
                    case CmdOp.Move:
                        u.State = UnitState.Moving;
                        u.MoveTo = cmd.TargetPos;
                        u.TargetID = 0;
                        break;
                    case CmdOp.Attack:
                        u.State = UnitState.Idle;
                        u.TargetID = cmd.TargetID;
                        break;
                    case CmdOp.Stop:
                        u.State = UnitState.Idle;
                        u.TargetID = 0;
                        break;
                }
                w.Units[idx] = u;
            }
        }

        private static void StepMove(World w, ref Unit u)
        {
            var newPos = Vec2.MoveToward(u.Pos, u.MoveTo, u.Speed);
            newPos = new Vec2(
                newPos.X.Clamp(Fixed32.Zero, w.MapSizeX),
                newPos.Y.Clamp(Fixed32.Zero, w.MapSizeY));
            u.Pos = newPos;
            if (u.Pos.DistSq(u.MoveTo) <= Fixed32.Eps)
                u.State = UnitState.Idle;
        }

        private static void StepAttack(World w, ref Unit u)
        {
            if (u.TargetID == 0) return;

            int targetIdx = w.FindUnitIndex(u.TargetID);
            if (targetIdx < 0)
            {
                u.TargetID = 0;
                return;
            }

            var target = w.Units[targetIdx];
            if (target.State == UnitState.Dead)
            {
                u.TargetID = 0;
                return;
            }

            var distSq = u.Pos.DistSq(target.Pos);
            var rangeSq = AttackRange * AttackRange;

            if (distSq <= rangeSq)
            {
                target.HP = target.HP - AttackDamage;
                if (target.HP <= Fixed32.Zero)
                {
                    target.State = UnitState.Dead;
                    target.HP = Fixed32.Zero;
                }
                w.Units[targetIdx] = target;
            }
            else
            {
                u.Pos = Vec2.MoveToward(u.Pos, target.Pos, u.Speed);
            }
        }

        /// <summary>
        /// Insertion sort by ID — deterministic, stable, matches Go.
        /// DO NOT replace with List.Sort() — it is not guaranteed stable.
        /// </summary>
        private static void SortUnitsByID(World w)
        {
            var units = w.Units;
            for (int i = 1; i < units.Count; i++)
            {
                var key = units[i];
                int j = i - 1;
                while (j >= 0 && units[j].ID > key.ID)
                {
                    units[j + 1] = units[j];
                    j--;
                }
                units[j + 1] = key;
            }
        }
    }
}

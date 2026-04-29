using System;

namespace RTS.Sim
{
    public static class SimConstruction
    {
        public static void StepBuilding(World w, ref Unit u)
        {
            if (u.TargetID == 0) { u.State = UnitState.Idle; return; }
            int bIdx = w.FindBuildingIndex(u.TargetID);
            if (bIdx < 0) { u.State = UnitState.Idle; u.TargetID = 0; return; }
            var bld = w.Buildings[bIdx];
            var np = Vec2.MoveToward(u.Pos, bld.Pos, u.Speed);
            u.Pos = new Vec2(np.X.Clamp(Fixed32.Zero, w.MapSizeX), np.Y.Clamp(Fixed32.Zero, w.MapSizeY));
            var ar = Fixed32.FromInt(bld.SizeCells);
            if (u.Pos.DistSq(bld.Pos) > ar * ar) return;
            if (bld.State != BuildingState.Constructing) { u.State = UnitState.Idle; u.TargetID = 0; return; }
            var stats = SimConstants.BuildingStats[bld.Type];
            var inc = Fixed32.One / Fixed32.FromInt((int)stats.BuildTicks);
            bld.ConstructProgress = bld.ConstructProgress + inc;
            bld.HP = bld.MaxHP * bld.ConstructProgress;
            if (bld.HP <= Fixed32.Zero) bld.HP = Fixed32.One;
            if (bld.ConstructProgress >= Fixed32.One)
            { bld.State = BuildingState.Ready; bld.ConstructProgress = Fixed32.One; bld.HP = bld.MaxHP; u.State = UnitState.Idle; u.TargetID = 0; }
            w.Buildings[bIdx] = bld;
        }

        public static void ApplyCmdBuild(World w, in Cmd cmd)
        {
            int uIdx = w.FindUnitIndex(cmd.UnitID);
            if (uIdx < 0) return;
            var u = w.Units[uIdx];
            if (u.State == UnitState.Dead || u.Owner != cmd.Player || u.Type != UnitType.Worker) return;
            var bt = (BuildingType)cmd.TargetID;
            if (!SimConstants.BuildingStats.TryGetValue(bt, out var s) || bt == BuildingType.HQ) return;
            if (cmd.Player >= w.Players.Count) return;
            if (w.Players[cmd.Player].Crystal < Fixed32.FromInt(s.Cost)) return;
            var pos = cmd.TargetPos;
            int sz = s.SizeCells;
            if (pos.X.ToInt() < 0 || pos.Y.ToInt() < 0 || pos.X.ToInt()+sz > w.NavGrid.W || pos.Y.ToInt()+sz > w.NavGrid.H) return;
            if (!IsAABBFree(w, pos, sz)) return;
            var p = w.Players[cmd.Player]; p.Crystal = p.Crystal - Fixed32.FromInt(s.Cost); w.Players[cmd.Player] = p;
            var b = new Building { ID = w.NextID++, Owner = cmd.Player, Type = bt, SizeCells = s.SizeCells, Pos = pos, HP = Fixed32.One, MaxHP = Fixed32.FromInt(s.MaxHP), State = BuildingState.Constructing };
            w.Buildings.Add(b);
            u.State = UnitState.Building; u.TargetID = b.ID; u.MoveTo = pos; w.Units[uIdx] = u;
        }

        private static bool IsAABBFree(World w, Vec2 pos, int size)
        {
            int sx = pos.X.ToInt(), sy = pos.Y.ToInt();
            for (int i = 0; i < w.Buildings.Count; i++)
            { var b = w.Buildings[i]; if (b.State == BuildingState.Dead) continue; int bx = b.Pos.X.ToInt(), by = b.Pos.Y.ToInt(), bs = b.SizeCells; if (sx < bx+bs && sx+size > bx && sy < by+bs && sy+size > by) return false; }
            return true;
        }
    }
}

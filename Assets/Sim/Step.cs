using System;

namespace RTS.Sim
{
    public static class SimStep
    {
        public static readonly Fixed32 AttackRange = Fixed32.FromInt(2);
        public static readonly Fixed32 AttackDamage = Fixed32.Half;

        public static void Step(World w, Cmd[] cmds)
        {
            w.Tick++;

            ApplyCommands(w, cmds);

            SortUnitsByID(w);
            SortBuildingsByID(w);
            SortCrystalsByID(w);

            for (int i = 0; i < w.Units.Count; i++)
            {
                var u = w.Units[i];
                if (u.State == UnitState.Dead) continue;

                switch (u.State)
                {
                    case UnitState.Moving:
                        StepMove(w, ref u);
                        break;
                    case UnitState.Mining:
                        StepMining(w, ref u);
                        break;
                    case UnitState.Returning:
                        StepReturning(w, ref u);
                        break;
                    case UnitState.Attacking:
                        StepAttack(w, ref u);
                        break;
                    case UnitState.Idle:
                        StepAttack(w, ref u);
                        break;
                }
                w.Units[i] = u;
            }

            TickProduction(w);
            w.RemoveDead();
            w.RemoveDeadBuildings();
            w.RemoveDeadCrystals();
        }

        private static void ApplyCommands(World w, Cmd[] cmds)
        {
            for (int c = 0; c < cmds.Length; c++)
            {
                ref readonly var cmd = ref cmds[c];

                switch (cmd.Op)
                {
                    case CmdOp.Move:
                    {
                        int uIdx = w.FindUnitIndex(cmd.UnitID);
                        if (uIdx < 0) continue;
                        var u = w.Units[uIdx];
                        if (u.State == UnitState.Dead || u.Owner != cmd.Player) continue;

                        if (u.Type == UnitType.Worker)
                        {
                            int crystIdx = w.FindCrystalAt(cmd.TargetPos);
                            if (crystIdx >= 0)
                            {
                                u.State = UnitState.Mining;
                                u.TargetID = w.Crystals[crystIdx].ID;
                                u.MoveTo = w.Crystals[crystIdx].Pos;
                                u.CarryAmount = Fixed32.Zero;
                                w.Units[uIdx] = u;
                                continue;
                            }
                        }

                        u.State = UnitState.Moving;
                        u.MoveTo = cmd.TargetPos;
                        u.TargetID = 0;
                        w.Units[uIdx] = u;
                        break;
                    }
                    case CmdOp.Attack:
                    {
                        int uIdx = w.FindUnitIndex(cmd.UnitID);
                        if (uIdx < 0) continue;
                        var u = w.Units[uIdx];
                        if (u.State == UnitState.Dead || u.Owner != cmd.Player) continue;
                        if (u.Range <= Fixed32.Zero) continue;
                        u.State = UnitState.Idle;
                        u.TargetID = cmd.TargetID;
                        w.Units[uIdx] = u;
                        break;
                    }
                    case CmdOp.Stop:
                    {
                        int uIdx = w.FindUnitIndex(cmd.UnitID);
                        if (uIdx < 0) continue;
                        var u = w.Units[uIdx];
                        if (u.State == UnitState.Dead || u.Owner != cmd.Player) continue;
                        u.State = UnitState.Idle;
                        u.TargetID = 0;
                        u.CarryAmount = Fixed32.Zero;
                        w.Units[uIdx] = u;
                        break;
                    }
                    case CmdOp.Train:
                        ApplyCmdTrain(w, cmd);
                        break;
                }
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

        private static void StepMining(World w, ref Unit u)
        {
            if (u.TargetID == 0) { u.State = UnitState.Idle; return; }

            int crystalIdx = w.FindCrystalIndex(u.TargetID);
            if (crystalIdx < 0)
            {
                int nearest = w.FindNearestCrystalIndex(u.Pos);
                if (nearest < 0) { u.State = UnitState.Idle; u.TargetID = 0; return; }
                u.TargetID = w.Crystals[nearest].ID;
                crystalIdx = nearest;
            }

            var crystal = w.Crystals[crystalIdx];

            if (u.CarryAmount <= Fixed32.Zero)
                u.CarryAmount = Fixed32.FromInt(SimConstants.MiningTicksPerTrip);

            u.CarryAmount = u.CarryAmount - Fixed32.One;
            if (u.CarryAmount > Fixed32.Zero)
            {
                w.Crystals[crystalIdx] = crystal;
                return;
            }

            var take = Fixed32.FromInt(SimConstants.CarryCapacity);
            if (crystal.Remaining < take) take = crystal.Remaining;
            crystal.Remaining = crystal.Remaining - take;
            u.CarryAmount = take;
            w.Crystals[crystalIdx] = crystal;

            int hqIdx = w.FindNearestOwnHQIndex(u.Pos, u.Owner);
            if (hqIdx < 0) { u.State = UnitState.Idle; u.CarryAmount = Fixed32.Zero; return; }

            u.State = UnitState.Returning;
            u.TargetID = w.Buildings[hqIdx].ID;
            u.MoveTo = w.Buildings[hqIdx].Pos;
        }

        private static void StepReturning(World w, ref Unit u)
        {
            if (u.TargetID == 0) { u.State = UnitState.Idle; return; }

            int hqIdx = w.FindBuildingIndex(u.TargetID);
            if (hqIdx < 0 || w.Buildings[hqIdx].Owner != u.Owner)
            {
                hqIdx = w.FindNearestOwnHQIndex(u.Pos, u.Owner);
                if (hqIdx < 0) { u.State = UnitState.Idle; u.CarryAmount = Fixed32.Zero; return; }
                u.TargetID = w.Buildings[hqIdx].ID;
                u.MoveTo = w.Buildings[hqIdx].Pos;
            }

            var newPos = Vec2.MoveToward(u.Pos, u.MoveTo, u.Speed);
            newPos = new Vec2(
                newPos.X.Clamp(Fixed32.Zero, w.MapSizeX),
                newPos.Y.Clamp(Fixed32.Zero, w.MapSizeY));
            u.Pos = newPos;

            var hq = w.Buildings[hqIdx];
            var arrivalRange = Fixed32.FromInt(hq.SizeCells);
            var arrivalRangeSq = arrivalRange * arrivalRange;
            if (u.Pos.DistSq(hq.Pos) <= arrivalRangeSq)
            {
                if (u.Owner < w.Players.Count)
                {
                    var p = w.Players[u.Owner];
                    p.Crystal = p.Crystal + u.CarryAmount;
                    w.Players[u.Owner] = p;
                }
                u.CarryAmount = Fixed32.Zero;

                int nearest = w.FindNearestCrystalIndex(u.Pos);
                if (nearest < 0) { u.State = UnitState.Idle; u.TargetID = 0; return; }
                u.State = UnitState.Mining;
                u.TargetID = w.Crystals[nearest].ID;
                u.MoveTo = w.Crystals[nearest].Pos;
                u.CarryAmount = Fixed32.Zero;
            }
        }

        private static void TickProduction(World w)
        {
            for (int i = 0; i < w.Buildings.Count; i++)
            {
                var b = w.Buildings[i];
                if (b.State != BuildingState.Ready) continue;
                if (b.ProductionQueue == null || b.ProductionQueue.Length == 0) continue;

                if (b.ProductionQueue[0].TicksLeft > 0)
                    b.ProductionQueue[0].TicksLeft--;

                if (b.ProductionQueue[0].TicksLeft > 0)
                { w.Buildings[i] = b; continue; }

                var spawnPos = FindEdgeSpawnCell(w, b);
                if (spawnPos == null) { w.Buildings[i] = b; continue; }

                var stats = SimConstants.UnitStats[b.ProductionQueue[0].UnitType];
                var newUnit = new Unit
                {
                    ID = w.NextID++,
                    Owner = b.Owner,
                    Type = b.ProductionQueue[0].UnitType,
                    Pos = spawnPos.Value,
                    HP = Fixed32.FromInt(stats.MaxHP),
                    MaxHP = Fixed32.FromInt(stats.MaxHP),
                    Speed = new Fixed32((int)(stats.Speed * 65536f)),
                    Range = new Fixed32((int)(stats.Range * 65536f)),
                    Damage = new Fixed32((int)(stats.Damage * 65536f)),
                    VisionRange = Fixed32.FromInt(stats.VisionRange),
                    State = UnitState.Idle,
                };

                if (b.RallyPoint.X.Raw != 0 || b.RallyPoint.Y.Raw != 0)
                {
                    newUnit.State = UnitState.Moving;
                    newUnit.MoveTo = b.RallyPoint;
                }

                w.Units.Add(newUnit);

                var newQueue = new QueueItem[b.ProductionQueue.Length - 1];
                for (int q = 1; q < b.ProductionQueue.Length; q++)
                    newQueue[q - 1] = b.ProductionQueue[q];
                b.ProductionQueue = newQueue;
                w.Buildings[i] = b;
            }
        }

        private static Vec2? FindEdgeSpawnCell(World w, in Building b)
        {
            int size = b.SizeCells;
            int startX = b.Pos.X.ToInt();
            int startY = b.Pos.Y.ToInt();
            for (int dx = -1; dx <= size; dx++)
            {
                for (int dy = -1; dy <= size; dy++)
                {
                    if (dx >= 0 && dx < size && dy >= 0 && dy < size) continue;
                    int cx = startX + dx;
                    int cy = startY + dy;
                    if (cx < 0 || cy < 0 || cx >= w.NavGrid.W || cy >= w.NavGrid.H) continue;
                    if (!IsCellBlocked(w, cx, cy))
                        return Vec2.FromInt(cx, cy);
                }
            }
            return null;
        }

        private static bool IsCellBlocked(World w, int cx, int cy)
        {
            for (int i = 0; i < w.Buildings.Count; i++)
            {
                var bld = w.Buildings[i];
                if (bld.State == BuildingState.Dead) continue;
                int bx = bld.Pos.X.ToInt(), by = bld.Pos.Y.ToInt(), sz = bld.SizeCells;
                if (cx >= bx && cx < bx + sz && cy >= by && cy < by + sz) return true;
            }
            int idx = cy * w.NavGrid.W + cx;
            int word = idx / 64, bit = idx % 64;
            if (word < w.NavGrid.Blocked.Length && (w.NavGrid.Blocked[word] & (1UL << bit)) != 0)
                return true;
            return false;
        }

        private static void ApplyCmdTrain(World w, in Cmd cmd)
        {
            int bldIdx = w.FindBuildingIndex(cmd.UnitID);
            if (bldIdx < 0) return;

            var b = w.Buildings[bldIdx];
            if (b.Owner != cmd.Player) return;
            if (b.State != BuildingState.Ready) return;
            if (b.ProductionQueue != null && b.ProductionQueue.Length >= SimConstants.MaxQueueLength) return;

            var unitType = (UnitType)cmd.TargetID;
            if (unitType < UnitType.Worker || unitType > UnitType.Cavalry) return;

            var bldStats = SimConstants.BuildingStats[b.Type];
            bool canTrain = false;
            foreach (var ut in bldStats.Trains)
                if (ut == unitType) { canTrain = true; break; }
            if (!canTrain) return;

            var unitStats = SimConstants.UnitStats[unitType];
            if (cmd.Player >= w.Players.Count) return;
            var p = w.Players[cmd.Player];
            var cost = Fixed32.FromInt(unitStats.Cost);
            if (p.Crystal < cost) return;

            p.Crystal = p.Crystal - cost;
            w.Players[cmd.Player] = p;

            var newQueue = b.ProductionQueue == null
                ? new QueueItem[1]
                : new QueueItem[b.ProductionQueue.Length + 1];
            if (b.ProductionQueue != null)
                Array.Copy(b.ProductionQueue, newQueue, b.ProductionQueue.Length);
            newQueue[newQueue.Length - 1] = new QueueItem
            {
                UnitType = unitType,
                TicksLeft = unitStats.TrainTicks,
                StartTick = w.Tick,
            };
            b.ProductionQueue = newQueue;
            w.Buildings[bldIdx] = b;
        }

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

        private static void SortBuildingsByID(World w)
        {
            var list = w.Buildings;
            for (int i = 1; i < list.Count; i++)
            {
                var key = list[i];
                int j = i - 1;
                while (j >= 0 && list[j].ID > key.ID) { list[j + 1] = list[j]; j--; }
                list[j + 1] = key;
            }
        }

        private static void SortCrystalsByID(World w)
        {
            var list = w.Crystals;
            for (int i = 1; i < list.Count; i++)
            {
                var key = list[i];
                int j = i - 1;
                while (j >= 0 && list[j].ID > key.ID) { list[j + 1] = list[j]; j--; }
                list[j + 1] = key;
            }
        }
    }
}

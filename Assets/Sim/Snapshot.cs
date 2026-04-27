using RTS.Core;

namespace RTS.Sim
{
    /// <summary>
    /// Marshal/Unmarshal world state. Ported from Go internal/sim/snapshot.go.
    /// Byte-layout authority: docs/superpowers/plans/2026-04-19-sub2-phase0-foundation.md
    /// </summary>
    public static class Snapshot
    {
        public static byte[] Marshal(World w)
        {
            var bw = new ByteWriter(2048);

            bw.WriteU32(w.Tick);
            bw.WriteU64(w.Seed);
            bw.WriteU64(w.Rand.State);
            bw.WriteU32(w.NextID);
            bw.WriteU32((uint)w.MapSizeX.Raw);
            bw.WriteU32((uint)w.MapSizeY.Raw);
            bw.WriteU32((uint)w.Units.Count);
            bw.WriteU32((uint)w.Buildings.Count);
            bw.WriteU32((uint)w.Crystals.Count);
            bw.WriteU32((uint)w.Players.Count);
            bw.WriteU32((uint)w.NavGrid.W);
            bw.WriteU32((uint)w.NavGrid.H);

            for (int i = 0; i < w.Units.Count; i++)
            {
                var u = w.Units[i];
                bw.WriteU32(u.ID);
                bw.WriteU8(u.Owner);
                bw.WriteU8((byte)u.State);
                bw.WriteU8((byte)u.Type);
                bw.WriteU32((uint)u.HP.Raw);
                bw.WriteU32((uint)u.MaxHP.Raw);
                bw.WriteU32((uint)u.Speed.Raw);
                bw.WriteU32((uint)u.Range.Raw);
                bw.WriteU32((uint)u.Damage.Raw);
                bw.WriteU32((uint)u.VisionRange.Raw);
                bw.WriteU32((uint)u.CarryAmount.Raw);
                bw.WriteU32((uint)u.Pos.X.Raw);
                bw.WriteU32((uint)u.Pos.Y.Raw);
                bw.WriteU32(u.TargetID);
                bw.WriteU32((uint)u.MoveTo.X.Raw);
                bw.WriteU32((uint)u.MoveTo.Y.Raw);
                bw.WriteU32((uint)u.AttackMoveTarget.X.Raw);
                bw.WriteU32((uint)u.AttackMoveTarget.Y.Raw);
                int pathLen = u.Path == null ? 0 : u.Path.Length;
                bw.WriteU32((uint)pathLen);
                for (int p = 0; p < pathLen; p++)
                {
                    bw.WriteU32((uint)u.Path[p].X.Raw);
                    bw.WriteU32((uint)u.Path[p].Y.Raw);
                }
            }

            for (int i = 0; i < w.Buildings.Count; i++)
            {
                var b = w.Buildings[i];
                bw.WriteU32(b.ID);
                bw.WriteU8(b.Owner);
                bw.WriteU8((byte)b.Type);
                bw.WriteU8((byte)b.State);
                bw.WriteU8(b.SizeCells);
                bw.WriteU32((uint)b.Pos.X.Raw);
                bw.WriteU32((uint)b.Pos.Y.Raw);
                bw.WriteU32((uint)b.HP.Raw);
                bw.WriteU32((uint)b.MaxHP.Raw);
                bw.WriteU32((uint)b.ConstructProgress.Raw);
                bw.WriteU32((uint)b.RallyPoint.X.Raw);
                bw.WriteU32((uint)b.RallyPoint.Y.Raw);
                int qLen = b.ProductionQueue == null ? 0 : b.ProductionQueue.Length;
                bw.WriteU32((uint)qLen);
                for (int q = 0; q < qLen; q++)
                {
                    bw.WriteU8((byte)b.ProductionQueue[q].UnitType);
                    bw.WriteU32(b.ProductionQueue[q].TicksLeft);
                    bw.WriteU32(b.ProductionQueue[q].StartTick);
                }
            }

            for (int i = 0; i < w.Crystals.Count; i++)
            {
                var c = w.Crystals[i];
                bw.WriteU32(c.ID);
                bw.WriteU32((uint)c.Pos.X.Raw);
                bw.WriteU32((uint)c.Pos.Y.Raw);
                bw.WriteU32((uint)c.Remaining.Raw);
            }

            for (int i = 0; i < w.Players.Count; i++)
            {
                var p = w.Players[i];
                bw.WriteU8(p.ID);
                bw.WriteU32((uint)p.Crystal.Raw);
                bw.WriteU8(p.Surrendered ? (byte)1 : (byte)0);
            }

            int words = w.NavGrid.Blocked == null ? 0 : w.NavGrid.Blocked.Length;
            bw.WriteU32((uint)words);
            for (int i = 0; i < words; i++)
                bw.WriteU64(w.NavGrid.Blocked[i]);

            return bw.ToArray();
        }

        public static World Unmarshal(byte[] data)
        {
            var br = new ByteReader(data);

            var w = new World();
            w.Tick = br.ReadU32();
            w.Seed = br.ReadU64();
            ulong randState = br.ReadU64();
            w.Rand = new SplitMix64(0) { State = randState };
            w.NextID = br.ReadU32();
            w.MapSizeX = Fixed32.FromRaw((int)br.ReadU32());
            w.MapSizeY = Fixed32.FromRaw((int)br.ReadU32());

            int unitCount   = (int)br.ReadU32();
            int bldCount    = (int)br.ReadU32();
            int crystCount  = (int)br.ReadU32();
            int playerCount = (int)br.ReadU32();
            int navW        = (int)br.ReadU32();
            int navH        = (int)br.ReadU32();

            w.Units     = new System.Collections.Generic.List<Unit>(unitCount);
            w.Buildings = new System.Collections.Generic.List<Building>(bldCount);
            w.Crystals  = new System.Collections.Generic.List<Crystal>(crystCount);
            w.Players   = new System.Collections.Generic.List<Player>(playerCount);

            for (int i = 0; i < unitCount; i++)
            {
                var u = new Unit
                {
                    ID = br.ReadU32(),
                    Owner = br.ReadU8(),
                    State = (UnitState)br.ReadU8(),
                    Type = (UnitType)br.ReadU8(),
                    HP = Fixed32.FromRaw((int)br.ReadU32()),
                    MaxHP = Fixed32.FromRaw((int)br.ReadU32()),
                    Speed = Fixed32.FromRaw((int)br.ReadU32()),
                    Range = Fixed32.FromRaw((int)br.ReadU32()),
                    Damage = Fixed32.FromRaw((int)br.ReadU32()),
                    VisionRange = Fixed32.FromRaw((int)br.ReadU32()),
                    CarryAmount = Fixed32.FromRaw((int)br.ReadU32()),
                    Pos = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32())),
                    TargetID = br.ReadU32(),
                    MoveTo = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32())),
                    AttackMoveTarget = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32())),
                };
                int pathLen = (int)br.ReadU32();
                if (pathLen > 0)
                {
                    u.Path = new Vec2[pathLen];
                    for (int p = 0; p < pathLen; p++)
                    {
                        u.Path[p] = new Vec2(
                            Fixed32.FromRaw((int)br.ReadU32()),
                            Fixed32.FromRaw((int)br.ReadU32()));
                    }
                }
                w.Units.Add(u);
            }

            for (int i = 0; i < bldCount; i++)
            {
                var b = new Building
                {
                    ID = br.ReadU32(),
                    Owner = br.ReadU8(),
                    Type = (BuildingType)br.ReadU8(),
                    State = (BuildingState)br.ReadU8(),
                    SizeCells = br.ReadU8(),
                    Pos = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32())),
                    HP = Fixed32.FromRaw((int)br.ReadU32()),
                    MaxHP = Fixed32.FromRaw((int)br.ReadU32()),
                    ConstructProgress = Fixed32.FromRaw((int)br.ReadU32()),
                    RallyPoint = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32())),
                };
                int qLen = (int)br.ReadU32();
                if (qLen > 0)
                {
                    b.ProductionQueue = new QueueItem[qLen];
                    for (int q = 0; q < qLen; q++)
                    {
                        b.ProductionQueue[q] = new QueueItem
                        {
                            UnitType = (UnitType)br.ReadU8(),
                            TicksLeft = br.ReadU32(),
                            StartTick = br.ReadU32(),
                        };
                    }
                }
                w.Buildings.Add(b);
            }

            for (int i = 0; i < crystCount; i++)
            {
                w.Crystals.Add(new Crystal
                {
                    ID = br.ReadU32(),
                    Pos = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32())),
                    Remaining = Fixed32.FromRaw((int)br.ReadU32()),
                });
            }

            for (int i = 0; i < playerCount; i++)
            {
                w.Players.Add(new Player
                {
                    ID = br.ReadU8(),
                    Crystal = Fixed32.FromRaw((int)br.ReadU32()),
                    Surrendered = br.ReadU8() != 0,
                });
            }

            int words = (int)br.ReadU32();
            var nav = new NavGrid { W = navW, H = navH, Blocked = new ulong[words] };
            for (int i = 0; i < words; i++)
                nav.Blocked[i] = br.ReadU64();
            w.NavGrid = nav;

            return w;
        }
    }
}

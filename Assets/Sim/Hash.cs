using System.Collections.Generic;

namespace RTS.Sim
{
    /// <summary>
    /// FNV-1a-64 canonical state hash. Ported from Go internal/sim/hash.go.
    /// Byte-layout authority: docs/superpowers/plans/2026-04-19-sub2-phase0-foundation.md
    /// </summary>
    public static class SimHash
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static ulong Hash(World w)
        {
            ulong h = FnvOffsetBasis;

            h = MixU32(h, w.Tick);

            h = MixU32(h, (uint)w.Units.Count);
            for (int i = 0; i < w.Units.Count; i++)
                h = HashUnit(h, w.Units[i]);

            h = HashBuildings(h, w.Buildings);
            h = HashCrystals(h, w.Crystals);
            h = HashPlayers(h, w.Players);
            h = HashNavGrid(h, w.NavGrid);

            return h;
        }

        private static ulong HashUnit(ulong h, in Unit u)
        {
            h = MixU32(h, u.ID);
            h = MixByte(h, u.Owner);
            h = MixByte(h, (byte)u.State);
            h = MixByte(h, (byte)u.Type);
            h = MixU32(h, (uint)u.HP.Raw);
            h = MixU32(h, (uint)u.Range.Raw);
            h = MixU32(h, (uint)u.Damage.Raw);
            h = MixU32(h, (uint)u.VisionRange.Raw);
            h = MixU32(h, (uint)u.CarryAmount.Raw);
            h = MixU32(h, (uint)u.Pos.X.Raw);
            h = MixU32(h, (uint)u.Pos.Y.Raw);
            h = MixU32(h, u.TargetID);
            h = MixU32(h, (uint)u.AttackMoveTarget.X.Raw);
            h = MixU32(h, (uint)u.AttackMoveTarget.Y.Raw);

            int pathLen = u.Path == null ? 0 : u.Path.Length;
            h = MixU32(h, (uint)pathLen);
            for (int i = 0; i < pathLen; i++)
            {
                h = MixU32(h, (uint)u.Path[i].X.Raw);
                h = MixU32(h, (uint)u.Path[i].Y.Raw);
            }
            return h;
        }

        private static ulong HashBuildings(ulong h, List<Building> list)
        {
            h = MixU32(h, (uint)list.Count);
            var sorted = new List<Building>(list);
            sorted.Sort((a, b) => a.ID.CompareTo(b.ID));
            for (int i = 0; i < sorted.Count; i++)
                h = HashBuilding(h, sorted[i]);
            return h;
        }

        private static ulong HashBuilding(ulong h, in Building b)
        {
            h = MixU32(h, b.ID);
            h = MixByte(h, b.Owner);
            h = MixByte(h, (byte)b.Type);
            h = MixByte(h, (byte)b.State);
            h = MixByte(h, b.SizeCells);
            h = MixU32(h, (uint)b.Pos.X.Raw);
            h = MixU32(h, (uint)b.Pos.Y.Raw);
            h = MixU32(h, (uint)b.HP.Raw);
            h = MixU32(h, (uint)b.MaxHP.Raw);
            h = MixU32(h, (uint)b.ConstructProgress.Raw);
            h = MixU32(h, (uint)b.RallyPoint.X.Raw);
            h = MixU32(h, (uint)b.RallyPoint.Y.Raw);

            int qLen = b.ProductionQueue == null ? 0 : b.ProductionQueue.Length;
            h = MixU32(h, (uint)qLen);
            for (int i = 0; i < qLen; i++)
            {
                h = MixByte(h, (byte)b.ProductionQueue[i].UnitType);
                h = MixU32(h, b.ProductionQueue[i].TicksLeft);
                h = MixU32(h, b.ProductionQueue[i].StartTick);
            }
            return h;
        }

        private static ulong HashCrystals(ulong h, List<Crystal> list)
        {
            h = MixU32(h, (uint)list.Count);
            var sorted = new List<Crystal>(list);
            sorted.Sort((a, b) => a.ID.CompareTo(b.ID));
            for (int i = 0; i < sorted.Count; i++)
            {
                h = MixU32(h, sorted[i].ID);
                h = MixU32(h, (uint)sorted[i].Pos.X.Raw);
                h = MixU32(h, (uint)sorted[i].Pos.Y.Raw);
                h = MixU32(h, (uint)sorted[i].Remaining.Raw);
            }
            return h;
        }

        private static ulong HashPlayers(ulong h, List<Player> list)
        {
            h = MixU32(h, (uint)list.Count);
            var sorted = new List<Player>(list);
            sorted.Sort((a, b) => a.ID.CompareTo(b.ID));
            for (int i = 0; i < sorted.Count; i++)
            {
                h = MixByte(h, sorted[i].ID);
                h = MixU32(h, (uint)sorted[i].Crystal.Raw);
                h = MixByte(h, sorted[i].Surrendered ? (byte)1 : (byte)0);
            }
            return h;
        }

        private static ulong HashNavGrid(ulong h, NavGrid g)
        {
            h = MixU32(h, (uint)g.W);
            h = MixU32(h, (uint)g.H);
            int words = g.Blocked == null ? 0 : g.Blocked.Length;
            h = MixU32(h, (uint)words);
            for (int i = 0; i < words; i++)
                h = MixU64(h, g.Blocked[i]);
            return h;
        }

        private static ulong MixByte(ulong h, byte b)
        {
            h ^= b;
            h *= FnvPrime;
            return h;
        }

        private static ulong MixU32(ulong h, uint v)
        {
            h = MixByte(h, (byte)(v & 0xFF));
            h = MixByte(h, (byte)((v >> 8) & 0xFF));
            h = MixByte(h, (byte)((v >> 16) & 0xFF));
            h = MixByte(h, (byte)((v >> 24) & 0xFF));
            return h;
        }

        private static ulong MixU64(ulong h, ulong v)
        {
            h = MixU32(h, (uint)(v & 0xFFFFFFFFUL));
            h = MixU32(h, (uint)((v >> 32) & 0xFFFFFFFFUL));
            return h;
        }
    }
}

namespace RTS.Sim
{
    /// <summary>
    /// FNV-1a-64 canonical state hash. Ported from Go internal/sim/hash.go.
    /// 22-byte per-unit layout: ID(4)+Owner(1)+State(1)+HP(4)+PosX(4)+PosY(4)+TargetID(4)
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
            {
                h = HashUnit(h, w.Units[i]);
            }

            return h;
        }

        private static ulong HashUnit(ulong h, in Unit u)
        {
            h = MixU32(h, u.ID);
            h = MixByte(h, u.Owner);
            h = MixByte(h, (byte)u.State);
            h = MixU32(h, (uint)u.HP.Raw);
            h = MixU32(h, (uint)u.Pos.X.Raw);
            h = MixU32(h, (uint)u.Pos.Y.Raw);
            h = MixU32(h, u.TargetID);
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
    }
}

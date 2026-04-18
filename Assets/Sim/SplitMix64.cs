namespace RTS.Sim
{
    /// <summary>
    /// Deterministic PRNG. Ported from Go internal/sim/rand.go.
    /// Same seed + same call sequence = same output on every platform.
    /// </summary>
    public class SplitMix64
    {
        public ulong State;

        public SplitMix64(ulong seed)
        {
            State = seed;
        }

        public ulong Next()
        {
            State += 0x9e3779b97f4a7c15UL;
            ulong z = State;
            z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
            z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
            return z ^ (z >> 31);
        }

        public int Intn(int n)
        {
            if (n <= 0) return 0;
            return (int)(Next() % (ulong)n);
        }
    }
}

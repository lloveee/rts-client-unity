namespace RTS.Sim
{
    /// <summary>
    /// Trig functions using a 1024-entry lookup table for sin.
    /// sinTable values are loaded from Go golden export — NOT computed at runtime.
    /// Ported from Go internal/sim/fixed/trig.go.
    /// </summary>
    public static class Trig
    {
        private const int SinTableSize = 1024;

        private static readonly int[] _sinTable = new int[SinTableSize];
        private static bool _initialized;

        /// <summary>
        /// Initialize the sin table from golden data (exported by Go).
        /// Must be called once at startup before any Sim.Step calls.
        /// </summary>
        public static void InitSinTable(int[] goldenValues)
        {
            for (int i = 0; i < SinTableSize; i++)
                _sinTable[i] = goldenValues[i];
            _initialized = true;
        }

        public static int GetSinTableEntry(int index) => _sinTable[index];

        public static Fixed32 Sin(Fixed32 angle)
        {
            int twoPi = Fixed32.TwoPi.Raw;
            int a = angle.Raw % twoPi;
            if (a < 0) a += twoPi;

            int halfPi = twoPi / 4;
            int pi = twoPi / 2;

            int idx;
            bool neg;

            if (a < halfPi)
            {
                idx = (int)((long)a * SinTableSize / halfPi);
                neg = false;
            }
            else if (a < pi)
            {
                idx = (int)((long)(pi - a) * SinTableSize / halfPi);
                neg = false;
            }
            else if (a < pi + halfPi)
            {
                idx = (int)((long)(a - pi) * SinTableSize / halfPi);
                neg = true;
            }
            else
            {
                idx = (int)((long)(twoPi - a) * SinTableSize / halfPi);
                neg = true;
            }

            if (idx >= SinTableSize) idx = SinTableSize - 1;
            if (idx < 0) idx = 0;

            var v = new Fixed32(_sinTable[idx]);
            return neg ? -v : v;
        }

        public static Fixed32 Cos(Fixed32 angle)
        {
            var halfPi = new Fixed32(Fixed32.TwoPi.Raw / 4);
            return Sin(angle + halfPi);
        }

        public static Fixed32 Atan2(Fixed32 y, Fixed32 x)
        {
            if (x == Fixed32.Zero && y == Fixed32.Zero)
                return Fixed32.Zero;

            var halfPi = new Fixed32(Fixed32.TwoPi.Raw / 4);
            var pi = new Fixed32(Fixed32.TwoPi.Raw / 2);

            if (x == Fixed32.Zero)
                return y > Fixed32.Zero ? halfPi : -halfPi;

            var ax = Fixed32.Abs(x);
            var ay = Fixed32.Abs(y);

            Fixed32 angle;
            if (ax >= ay)
            {
                var ratio = ay / ax;
                var r2 = ratio * ratio;
                angle = ratio * new Fixed32(64337) - r2 * ratio * new Fixed32(12864);
            }
            else
            {
                var ratio = ax / ay;
                var r2 = ratio * ratio;
                angle = halfPi - (ratio * new Fixed32(64337) - r2 * ratio * new Fixed32(12864));
            }

            if (x < Fixed32.Zero)
                angle = pi - angle;
            if (y < Fixed32.Zero)
                angle = -angle;

            return angle;
        }
    }
}

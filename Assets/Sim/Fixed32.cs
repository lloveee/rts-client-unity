using System;
using System.Runtime.CompilerServices;

namespace RTS.Sim
{
    /// <summary>
    /// Q16.16 fixed-point number. Ported from Go internal/sim/fixed/fixed.go.
    /// Must produce bit-identical results to the Go version.
    /// </summary>
    public readonly struct Fixed32 : IEquatable<Fixed32>, IComparable<Fixed32>
    {
        public const int Shift = 16;

        public static readonly Fixed32 Zero = new(0);
        public static readonly Fixed32 One = new(1 << Shift);
        public static readonly Fixed32 Half = new(1 << (Shift - 1));
        public static readonly Fixed32 Max = new(0x7FFFFFFF);
        public static readonly Fixed32 Min = new(-0x80000000);
        public static readonly Fixed32 Pi = new(205887);
        public static readonly Fixed32 TwoPi = new(411775);
        public static readonly Fixed32 Eps = new(1);

        public readonly int Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fixed32(int raw) => Raw = raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 FromInt(int v) => new(v << Shift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 FromRaw(int v) => new(v);

        public int ToInt()
        {
            long v = Raw;
            if (v >= 0)
                return (int)(v >> Shift);
            return (int)(-((-v) >> Shift));
        }

        public float ToFloat() => (float)Raw / One.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 operator +(Fixed32 a, Fixed32 b) => new(a.Raw + b.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 operator -(Fixed32 a, Fixed32 b) => new(a.Raw - b.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 operator *(Fixed32 a, Fixed32 b) =>
            new((int)(((long)a.Raw * b.Raw) >> Shift));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 operator /(Fixed32 a, Fixed32 b) =>
            new((int)(((long)a.Raw << Shift) / b.Raw));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed32 operator -(Fixed32 a) => new(-a.Raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Fixed32 a, Fixed32 b) => a.Raw < b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Fixed32 a, Fixed32 b) => a.Raw > b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Fixed32 a, Fixed32 b) => a.Raw <= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Fixed32 a, Fixed32 b) => a.Raw >= b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Fixed32 a, Fixed32 b) => a.Raw == b.Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Fixed32 a, Fixed32 b) => a.Raw != b.Raw;

        public static Fixed32 Abs(Fixed32 a) => a.Raw < 0 ? new(-a.Raw) : a;

        public Fixed32 Floor()
        {
            return new(Raw & ~(One.Raw - 1));
        }

        public Fixed32 Ceil()
        {
            if ((Raw & (One.Raw - 1)) == 0)
                return this;
            return new((Raw & ~(One.Raw - 1)) + One.Raw);
        }

        public Fixed32 Clamp(Fixed32 lo, Fixed32 hi)
        {
            if (Raw < lo.Raw) return lo;
            if (Raw > hi.Raw) return hi;
            return this;
        }

        public static Fixed32 Sqrt(Fixed32 a)
        {
            if (a.Raw <= 0) return Zero;

            long n = (long)a.Raw << Shift;
            long result = 0;
            long bit = 1L << 30;

            while (bit > 0)
            {
                long trial = result | bit;
                if (trial * trial <= n)
                    result = trial;
                bit >>= 1;
            }

            return new((int)result);
        }

        public static Fixed32 Min2(Fixed32 a, Fixed32 b) => a.Raw < b.Raw ? a : b;
        public static Fixed32 Max2(Fixed32 a, Fixed32 b) => a.Raw > b.Raw ? a : b;

        public bool Equals(Fixed32 other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is Fixed32 f && Raw == f.Raw;
        public override int GetHashCode() => Raw;
        public int CompareTo(Fixed32 other) => Raw.CompareTo(other.Raw);
        public override string ToString() => $"Fixed32({ToFloat():F4})";
    }
}

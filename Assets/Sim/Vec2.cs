using System.Runtime.CompilerServices;

namespace RTS.Sim
{
    public readonly struct Vec2
    {
        public readonly Fixed32 X;
        public readonly Fixed32 Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2(Fixed32 x, Fixed32 y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 FromInt(int x, int y) =>
            new(Fixed32.FromInt(x), Fixed32.FromInt(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator +(Vec2 a, Vec2 b) =>
            new(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator -(Vec2 a, Vec2 b) =>
            new(a.X - b.X, a.Y - b.Y);

        public Vec2 Scale(Fixed32 s) => new(X * s, Y * s);

        public Fixed32 Dot(Vec2 b) => X * b.X + Y * b.Y;

        public Fixed32 LenSq() => Dot(this);

        public Fixed32 Len() => Fixed32.Sqrt(LenSq());

        public Fixed32 DistSq(Vec2 b) => (this - b).LenSq();

        public Fixed32 Dist(Vec2 b) => Fixed32.Sqrt(DistSq(b));

        public Vec2 Normalize()
        {
            var l = Len();
            if (l == Fixed32.Zero) return new Vec2(Fixed32.Zero, Fixed32.Zero);
            return new Vec2(X / l, Y / l);
        }

        public static Vec2 MoveToward(Vec2 from, Vec2 target, Fixed32 maxDist)
        {
            var diff = target - from;
            var dSq = diff.LenSq();
            if (dSq <= maxDist * maxDist)
                return target;
            var d = Fixed32.Sqrt(dSq);
            return from + diff.Scale(maxDist / d);
        }

        public override string ToString() => $"({X}, {Y})";
    }
}

using System;

namespace RTS.Tests
{
    [Serializable]
    public class GoldenData
    {
        public long seed;
        public int mapW;
        public int mapH;
        public int players;
        public int unitsPerPlayer;
        public int ticks;
        public GoldenCmd[] commands;
        public string[] tickHashes;
        public GoldenFixedArith[] fixedArithTests;
        public int[] sinTableRaw;
        public long[] splitMixSequence;
        public GoldenVec2Test[] vec2Tests;
        public GoldenAtan2Test[] atan2Tests;
    }

    [Serializable]
    public class GoldenCmd
    {
        public int tick;
        public int player;
        public int op;
        public int unitID;
        public int targetX;
        public int targetY;
        public int targetID;
    }

    [Serializable]
    public class GoldenFixedArith
    {
        public string op;
        public int a;
        public int b;
        public int expect;
    }

    [Serializable]
    public class GoldenVec2Test
    {
        public string op;
        public int ax, ay, bx, by, expectX, expectY;
    }

    [Serializable]
    public class GoldenAtan2Test
    {
        public int y, x, expect;
    }
}

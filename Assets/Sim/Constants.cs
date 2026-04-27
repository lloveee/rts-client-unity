using System.Collections.Generic;

namespace RTS.Sim
{
    public static class SimConstants
    {
        public const int CarryCapacity = 5;
        public const int MiningTicksPerTrip = 30;
        public const int MaxQueueLength = 5;
        public const int CrystalsPerPlayer = 8;
        public const int CrystalStartValue = 1500;
        public const int StartingCrystal = 200;

        public static readonly Dictionary<UnitType, UnitStat> UnitStats =
            new Dictionary<UnitType, UnitStat>
        {
            [UnitType.Worker] = new UnitStat
            {
                MaxHP = 20, Speed = 0.8f, Range = 0, Damage = 0,
                VisionRange = 6, Cost = 50, TrainTicks = 10
            },
            [UnitType.Soldier] = new UnitStat
            {
                MaxHP = 60, Speed = 0.5f, Range = 1.5f, Damage = 1.0f,
                VisionRange = 8, Cost = 80, TrainTicks = 15
            },
            [UnitType.Archer] = new UnitStat
            {
                MaxHP = 40, Speed = 0.5f, Range = 6, Damage = 0.6f,
                VisionRange = 12, Cost = 120, TrainTicks = 20
            },
            [UnitType.Cavalry] = new UnitStat
            {
                MaxHP = 100, Speed = 1.2f, Range = 1.5f, Damage = 1.5f,
                VisionRange = 10, Cost = 200, TrainTicks = 30
            },
        };

        public static readonly Dictionary<BuildingType, BuildingStat> BuildingStats =
            new Dictionary<BuildingType, BuildingStat>
        {
            [BuildingType.HQ] = new BuildingStat
            {
                MaxHP = 800, VisionRange = 12, Cost = 0, BuildTicks = 0, SizeCells = 4,
                Trains = new[] { UnitType.Worker }
            },
            [BuildingType.Barracks] = new BuildingStat
            {
                MaxHP = 400, VisionRange = 8, Cost = 150, BuildTicks = 60, SizeCells = 3,
                Trains = new[] { UnitType.Soldier }
            },
            [BuildingType.Archery] = new BuildingStat
            {
                MaxHP = 400, VisionRange = 10, Cost = 150, BuildTicks = 60, SizeCells = 3,
                Trains = new[] { UnitType.Archer }
            },
            [BuildingType.Stable] = new BuildingStat
            {
                MaxHP = 400, VisionRange = 8, Cost = 150, BuildTicks = 60, SizeCells = 3,
                Trains = new[] { UnitType.Cavalry }
            },
        };

        public static Vec2[] CrystalPositions(byte playerID)
        {
            int baseX = playerID == 1 ? 75 : 5;
            return new Vec2[]
            {
                Vec2.FromInt(baseX, 10),     Vec2.FromInt(baseX + 5, 25),
                Vec2.FromInt(baseX + 10, 40), Vec2.FromInt(baseX + 15, 55),
                Vec2.FromInt(baseX + 20, 70), Vec2.FromInt(baseX + 5, 80),
                Vec2.FromInt(baseX + 10, 15), Vec2.FromInt(baseX + 15, 65),
            };
        }
    }

    public struct UnitStat
    {
        public int MaxHP;
        public float Speed;
        public float Range;
        public float Damage;
        public int VisionRange;
        public int Cost;
        public uint TrainTicks;
    }

    public struct BuildingStat
    {
        public int MaxHP;
        public int VisionRange;
        public int Cost;
        public uint BuildTicks;
        public byte SizeCells;
        public UnitType[] Trains;
    }
}

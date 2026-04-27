namespace RTS.Sim
{
    public enum BuildingType : byte
    {
        HQ = 1,
        Barracks = 2,
        Archery = 3,
        Stable = 4
    }

    public enum BuildingState : byte
    {
        Constructing = 0,
        Ready = 1,
        Dead = 2
    }

    public struct QueueItem
    {
        public UnitType UnitType;
        public uint TicksLeft;
        public uint StartTick;
    }

    public struct Building
    {
        public uint ID;
        public byte Owner;
        public BuildingType Type;
        public byte SizeCells;
        public Vec2 Pos;
        public Fixed32 HP;
        public Fixed32 MaxHP;
        public BuildingState State;
        public Fixed32 ConstructProgress;
        public Vec2 RallyPoint;
        public QueueItem[] ProductionQueue;
    }
}

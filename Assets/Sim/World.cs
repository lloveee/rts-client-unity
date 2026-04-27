using System.Collections.Generic;

namespace RTS.Sim
{
    public enum UnitState : byte
    {
        Idle = 0,
        Moving = 1,
        Attacking = 2,
        Mining = 3,
        Returning = 4,
        Building = 5,
        Dead = 6
    }

    public enum UnitType : byte
    {
        Worker = 1,
        Soldier = 2,
        Archer = 3,
        Cavalry = 4
    }

    public enum CmdOp : byte
    {
        Move = 1,
        Attack = 2,
        Stop = 3
    }

    public struct Unit
    {
        public uint ID;
        public byte Owner;
        public UnitType Type;
        public Vec2 Pos;
        public Fixed32 HP;
        public Fixed32 MaxHP;
        public Fixed32 Speed;
        public Fixed32 Range;
        public Fixed32 Damage;
        public Fixed32 VisionRange;
        public Fixed32 CarryAmount;
        public UnitState State;
        public uint TargetID;
        public Vec2 MoveTo;
        public Vec2 AttackMoveTarget;
        public Vec2[] Path;
    }

    public struct Cmd
    {
        public byte Player;
        public CmdOp Op;
        public uint UnitID;
        public Vec2 TargetPos;
        public uint TargetID;
    }

    public class World
    {
        public uint Tick;
        public ulong Seed;
        public SplitMix64 Rand;
        public uint NextID;
        public Fixed32 MapSizeX;
        public Fixed32 MapSizeY;

        public List<Unit> Units;
        public List<Building> Buildings;
        public List<Crystal> Crystals;
        public List<Player> Players;

        public NavGrid NavGrid;

        public World(ulong seed, int mapW, int mapH)
        {
            Tick = 0;
            Seed = seed;
            Rand = new SplitMix64(seed);
            Units = new List<Unit>(64);
            Buildings = new List<Building>(8);
            Crystals = new List<Crystal>(16);
            Players = new List<Player>(4);
            NextID = 1;
            MapSizeX = Fixed32.FromInt(mapW);
            MapSizeY = Fixed32.FromInt(mapH);
            NavGrid = new NavGrid(mapW, mapH);
        }

        public World() { }

        public uint SpawnUnit(byte owner, Vec2 pos, Fixed32 hp, Fixed32 speed)
        {
            uint id = NextID++;
            Units.Add(new Unit
            {
                ID = id,
                Owner = owner,
                Pos = pos,
                HP = hp,
                MaxHP = hp,
                Speed = speed,
                State = UnitState.Idle
            });
            return id;
        }

        public int FindUnitIndex(uint id)
        {
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].ID == id) return i;
            }
            return -1;
        }

        public void RemoveDead()
        {
            int write = 0;
            for (int read = 0; read < Units.Count; read++)
            {
                if (Units[read].State != UnitState.Dead)
                {
                    if (write != read) Units[write] = Units[read];
                    write++;
                }
            }
            if (write < Units.Count)
                Units.RemoveRange(write, Units.Count - write);
        }
    }
}

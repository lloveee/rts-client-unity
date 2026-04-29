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
        Stop = 3,
        AttackMove = 4,
        Build = 5,
        Train = 6,
        Surrender = 7
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

        // --- Phase 1 economy helpers ---

        public (bool found, int unitIdx, int bldIdx, int crystIdx) FindEntity(uint id)
        {
            for (int i = 0; i < Units.Count; i++)
                if (Units[i].ID == id && Units[i].State != UnitState.Dead)
                    return (true, i, -1, -1);
            for (int i = 0; i < Buildings.Count; i++)
                if (Buildings[i].ID == id && Buildings[i].State != BuildingState.Dead)
                    return (true, -1, i, -1);
            for (int i = 0; i < Crystals.Count; i++)
                if (Crystals[i].ID == id && Crystals[i].Remaining > Fixed32.Zero)
                    return (true, -1, -1, i);
            return (false, -1, -1, -1);
        }

        public int FindCrystalIndex(uint id)
        {
            for (int i = 0; i < Crystals.Count; i++)
                if (Crystals[i].ID == id && Crystals[i].Remaining > Fixed32.Zero)
                    return i;
            return -1;
        }

        public int FindBuildingIndex(uint id)
        {
            for (int i = 0; i < Buildings.Count; i++)
                if (Buildings[i].ID == id && Buildings[i].State != BuildingState.Dead)
                    return i;
            return -1;
        }

        public uint SpawnBuilding(byte owner, BuildingType type, Vec2 pos)
        {
            var stats = SimConstants.BuildingStats[type];
            uint id = NextID++;
            Buildings.Add(new Building
            {
                ID = id, Owner = owner, Type = type,
                SizeCells = stats.SizeCells, Pos = pos,
                HP = Fixed32.FromInt(stats.MaxHP),
                MaxHP = Fixed32.FromInt(stats.MaxHP),
                State = BuildingState.Ready,
            });
            return id;
        }

        public uint SpawnCrystal(Vec2 pos)
        {
            uint id = NextID++;
            Crystals.Add(new Crystal
            {
                ID = id, Pos = pos,
                Remaining = Fixed32.FromInt(SimConstants.CrystalStartValue),
            });
            return id;
        }

        public int FindNearestCrystalIndex(Vec2 pos)
        {
            int best = -1;
            Fixed32 bestDistSq = Fixed32.Zero;
            for (int i = 0; i < Crystals.Count; i++)
            {
                if (Crystals[i].Remaining <= Fixed32.Zero) continue;
                var dSq = pos.DistSq(Crystals[i].Pos);
                if (best < 0 || dSq < bestDistSq) { best = i; bestDistSq = dSq; }
            }
            return best;
        }

        public int FindNearestOwnHQIndex(Vec2 pos, byte owner)
        {
            int best = -1;
            Fixed32 bestDistSq = Fixed32.Zero;
            for (int i = 0; i < Buildings.Count; i++)
            {
                var b = Buildings[i];
                if (b.Owner != owner || b.Type != BuildingType.HQ || b.State != BuildingState.Ready)
                    continue;
                var dSq = pos.DistSq(b.Pos);
                if (best < 0 || dSq < bestDistSq) { best = i; bestDistSq = dSq; }
            }
            return best;
        }

        public int FindCrystalAt(Vec2 pos)
        {
            var rangeSq = Fixed32.FromInt(2) * Fixed32.FromInt(2);
            for (int i = 0; i < Crystals.Count; i++)
            {
                if (Crystals[i].Remaining <= Fixed32.Zero) continue;
                if (Crystals[i].Pos.DistSq(pos) <= rangeSq) return i;
            }
            return -1;
        }

        public void RemoveDeadBuildings()
        {
            int write = 0;
            for (int read = 0; read < Buildings.Count; read++)
            {
                if (Buildings[read].State != BuildingState.Dead)
                {
                    if (write != read) Buildings[write] = Buildings[read];
                    write++;
                }
            }
            if (write < Buildings.Count)
                Buildings.RemoveRange(write, Buildings.Count - write);
        }

        public void RemoveDeadCrystals()
        {
            int write = 0;
            for (int read = 0; read < Crystals.Count; read++)
            {
                if (Crystals[read].Remaining > Fixed32.Zero)
                {
                    if (write != read) Crystals[write] = Crystals[read];
                    write++;
                }
            }
            if (write < Crystals.Count)
                Crystals.RemoveRange(write, Crystals.Count - write);
        }
        public bool GameOver;
        public PlayerResult[] GameOverResults;

        public IEntity FindEntityAny(uint id)
        {
            for (int i = 0; i < Units.Count; i++)
                if (Units[i].ID == id && Units[i].State != UnitState.Dead)
                    return new UnitRef { u = Units[i], w = this, idx = i };
            for (int i = 0; i < Buildings.Count; i++)
                if (Buildings[i].ID == id && Buildings[i].State != BuildingState.Dead)
                    return new BuildingRef { b = Buildings[i], w = this, idx = i };
            for (int i = 0; i < Crystals.Count; i++)
                if (Crystals[i].ID == id && Crystals[i].Remaining > Fixed32.Zero)
                    return new CrystalRef { c = Crystals[i], w = this, idx = i };
            return null;
        }
    }

    public struct PlayerResult { public byte PlayerID; public byte Result; }

    public interface IEntity
    {
        bool IsDead();
        Vec2 GetPos();
        Fixed32 GetHP();
        void SetHP(Fixed32 v);
        void SetDead();
        uint GetID();
    }

    internal struct UnitRef : IEntity
    {
        public Unit u; public World w; public int idx;
        public bool IsDead() => u.State == UnitState.Dead;
        public Vec2 GetPos() => u.Pos;
        public Fixed32 GetHP() => u.HP;
        public void SetHP(Fixed32 v) { u.HP = v; w.Units[idx] = u; }
        public void SetDead() { u.State = UnitState.Dead; u.HP = Fixed32.Zero; w.Units[idx] = u; }
        public uint GetID() => u.ID;
    }

    internal struct BuildingRef : IEntity
    {
        public Building b; public World w; public int idx;
        public bool IsDead() => b.State == BuildingState.Dead;
        public Vec2 GetPos() => b.Pos;
        public Fixed32 GetHP() => b.HP;
        public void SetHP(Fixed32 v) { b.HP = v; w.Buildings[idx] = b; }
        public void SetDead() { b.State = BuildingState.Dead; w.Buildings[idx] = b; }
        public uint GetID() => b.ID;
    }

    internal struct CrystalRef : IEntity
    {
        public Crystal c; public World w; public int idx;
        public bool IsDead() => c.Remaining <= Fixed32.Zero;
        public Vec2 GetPos() => c.Pos;
        public Fixed32 GetHP() => c.Remaining;
        public void SetHP(Fixed32 v) { c.Remaining = v; w.Crystals[idx] = c; }
        public void SetDead() { c.Remaining = Fixed32.Zero; w.Crystals[idx] = c; }
        public uint GetID() => c.ID;
    }
}

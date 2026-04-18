using RTS.Core;

namespace RTS.Sim
{
    /// <summary>
    /// Marshal/Unmarshal world state. Ported from Go internal/sim/snapshot.go.
    /// Header: tick(4)+seed(8)+rand_state(8)+next_id(4)+mapW(4)+mapH(4)+unit_count(4) = 36 bytes
    /// Unit: id(4)+owner(1)+state(1)+hp(4)+maxhp(4)+speed(4)+posx(4)+posy(4)+targetid(4)+movex(4)+movey(4) = 38 bytes
    /// </summary>
    public static class Snapshot
    {
        private const int HeaderSize = 36;
        private const int UnitSize = 38;

        public static byte[] Marshal(World w)
        {
            var bw = new ByteWriter(HeaderSize + w.Units.Count * UnitSize);

            bw.WriteU32(w.Tick);
            bw.WriteU64(w.Seed);
            bw.WriteU64(w.Rand.State);
            bw.WriteU32(w.NextID);
            bw.WriteU32((uint)w.MapSizeX.Raw);
            bw.WriteU32((uint)w.MapSizeY.Raw);
            bw.WriteU32((uint)w.Units.Count);

            for (int i = 0; i < w.Units.Count; i++)
            {
                var u = w.Units[i];
                bw.WriteU32(u.ID);
                bw.WriteU8(u.Owner);
                bw.WriteU8((byte)u.State);
                bw.WriteU32((uint)u.HP.Raw);
                bw.WriteU32((uint)u.MaxHP.Raw);
                bw.WriteU32((uint)u.Speed.Raw);
                bw.WriteU32((uint)u.Pos.X.Raw);
                bw.WriteU32((uint)u.Pos.Y.Raw);
                bw.WriteU32(u.TargetID);
                bw.WriteU32((uint)u.MoveTo.X.Raw);
                bw.WriteU32((uint)u.MoveTo.Y.Raw);
            }

            return bw.ToArray();
        }

        public static World Unmarshal(byte[] data)
        {
            var br = new ByteReader(data);

            var w = new World
            {
                Tick = br.ReadU32(),
                Seed = br.ReadU64()
            };
            ulong randState = br.ReadU64();
            w.Rand = new SplitMix64(0) { State = randState };
            w.NextID = br.ReadU32();
            w.MapSizeX = Fixed32.FromRaw((int)br.ReadU32());
            w.MapSizeY = Fixed32.FromRaw((int)br.ReadU32());

            int unitCount = (int)br.ReadU32();
            w.Units = new System.Collections.Generic.List<Unit>(unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                var u = new Unit
                {
                    ID = br.ReadU32(),
                    Owner = br.ReadU8(),
                    State = (UnitState)br.ReadU8(),
                    HP = Fixed32.FromRaw((int)br.ReadU32()),
                    MaxHP = Fixed32.FromRaw((int)br.ReadU32()),
                    Speed = Fixed32.FromRaw((int)br.ReadU32()),
                    Pos = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32())),
                    TargetID = br.ReadU32(),
                    MoveTo = new Vec2(
                        Fixed32.FromRaw((int)br.ReadU32()),
                        Fixed32.FromRaw((int)br.ReadU32()))
                };
                w.Units.Add(u);
            }

            return w;
        }
    }
}

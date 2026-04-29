using RTS.Sim;
using System.Collections.Generic;

namespace RTS.Game
{
    public class FOWState
    {
        public int W, H;
        public byte[] Cells;
        public Dictionary<uint, LastSeen> Memory = new();

        public struct LastSeen { public BuildingType Type; public byte Owner; public int HPRaw; public byte Size; public int XRaw, YRaw; }

        public FOWState(int w, int h) { W = w; H = h; Cells = new byte[w * h]; }

        public bool IsVisible(int cx, int cy) => cx >= 0 && cy >= 0 && cx < W && cy < H && Cells[cy * W + cx] == 2;
        public bool IsExplored(int cx, int cy) => cx >= 0 && cy >= 0 && cx < W && cy < H && Cells[cy * W + cx] >= 1;

        public void Update(World world, byte myID)
        {
            for (int i = 0; i < Cells.Length; i++) if (Cells[i] == 2) Cells[i] = 1;

            for (int i = 0; i < world.Units.Count; i++)
            { var u = world.Units[i]; if (u.State != UnitState.Dead && u.Owner == myID) Paint(u.Pos, u.VisionRange.ToInt()); }
            for (int i = 0; i < world.Buildings.Count; i++)
            { var b = world.Buildings[i]; if (b.State != BuildingState.Dead && b.Owner == myID) Paint(b.Pos, SimConstants.BuildingStats[b.Type].VisionRange); }

            for (int i = 0; i < world.Buildings.Count; i++)
            {
                var b = world.Buildings[i];
                if (b.State == BuildingState.Dead || b.Owner == myID) continue;
                if (IsVisible(b.Pos.X.ToInt(), b.Pos.Y.ToInt()))
                    Memory[b.ID] = new LastSeen { Type = b.Type, Owner = b.Owner, HPRaw = b.HP.Raw, Size = b.SizeCells, XRaw = b.Pos.X.Raw, YRaw = b.Pos.Y.Raw };
            }
        }

        void Paint(Vec2 c, int r)
        {
            int cx = c.X.ToInt(), cy = c.Y.ToInt();
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                { if (dx*dx+dy*dy>r*r) continue; int x=cx+dx,y=cy+dy; if(x>=0&&y>=0&&x<W&&y<H) Cells[y*W+x]=2; }
        }
    }
}

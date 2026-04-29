using System.Collections.Generic;

namespace RTS.Sim
{
    public static class SimPathfinding
    {
        private const int DiagCost = 92682, StraightCost = 65536;
        private static readonly int[][] Dirs = { new[]{1,0},new[]{-1,0},new[]{0,1},new[]{0,-1},new[]{1,1},new[]{-1,-1},new[]{1,-1},new[]{-1,1} };

        private class Node { public int x, y, g, f; public Node parent; }

        private static int OctileDist(int x1, int y1, int x2, int y2)
        { int dx = x1<x2 ? x2-x1 : x1-x2; int dy = y1<y2 ? y2-y1 : y1-y2; if (dx < dy) { int t=dx; dx=dy; dy=t; } return dy*DiagCost+(dx-dy)*StraightCost; }

        public static List<Vec2> FindPath(World w, Vec2 start, Vec2 goal)
        {
            int sx=start.X.ToInt(), sy=start.Y.ToInt(), gx=goal.X.ToInt(), gy=goal.Y.ToInt();
            int W=w.NavGrid.W, H=w.NavGrid.H;
            if (sx==gx&&sy==gy||sx<0||sy<0||sx>=W||sy>=H||gx<0||gy<0||gx>=W||gy>=H) return null;
            if (IsBlocked(w,gx,gy,0)) return null;
            var open=new SortedSet<(int f,int y,int x,Node n)>();
            var closed=new HashSet<(int,int)>();
            var sn=new Node{x=sx,y=sy,f=OctileDist(sx,sy,gx,gy)};
            open.Add((sn.f,sy,sx,sn));
            while (open.Count>0){ var min=open.Min; open.Remove(min); var cur=min.n; var k=(cur.x,cur.y); if(closed.Contains(k))continue; closed.Add(k); if(cur.x==gx&&cur.y==gy)return Build(cur);
            foreach(var d in Dirs){ int nx=cur.x+d[0],ny=cur.y+d[1]; if(nx<0||ny<0||nx>=W||ny>=H||closed.Contains((nx,ny))||IsBlocked(w,nx,ny,0))continue;
            int mc=StraightCost; if(d[0]!=0&&d[1]!=0){if(IsBlocked(w,cur.x+d[0],cur.y,0)||IsBlocked(w,cur.x,cur.y+d[1],0))continue;mc=DiagCost;}
            int ng=cur.g+mc; var nn=new Node{x=nx,y=ny,g=ng,f=ng+OctileDist(nx,ny,gx,gy),parent=cur}; open.Add((nn.f,ny,nx,nn)); }}
            return null;
        }

        private static List<Vec2> Build(Node n){ var r=new List<Vec2>(); for(var c=n;c!=null;c=c.parent)r.Add(Vec2.FromInt(c.x,c.y)); r.Reverse(); return r.Count<=1?null:r.GetRange(1,r.Count-1); }

        public static void StepMovePath(World w, ref Unit u)
        {
            if (u.Path==null||u.Path.Length==0){u.State=UnitState.Idle;return;}
            var wp=u.Path[0];
            if (u.Pos.DistSq(wp)<=Fixed32.FromInt(4)){ var n=new Vec2[u.Path.Length-1]; for(int i=1;i<u.Path.Length;i++)n[i-1]=u.Path[i]; u.Path=n; if(u.Path.Length==0){u.State=UnitState.Idle;return;} wp=u.Path[0]; }
            var np=Vec2.MoveToward(u.Pos,wp,u.Speed); u.Pos=new Vec2(np.X.Clamp(Fixed32.Zero,w.MapSizeX),np.Y.Clamp(Fixed32.Zero,w.MapSizeY));
        }

        public static bool IsBlocked(World w,int cx,int cy,uint ex){for(int i=0;i<w.Buildings.Count;i++){var b=w.Buildings[i];if(b.State==BuildingState.Dead||b.ID==ex)continue;int bx=b.Pos.X.ToInt(),by=b.Pos.Y.ToInt(),bs=b.SizeCells;if(cx>=bx&&cx<bx+bs&&cy>=by&&cy<by+bs)return true;}return false;}
    }
}

namespace RTS.Sim
{
    public class NavGrid
    {
        public int W;
        public int H;
        public ulong[] Blocked;

        public NavGrid(int w, int h)
        {
            W = w;
            H = h;
            int total = w * h;
            int words = (total + 63) / 64;
            Blocked = new ulong[words];
        }

        public NavGrid() { }
    }
}

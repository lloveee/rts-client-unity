using RTS.Sim;
using UnityEngine;

namespace RTS.Game
{
    public class FOWRenderer : MonoBehaviour
    {
        public FOWState State { get; private set; }
        [SerializeField] private int _updateInterval = 5;
        private int _tickCounter;

        public void Init(int mapW, int mapH) { State = new FOWState(mapW, mapH); }

        public void Tick(World world, byte myID)
        {
            if (++_tickCounter % _updateInterval != 0 || world == null) return;
            State.Update(world, myID);
        }

        public bool IsUnitVisible(Unit u, byte myID) => u.Owner == myID || State.IsVisible(u.Pos.X.ToInt(), u.Pos.Y.ToInt());
        public bool IsBuildingVisible(Building b, byte myID) => b.Owner == myID || State.IsVisible(b.Pos.X.ToInt(), b.Pos.Y.ToInt());
    }
}

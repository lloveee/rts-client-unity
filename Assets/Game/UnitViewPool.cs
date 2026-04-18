using System.Collections.Generic;
using UnityEngine;

namespace RTS.Game
{
    public class UnitViewPool : MonoBehaviour
    {
        private readonly Dictionary<uint, UnitView> _active = new();
        private readonly Stack<UnitView> _pool = new();
        private Transform _unitParent;

        private void Awake()
        {
            _unitParent = new GameObject("Units").transform;
        }

        public UnitView Get(uint unitID, byte owner)
        {
            if (_active.TryGetValue(unitID, out var existing))
                return existing;

            UnitView view;
            if (_pool.Count > 0)
            {
                view = _pool.Pop();
                view.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject($"Unit_{unitID}");
                go.transform.SetParent(_unitParent);
                view = go.AddComponent<UnitView>();
            }

            view.Init(unitID, owner);
            view.gameObject.name = $"Unit_{unitID}_P{owner}";
            _active[unitID] = view;
            return view;
        }

        public void Return(uint unitID)
        {
            if (!_active.TryGetValue(unitID, out var view)) return;
            _active.Remove(unitID);
            view.Recycle();
            _pool.Push(view);
        }

        public UnitView Find(uint unitID)
        {
            _active.TryGetValue(unitID, out var v);
            return v;
        }

        public IEnumerable<UnitView> ActiveViews => _active.Values;

        public void SyncWithWorld(RTS.Sim.World world)
        {
            var alive = new HashSet<uint>();
            foreach (var unit in world.Units)
            {
                alive.Add(unit.ID);
                var view = Get(unit.ID, unit.Owner);

                Vector3 prevPos = view.transform.position;
                view.UpdateFromSim(in unit, prevPos);
            }

            var toRemove = new List<uint>();
            foreach (var kvp in _active)
            {
                if (!alive.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (var id in toRemove)
                Return(id);
        }
    }
}

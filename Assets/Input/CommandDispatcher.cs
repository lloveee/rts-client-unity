using System.Collections.Generic;
using RTS.Game;
using RTS.Sim;
using UnityEngine;

namespace RTS.Input
{
    public class CommandDispatcher : MonoBehaviour
    {
        private enum CommandMode { Normal, AttackMove, Patrol }
        private CommandMode _mode = CommandMode.Normal;

        private readonly Dictionary<uint, Queue<(int tx, int ty)>> _waypoints =
            new Dictionary<uint, Queue<(int tx, int ty)>>();

        private bool _runnerHooked;

        private void Update()
        {
            HookRunnerIfNeeded();
            HandleCommandModeKeys();
            HandleRightClick();
            HandleStopKey();
        }

        private void HookRunnerIfNeeded()
        {
            if (_runnerHooked) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Runner == null || gm.State != GameState.Playing) return;
            gm.Runner.OnTickAdvanced += OnTickAdvanced;
            _runnerHooked = true;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Runner != null && _runnerHooked)
                gm.Runner.OnTickAdvanced -= OnTickAdvanced;
        }

        private void HandleCommandModeKeys()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                _mode = CommandMode.AttackMove;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                _mode = CommandMode.Patrol;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                var sel = SelectionManager.Instance;
                if (sel != null)
                    foreach (var view in sel.Selected)
                        _waypoints.Remove(view.UnitID);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                _mode = CommandMode.Normal;
            }
        }

        private void HandleRightClick()
        {
            if (!UnityEngine.Input.GetMouseButtonDown(1)) return;

            var gm = GameManager.Instance;
            var sel = SelectionManager.Instance;
            if (gm == null || sel == null || sel.Selected.Count == 0) return;
            if (gm.Client == null || gm.State != GameState.Playing) return;

            var ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float dist)) return;

            Vector3 worldPos = ray.GetPoint(dist);
            int targetX = (int)(worldPos.x * 65536f);
            int targetY = (int)(worldPos.z * 65536f);

            bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) ||
                         UnityEngine.Input.GetKey(KeyCode.RightShift);

            uint cmdTick = gm.Runner.CurrentTick + gm.CurrentN;

            foreach (var view in sel.Selected)
            {
                uint uid = view.UnitID;

                if (shift)
                {
                    if (!_waypoints.TryGetValue(uid, out var queue))
                    {
                        queue = new Queue<(int, int)>();
                        _waypoints[uid] = queue;
                    }
                    queue.Enqueue((targetX, targetY));

                    if (queue.Count == 1 && IsUnitIdle(gm, uid))
                    {
                        var wp = queue.Peek();
                        gm.Client.SendCmd(cmdTick, (byte)CmdOp.Move, uid, wp.tx, wp.ty);
                    }
                }
                else
                {
                    _waypoints.Remove(uid);
                    gm.Client.SendCmd(cmdTick, (byte)CmdOp.Move, uid, targetX, targetY);
                }
            }

            if (_mode == CommandMode.AttackMove)
                _mode = CommandMode.Normal;
        }

        private void HandleStopKey()
        {
            if (!UnityEngine.Input.GetKeyDown(KeyCode.S)) return;

            var gm = GameManager.Instance;
            var sel = SelectionManager.Instance;
            if (gm == null || sel == null || sel.Selected.Count == 0) return;
            if (gm.State != GameState.Playing) return;

            uint cmdTick = gm.Runner.CurrentTick + gm.CurrentN;

            foreach (var view in sel.Selected)
            {
                _waypoints.Remove(view.UnitID);
                gm.Client.SendCmd(cmdTick, (byte)CmdOp.Stop, view.UnitID, 0, 0);
            }
        }

        private void OnTickAdvanced()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Client == null || gm.Runner?.World == null) return;
            if (_waypoints.Count == 0) return;

            uint cmdTick = gm.Runner.CurrentTick + gm.CurrentN;

            var keys = new List<uint>(_waypoints.Keys);
            foreach (var uid in keys)
            {
                var queue = _waypoints[uid];

                int idx = gm.Runner.World.FindUnitIndex(uid);
                if (idx < 0)
                {
                    _waypoints.Remove(uid);
                    continue;
                }

                var unit = gm.Runner.World.Units[idx];
                if (unit.State == UnitState.Dead)
                {
                    _waypoints.Remove(uid);
                    continue;
                }

                if (unit.State == UnitState.Idle)
                {
                    if (queue.Count > 0) queue.Dequeue();

                    if (queue.Count == 0)
                    {
                        _waypoints.Remove(uid);
                        continue;
                    }

                    var wp = queue.Peek();
                    gm.Client.SendCmd(cmdTick, (byte)CmdOp.Move, uid, wp.tx, wp.ty);
                }
            }
        }

        private static bool IsUnitIdle(GameManager gm, uint unitID)
        {
            int idx = gm.Runner.World.FindUnitIndex(unitID);
            if (idx < 0) return false;
            return gm.Runner.World.Units[idx].State == UnitState.Idle;
        }
    }
}

using System.Collections.Generic;
using RTS.Game;
using UnityEngine;

namespace RTS.Input
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        public List<UnitView> Selected { get; } = new();
        public Dictionary<int, List<uint>> ControlGroups { get; } = new();

        [SerializeField] private float _dragThreshold = 5f;

        private Vector2 _mouseDownPos;
        private bool _isDragging;
        private bool _mouseIsDown;

        private GUIStyle _boxStyle;
        private Rect _selectionRect;

        private float _lastClickTime;
        private const float DoubleClickTime = 0.3f;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            HandleMouseSelection();
            HandleControlGroups();
        }

        private void HandleMouseSelection()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _mouseDownPos = UnityEngine.Input.mousePosition;
                _mouseIsDown = true;
                _isDragging = false;
            }

            if (_mouseIsDown && !_isDragging)
            {
                float dist = Vector2.Distance(_mouseDownPos, UnityEngine.Input.mousePosition);
                if (dist > _dragThreshold)
                    _isDragging = true;
            }

            if (UnityEngine.Input.GetMouseButtonUp(0) && _mouseIsDown)
            {
                _mouseIsDown = false;
                bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) ||
                             UnityEngine.Input.GetKey(KeyCode.RightShift);

                if (_isDragging)
                {
                    BoxSelect(shift);
                }
                else
                {
                    float now = Time.unscaledTime;
                    if (now - _lastClickTime < DoubleClickTime)
                    {
                        DoubleClickSelect();
                    }
                    else
                    {
                        ClickSelect(shift);
                    }
                    _lastClickTime = now;
                }
                _isDragging = false;
            }
        }

        private void ClickSelect(bool additive)
        {
            var ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var view = hit.collider.GetComponent<UnitView>();
                if (view != null && view.Owner == GameManager.Instance.LocalPlayerID)
                {
                    if (!additive) ClearSelection();
                    ToggleSelection(view);
                    return;
                }
            }
            if (!additive) ClearSelection();
        }

        private void BoxSelect(bool additive)
        {
            if (!additive) ClearSelection();

            Vector2 min = Vector2.Min(_mouseDownPos, (Vector2)UnityEngine.Input.mousePosition);
            Vector2 max = Vector2.Max(_mouseDownPos, (Vector2)UnityEngine.Input.mousePosition);
            _selectionRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);

            var pool = FindFirstObjectByType<UnitViewPool>();
            if (pool == null) return;

            foreach (var view in pool.ActiveViews)
            {
                if (view.Owner != GameManager.Instance.LocalPlayerID) continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(view.transform.position);
                if (screenPos.z > 0 && _selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
                {
                    SelectUnit(view);
                }
            }
        }

        private void DoubleClickSelect()
        {
            var pool = FindFirstObjectByType<UnitViewPool>();
            if (pool == null) return;

            ClearSelection();
            foreach (var view in pool.ActiveViews)
            {
                if (view.Owner != GameManager.Instance.LocalPlayerID) continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(view.transform.position);
                if (screenPos.z > 0 &&
                    screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    SelectUnit(view);
                }
            }
        }

        private void HandleControlGroups()
        {
            bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                        UnityEngine.Input.GetKey(KeyCode.RightControl);

            for (int i = 0; i <= 9; i++)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    if (ctrl)
                    {
                        var ids = new List<uint>();
                        foreach (var v in Selected)
                            ids.Add(v.UnitID);
                        ControlGroups[i] = ids;
                    }
                    else
                    {
                        if (ControlGroups.TryGetValue(i, out var ids))
                        {
                            ClearSelection();
                            var pool = FindFirstObjectByType<UnitViewPool>();
                            foreach (var id in ids)
                            {
                                var view = pool?.Find(id);
                                if (view != null) SelectUnit(view);
                            }
                        }
                    }
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) && ControlGroups.Count > 0)
            {
                foreach (var kvp in ControlGroups)
                {
                    if (kvp.Value.Count > 0)
                    {
                        ClearSelection();
                        var pool = FindFirstObjectByType<UnitViewPool>();
                        foreach (var id in kvp.Value)
                        {
                            var view = pool?.Find(id);
                            if (view != null) SelectUnit(view);
                        }
                        break;
                    }
                }
            }
        }

        public void SelectUnit(UnitView view)
        {
            if (!Selected.Contains(view))
            {
                Selected.Add(view);
                view.SetSelected(true);
            }
        }

        private void ToggleSelection(UnitView view)
        {
            if (Selected.Contains(view))
            {
                Selected.Remove(view);
                view.SetSelected(false);
            }
            else
            {
                SelectUnit(view);
            }
        }

        public void ClearSelection()
        {
            foreach (var v in Selected)
                v.SetSelected(false);
            Selected.Clear();
        }

        private void OnGUI()
        {
            if (!_isDragging || !_mouseIsDown) return;

            _boxStyle ??= new GUIStyle
            {
                normal = { background = MakeTexture(new Color(0, 1, 0, 0.2f)) }
            };

            Vector2 start = _mouseDownPos;
            Vector2 end = UnityEngine.Input.mousePosition;

            float x = Mathf.Min(start.x, end.x);
            float y = Screen.height - Mathf.Max(start.y, end.y);
            float w = Mathf.Abs(end.x - start.x);
            float h = Mathf.Abs(end.y - start.y);

            GUI.Box(new Rect(x, y, w, h), "", _boxStyle);
        }

        private static Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}

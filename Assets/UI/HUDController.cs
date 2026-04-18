using RTS.Game;
using RTS.Input;
using UnityEngine;
using UnityEngine.UIElements;

namespace RTS.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _topBar;
        private Label _tickLabel, _hashLabel, _nLabel, _rttLabel, _fpsLabel;
        private Label _roomLabel, _playerLabel, _selectionLabel;
        private bool _hudVisible = true;
        private float _fpsTimer;
        private int _fpsCount;
        private float _currentFps;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _topBar = root.Q("top-bar");
            _tickLabel = root.Q<Label>("tick-label");
            _hashLabel = root.Q<Label>("hash-label");
            _nLabel = root.Q<Label>("n-label");
            _rttLabel = root.Q<Label>("rtt-label");
            _fpsLabel = root.Q<Label>("fps-label");
            _roomLabel = root.Q<Label>("room-label");
            _playerLabel = root.Q<Label>("player-label");
            _selectionLabel = root.Q<Label>("selection-label");
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
            {
                _hudVisible = !_hudVisible;
                _topBar.style.display = _hudVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _fpsCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _currentFps = _fpsCount / _fpsTimer;
                _fpsCount = 0;
                _fpsTimer = 0;
            }

            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            var runner = gm.Runner;
            if (runner?.World == null) return;

            _tickLabel.text = $"Tick: {runner.CurrentTick}";
            ulong hash = RTS.Sim.SimHash.Hash(runner.World);
            string hashStr = $"Hash: {hash:x16}";
            _hashLabel.text = hashStr.Length >= 13 ? hashStr.Substring(0, 13) : hashStr;
            _nLabel.text = $"N: {gm.CurrentN}";
            _rttLabel.text = $"RTT: {(gm.Client != null ? "?" : "--")}ms";
            _fpsLabel.text = $"FPS: {_currentFps:F0}";
            _playerLabel.text = $"Player: {gm.LocalPlayerID}";

            var sel = SelectionManager.Instance;
            if (sel != null && sel.Selected.Count > 0)
            {
                _selectionLabel.text = $"Selected: {sel.Selected.Count} units";
            }
            else
            {
                _selectionLabel.text = "";
            }
        }
    }
}

using RTS.Game;
using RTS.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace RTS.UI
{
    public class ResourceBar : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        private Label _crystalLabel;

        private void OnEnable()
        {
            if (_document == null) return;
            _crystalLabel = _document.rootVisualElement.Q<Label>("crystal-label");
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            var w = gm.Runner?.World;
            if (w == null) return;
            int pid = gm.LocalPlayerID;
            if (pid >= w.Players.Count) return;

            if (_crystalLabel != null)
                _crystalLabel.text = $"Crystal: {w.Players[pid].Crystal.ToInt()}";
        }
    }
}

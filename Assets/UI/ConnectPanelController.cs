using RTS.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace RTS.UI
{
    public class ConnectPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _overlay;
        private TextField _serverField;
        private TextField _roomField;
        private TextField _nameField;
        private Button _connectBtn;
        private Label _statusText;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _overlay = root.Q("connect-overlay");
            _serverField = root.Q<TextField>("server-address");
            _roomField = root.Q<TextField>("room-id");
            _nameField = root.Q<TextField>("player-name");
            _connectBtn = root.Q<Button>("connect-btn");
            _statusText = root.Q<Label>("status-text");

            _connectBtn.clicked += OnConnectClicked;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnGameStarted += Hide;
                gm.OnError += err => _statusText.text = err;
            }
        }

        private void OnConnectClicked()
        {
            string address = _serverField.value;
            string room = _roomField.value;
            string name = _nameField.value;

            string[] parts = address.Split(':');
            string host = parts[0];
            int port = parts.Length > 1 ? int.Parse(parts[1]) : 9000;

            _statusText.text = "Connecting...";
            _connectBtn.SetEnabled(false);

            GameManager.Instance.Connect(host, port, name, room);
        }

        private void Hide()
        {
            _overlay.style.display = DisplayStyle.None;
        }

        public void Show()
        {
            _overlay.style.display = DisplayStyle.Flex;
            _connectBtn.SetEnabled(true);
            _statusText.text = "";
        }
    }
}

using RTS.Game;
using UnityEngine;

namespace RTS.Input
{
    [RequireComponent(typeof(GameManager))]
    public class SceneWiring : MonoBehaviour
    {
        [SerializeField] private MapView _mapView;
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private UnitViewPool _unitViewPool;

        private GameManager _gm;

        private void Awake()
        {
            _gm = GetComponent<GameManager>();
            _gm.OnGameStarted += HandleGameStarted;
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnGameStarted -= HandleGameStarted;
            if (_gm?.Runner != null) _gm.Runner.OnTickAdvanced -= HandleTickAdvanced;
        }

        private void HandleGameStarted()
        {
            int mapW = _gm.Client.MapW;
            int mapH = _gm.Client.MapH;

            if (_mapView != null) _mapView.Init(mapW, mapH);
            if (_cameraController != null) _cameraController.SetBounds(mapW, mapH);

            if (_unitViewPool != null && _gm.Runner?.World != null)
            {
                _unitViewPool.SyncWithWorld(_gm.Runner.World);
                _gm.Runner.OnTickAdvanced += HandleTickAdvanced;
            }
        }

        private void HandleTickAdvanced()
        {
            if (_unitViewPool != null && _gm.Runner?.World != null)
                _unitViewPool.SyncWithWorld(_gm.Runner.World);
        }
    }
}

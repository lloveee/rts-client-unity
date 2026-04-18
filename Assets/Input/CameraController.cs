using UnityEngine;

namespace RTS.Input
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _edgeScrollMargin = 10f;
        [SerializeField] private bool _edgeScrollEnabled = true;

        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _minHeight = 10f;
        [SerializeField] private float _maxHeight = 60f;

        [Header("Bounds")]
        [SerializeField] private float _mapMinX = 0f;
        [SerializeField] private float _mapMaxX = 100f;
        [SerializeField] private float _mapMinZ = 0f;
        [SerializeField] private float _mapMaxZ = 100f;

        private Vector3 _dragOrigin;
        private bool _isDragging;

        public void SetBounds(float mapW, float mapH)
        {
            _mapMinX = 0;
            _mapMaxX = mapW;
            _mapMinZ = 0;
            _mapMaxZ = mapH;
        }

        private void Start()
        {
            transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            transform.position = new Vector3(50f, 30f, 30f);
        }

        private void Update()
        {
            HandleKeyboardPan();
            HandleEdgeScroll();
            HandleMiddleMouseDrag();
            HandleZoom();
            ClampPosition();
        }

        private void HandleKeyboardPan()
        {
            Vector3 move = Vector3.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W)) move.z += 1;
            if (UnityEngine.Input.GetKey(KeyCode.S)) move.z -= 1;
            if (UnityEngine.Input.GetKey(KeyCode.A)) move.x -= 1;
            if (UnityEngine.Input.GetKey(KeyCode.D)) move.x += 1;

            if (move.sqrMagnitude > 0)
            {
                float speed = _moveSpeed * (transform.position.y / 30f);
                transform.position += move.normalized * speed * Time.deltaTime;
            }
        }

        private void HandleEdgeScroll()
        {
            if (!_edgeScrollEnabled) return;

            Vector3 move = Vector3.zero;
            Vector3 mousePos = UnityEngine.Input.mousePosition;
            if (mousePos.x < _edgeScrollMargin) move.x -= 1;
            if (mousePos.x > Screen.width - _edgeScrollMargin) move.x += 1;
            if (mousePos.y < _edgeScrollMargin) move.z -= 1;
            if (mousePos.y > Screen.height - _edgeScrollMargin) move.z += 1;

            if (move.sqrMagnitude > 0)
            {
                float speed = _moveSpeed * (transform.position.y / 30f);
                transform.position += move.normalized * speed * Time.deltaTime;
            }
        }

        private void HandleMiddleMouseDrag()
        {
            if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                _isDragging = true;
                _dragOrigin = GetGroundPoint(UnityEngine.Input.mousePosition);
            }
            if (UnityEngine.Input.GetMouseButton(2) && _isDragging)
            {
                Vector3 current = GetGroundPoint(UnityEngine.Input.mousePosition);
                Vector3 delta = _dragOrigin - current;
                transform.position += delta;
            }
            if (UnityEngine.Input.GetMouseButtonUp(2))
            {
                _isDragging = false;
            }
        }

        private void HandleZoom()
        {
            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.y -= scroll * _zoomSpeed;
                pos.y = Mathf.Clamp(pos.y, _minHeight, _maxHeight);
                transform.position = pos;
            }
        }

        private void ClampPosition()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, _mapMinX, _mapMaxX);
            pos.z = Mathf.Clamp(pos.z, _mapMinZ - 20f, _mapMaxZ);
            transform.position = pos;
        }

        private Vector3 GetGroundPoint(Vector3 screenPos)
        {
            var ray = Camera.main.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        public void FocusOn(Vector3 worldPos)
        {
            Vector3 pos = transform.position;
            pos.x = worldPos.x;
            pos.z = worldPos.z - transform.position.y;
            transform.position = pos;
        }
    }
}

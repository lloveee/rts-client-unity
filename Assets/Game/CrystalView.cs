using RTS.Sim;
using UnityEngine;

namespace RTS.Game
{
    public class CrystalView : MonoBehaviour
    {
        public uint CrystalID { get; private set; }

        [SerializeField] private GameObject _visual;
        [SerializeField] private float _maxScale = 1.0f;
        [SerializeField] private float _minScale = 0.3f;

        private float _initialRemaining;

        public static CrystalView Create(uint id, Vec2 pos, Fixed32 remaining, GameObject prefab)
        {
            var worldPos = new Vector3(pos.X.ToFloat(), 0.05f, pos.Y.ToFloat());
            var go = Object.Instantiate(prefab, worldPos, Quaternion.identity);
            var cv = go.GetComponent<CrystalView>();
            if (cv == null) cv = go.AddComponent<CrystalView>();
            cv.CrystalID = id;
            cv._initialRemaining = remaining.ToFloat();
            cv.UpdateVisual(remaining.ToFloat());
            return cv;
        }

        public void UpdateRemaining(Fixed32 remaining)
        {
            UpdateVisual(remaining.ToFloat());
        }

        private void UpdateVisual(float remaining)
        {
            if (_visual == null) return;
            float frac = _initialRemaining > 0 ? remaining / _initialRemaining : 0;
            float scale = Mathf.Lerp(_minScale, _maxScale, frac);
            _visual.transform.localScale = Vector3.one * scale;
        }

        public void Remove()
        {
            Destroy(gameObject);
        }
    }
}

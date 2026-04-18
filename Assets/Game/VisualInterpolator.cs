using UnityEngine;

namespace RTS.Game
{
    public class VisualInterpolator : MonoBehaviour
    {
        private Vector3 _prevPos;
        private Vector3 _nextPos;

        public void SetTarget(Vector3 prev, Vector3 next)
        {
            _prevPos = prev;
            _nextPos = next;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Runner == null) return;

            var runner = GameManager.Instance.Runner;
            float t = Mathf.Clamp01(runner.TimeSinceLastTick / runner.TickInterval);
            transform.position = Vector3.Lerp(_prevPos, _nextPos, t);

            Vector3 dir = _nextPos - _prevPos;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 10f);
            }
        }
    }
}

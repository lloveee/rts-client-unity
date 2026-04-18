using RTS.Sim;
using UnityEngine;

namespace RTS.Game
{
    public class UnitView : MonoBehaviour
    {
        public uint UnitID { get; private set; }
        public byte Owner { get; private set; }

        private VisualInterpolator _interpolator;
        private GameObject _selectionRing;
        private MeshRenderer _bodyRenderer;
        private bool _selected;

        private static readonly Color PlayerBlue = new(0.08f, 0.4f, 0.75f);
        private static readonly Color PlayerRed = new(0.78f, 0.16f, 0.16f);

        public void Init(uint unitID, byte owner)
        {
            UnitID = unitID;
            Owner = owner;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            _bodyRenderer = body.GetComponent<MeshRenderer>();
            _bodyRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _bodyRenderer.material.color = owner == 0 ? PlayerBlue : PlayerRed;

            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.transform.SetParent(transform, false);
            _selectionRing.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
            _selectionRing.transform.localPosition = Vector3.zero;
            var ringRenderer = _selectionRing.GetComponent<MeshRenderer>();
            ringRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ringRenderer.material.color = new Color(0.3f, 1f, 0.3f, 0.5f);
            _selectionRing.SetActive(false);

            Destroy(body.GetComponent<Collider>());
            Destroy(_selectionRing.GetComponent<Collider>());

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.size = new Vector3(0.8f, 1.6f, 0.8f);

            _interpolator = gameObject.AddComponent<VisualInterpolator>();
        }

        public void UpdateFromSim(in Unit unit, Vector3 prevWorldPos)
        {
            Vector3 nextWorldPos = SimToWorld(unit.Pos);
            _interpolator.SetTarget(prevWorldPos, nextWorldPos);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            _selectionRing.SetActive(selected);
        }

        public bool IsSelected => _selected;

        public static Vector3 SimToWorld(Vec2 simPos)
        {
            return new Vector3(simPos.X.ToFloat(), 0f, simPos.Y.ToFloat());
        }

        public void Recycle()
        {
            UnitID = 0;
            SetSelected(false);
            gameObject.SetActive(false);
        }
    }
}

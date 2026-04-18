using UnityEngine;

namespace RTS.Game
{
    public class MapView : MonoBehaviour
    {
        public float MapWidth { get; private set; }
        public float MapHeight { get; private set; }

        public void Init(int mapW, int mapH)
        {
            MapWidth = mapW;
            MapHeight = mapH;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(mapW / 10f, 1f, mapH / 10f);
            ground.transform.localPosition = new Vector3(mapW / 2f, 0f, mapH / 2f);

            var renderer = ground.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.25f, 0.35f, 0.2f);
            renderer.material = mat;

            ground.layer = LayerMask.NameToLayer("Default");
        }
    }
}

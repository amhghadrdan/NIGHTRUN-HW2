using UnityEngine;
using UnityEngine.Rendering;
namespace NightRun
{
    public sealed class ExhaustJet : MonoBehaviour
    {
        static Mesh flameMesh;
        static Material flameMaterial;
        MeshRenderer flame;
        Light glow;
        public bool Visible => flame && flame.enabled;
        public void Initialize()
        {
            if (!flameMaterial) flameMaterial = new Material(Shader.Find("NightRun/ExhaustFlame")) { name = "Blue core and orange flame" };
            if (!flameMesh)
            {
                var vertices = new Vector3[12]; var uv = new Vector2[12]; var indices = new int[18];
                for (int plane = 0; plane < 3; plane++)
                {
                    float a = plane * Mathf.PI / 3; Vector3 side = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                    int n = plane * 4;
                    vertices[n] = side * -.09f; vertices[n + 1] = side * .09f;
                    vertices[n + 2] = Vector3.forward * .8f + side * .015f; vertices[n + 3] = Vector3.forward * .8f - side * .015f;
                    uv[n] = Vector2.zero; uv[n + 1] = Vector2.right; uv[n + 2] = Vector2.one; uv[n + 3] = Vector2.up;
                    indices[plane * 6] = n; indices[plane * 6 + 1] = n + 1; indices[plane * 6 + 2] = n + 2;
                    indices[plane * 6 + 3] = n; indices[plane * 6 + 4] = n + 2; indices[plane * 6 + 5] = n + 3;
                }
                flameMesh = new Mesh { name = "Crossed tapered exhaust jet", vertices = vertices, uv = uv, triangles = indices }; flameMesh.RecalculateBounds();
            }
            gameObject.AddComponent<MeshFilter>().sharedMesh = flameMesh;
            flame = gameObject.AddComponent<MeshRenderer>(); flame.sharedMaterial = flameMaterial;
            flame.shadowCastingMode = ShadowCastingMode.Off; flame.receiveShadows = false;
            glow = Visuals.Light("Exhaust flame bounce", transform, new Vector3(0, 0, .2f), new Color(.6f, .72f, 1), .7f, 1.6f);
            SetBoost(false);
        }
        public void SetBoost(bool boost) { flame.enabled = boost; glow.enabled = boost; }
    }
}

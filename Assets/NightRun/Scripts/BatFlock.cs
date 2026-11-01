using UnityEngine;
using UnityEngine.Rendering;
namespace NightRun
{
    /// <summary>Fully 3D silhouettes with articulated wings; allocated only at startup.</summary>
    public sealed class BatFlock : MonoBehaviour
    {
        readonly Transform[] bodies = new Transform[7], leftWings = new Transform[7], rightWings = new Transform[7];
        readonly Vector3[] positions = new Vector3[7];
        float time;
        public int Count => bodies.Length;
        public void Initialize()
        {
            var material = new Material(Shader.Find("Unlit/Color")) { name = "Bat silhouettes", color = new Color(.008f, .012f, .018f) };
            var wing = WingMesh();
            for (int i = 0; i < bodies.Length; i++)
            {
                positions[i] = new Vector3(-23 + i * 10, 22 + (i % 3) * 3, 65 + (i % 4) * 13);
                bodies[i] = new GameObject("Bat " + (i + 1)).transform; bodies[i].SetParent(transform, false);
                bodies[i].position = positions[i]; bodies[i].localScale = Vector3.one * (i % 3 == 0 ? 1.3f : .95f);
                Visuals.Shape("Body", PrimitiveType.Sphere, bodies[i], Vector3.zero, new Vector3(.13f, .28f, .16f), material);
                for (int side = -1; side <= 1; side += 2)
                {
                    var ear = Visuals.Shape("Ear", PrimitiveType.Cube, bodies[i], new Vector3(side * .045f, .16f, 0), new Vector3(.04f, .1f, .035f), material);
                    ear.transform.localRotation = Quaternion.Euler(0, 0, -side * 20);
                    var w = new GameObject(side < 0 ? "Left wing" : "Right wing").transform; w.SetParent(bodies[i], false);
                    w.localScale = new Vector3(side, 1, 1); w.gameObject.AddComponent<MeshFilter>().sharedMesh = wing;
                    var renderer = w.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
                    if (side < 0) leftWings[i] = w; else rightWings[i] = w;
                }
            }
        }
        static Mesh WingMesh()
        {
            Vector3[] vertices = { new Vector3(.04f, .03f, 0), new Vector3(.28f, .25f, .02f), new Vector3(.7f, .32f, .04f), new Vector3(1.05f, .14f, .01f), new Vector3(.79f, -.02f, -.02f), new Vector3(.64f, .035f, -.01f), new Vector3(.49f, -.16f, 0), new Vector3(.34f, -.045f, .01f), new Vector3(.18f, -.2f, 0), new Vector3(.09f, -.1f, 0) };
            var triangles = new int[(vertices.Length - 2) * 6];
            for (int i = 0; i < vertices.Length - 2; i++)
            {
                triangles[i * 6] = 0; triangles[i * 6 + 1] = i + 1; triangles[i * 6 + 2] = i + 2;
                triangles[i * 6 + 3] = 0; triangles[i * 6 + 4] = i + 2; triangles[i * 6 + 5] = i + 1;
            }
            var mesh = new Mesh { name = "Scalloped bat wing", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); return mesh;
        }
        public void Tick(float dt)
        {
            time += dt;
            for (int i = 0; i < bodies.Length; i++)
            {
                float phase = time * (7.5f + i * .15f) + i * 1.7f;
                float flap = Mathf.Sin(phase) * 29;
                bodies[i].localPosition = positions[i] + new Vector3(Mathf.Sin(time * .2f + i) * 6, Mathf.Sin(time * .8f + i) * 1.4f, Mathf.Cos(time * .15f + i) * 4);
                bodies[i].localRotation = Quaternion.Euler(0, Mathf.Sin(time * .3f + i) * 13, Mathf.Sin(time * .45f + i) * 9);
                leftWings[i].localRotation = Quaternion.Euler(0, -8, -flap);
                rightWings[i].localRotation = Quaternion.Euler(0, 8, flap);
            }
        }
    }
}

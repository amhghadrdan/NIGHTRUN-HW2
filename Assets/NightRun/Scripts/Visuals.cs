using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightRun
{
    public static class Visuals
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material Mat(string name, Color color, float metal = 0, float smooth = .5f, float emission = 0)
        {
            if (materials.TryGetValue(name, out var existing) && existing) return existing;
            var mat = new Material(Shader.Find("Standard")) { name = name, color = color, enableInstancing = true };
            mat.SetFloat("_Metallic", metal); mat.SetFloat("_Glossiness", smooth);
            if (emission > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission); }
            materials[name] = mat; return mat;
        }
        public static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>(); if (collider) Object.Destroy(collider);
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go;
        }
        public static GameObject Box(string name, Transform p, Vector3 pos, Vector3 scale, Material mat) => Shape(name, PrimitiveType.Cube, p, pos, scale, mat);
        public static Light Light(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; return l;
        }
        public static void Combine(Transform parent)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var filters = parent.GetComponentsInChildren<MeshFilter>();
            foreach (var f in filters)
            {
                var r = f.GetComponent<MeshRenderer>(); if (!r || !r.sharedMaterial || f.sharedMesh.subMeshCount != 1) continue;
                if (!groups.ContainsKey(r.sharedMaterial)) groups[r.sharedMaterial] = new List<CombineInstance>();
                groups[r.sharedMaterial].Add(new CombineInstance { mesh = f.sharedMesh, transform = parent.worldToLocalMatrix * f.transform.localToWorldMatrix });
                r.enabled = false;
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "Batched " + group.Key.name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray());
                var go = new GameObject(mesh.name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = group.Key;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }
        }
    }
}

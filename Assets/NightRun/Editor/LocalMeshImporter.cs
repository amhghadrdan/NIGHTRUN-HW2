using System;
using System.IO;
using System.Text;
using NightRun;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(2, "nrm")]
public sealed class LocalMeshImporter : ScriptedImporter
{
    static string Text(BinaryReader reader) => Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    static Vector3[] Vectors(BinaryReader reader, int count)
    {
        var result = new Vector3[count];
        for (int i = 0; i < count; i++) result[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        return result;
    }
    public override void OnImportAsset(AssetImportContext context)
    {
        using (var reader = new BinaryReader(File.OpenRead(context.assetPath)))
        {
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "NRM1") throw new InvalidDataException("Unsupported local mesh");
            int parts = reader.ReadInt32();
            var root = new GameObject(Path.GetFileNameWithoutExtension(context.assetPath));
            context.AddObjectToAsset("root", root); context.SetMainObject(root);
            bool car = root.name.StartsWith("car_", StringComparison.Ordinal);
            for (int i = 0; i < parts; i++)
            {
                string node = Text(reader), materialName = Text(reader), texture = Text(reader);
                int count = reader.ReadInt32(), indexCount = reader.ReadInt32();
                var vertices = Vectors(reader, count); var normals = Vectors(reader, count);
                var uv = new Vector2[count];
                for (int j = 0; j < count; j++) uv[j] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                var indices = new int[indexCount];
                for (int j = 0; j < indexCount; j++) indices[j] = reader.ReadInt32();
                // Original mobile shadow overlays and additive lamp sprites
                // require their own renderer; omit them from the solid car.
                if (car && (materialName == "shadow" || materialName == "glow" || materialName == "glows" ||
                    (materialName.StartsWith("glow", StringComparison.Ordinal) && node.Contains("front_and_back")))) continue;
                var mesh = new Mesh { name = node + " " + materialName, vertices = vertices, normals = normals, uv = uv, triangles = indices };
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var material = MakeMaterial(materialName, texture, car, root.name, context);
                var child = new GameObject(node); child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.AddComponent<MeshRenderer>().sharedMaterial = material;
                context.AddObjectToAsset("mesh" + i, mesh); context.AddObjectToAsset("mat" + i, material);
            }
        }
    }

    static Material MakeMaterial(string name, string textureName, bool car, string model, AssetImportContext context)
    {
        string key = name.ToLowerInvariant();
        var material = new Material(Shader.Find("Standard")) { name = name, enableInstancing = true };
        material.color = Color.white;
        material.SetFloat("_Glossiness", car ? .55f : .2f);
        string stem = Path.GetFileNameWithoutExtension(textureName).ToLowerInvariant();
        if (model == "car_bmw_m3_opt" && stem == "car_bmw_f30_df") stem = "car_bmw_m3_df";
        string path = "Assets/LocalAsphalt/Textures/" + stem + ".png";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture) { material.mainTexture = texture; context.DependsOnSourceAsset(path); }
        if (key.Contains("carpaint") || key == "roof")
        {
            material.color = new Color(.55f, .59f, .64f); material.SetFloat("_Metallic", .6f); material.SetFloat("_Glossiness", .78f);
        }
        else if (key.Contains("glow"))
        {
            material.EnableKeyword("_EMISSION");
            if (texture) material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", Color.white * .85f);
        }
        else if (key.Contains("glass") || key.Contains("mirror"))
        {
            material.color = new Color(.035f, .055f, .075f); material.SetFloat("_Metallic", .5f); material.SetFloat("_Glossiness", .9f);
        }
        else if (key.Contains("chrome") || key.Contains("rims") || key.Contains("brake_disk"))
        {
            material.color = new Color(.43f, .45f, .48f); material.SetFloat("_Metallic", .85f); material.SetFloat("_Glossiness", .72f);
        }
        else if (key.Contains("plastic") || key.Contains("tire") || key.Contains("tyre") || key == "bottom")
            material.color = new Color(.07f, .075f, .08f);
        else if (!texture && !car)
        {
            // Procedural SBS textures require the phone's generated cache.
            // Use explicit temporary surfaces until those files are supplied.
            bool vegetation = model.Contains("tree") || model.Contains("palm");
            material.color = vegetation ? new Color(.21f, .29f, .18f) : new Color(.65f, .62f, .57f);
            string fallback = key.Contains("roof") || key.Contains("slate") ? "brick_diff" : "plaster_diff";
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/" + fallback + ".jpg");
        }
        return material;
    }
}

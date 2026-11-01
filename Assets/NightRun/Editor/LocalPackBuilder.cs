using System;
using System.Collections.Generic;
using System.IO;
using NightRun;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LocalPackBuilder
{
    const string Root = "Assets/LocalAsphalt/";
    static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Models/" + name + ".nrm");

    static GameObject Prefab(string name, float length = 0)
    {
        var original = Model(name);
        if (!original) throw new InvalidOperationException("Missing converted model " + name);
        var root = new GameObject(name);
        var visual = UnityEngine.Object.Instantiate(original, root.transform);
        var bounds = LocalAssetPack.BoundsOf(root.transform);
        float scale = length > 0 ? length / bounds.size.z : 1;
        visual.transform.localScale = Vector3.one * scale;
        visual.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "Generated/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root); return prefab;
    }

    [MenuItem("NIGHTRUN/Import Local Asphalt Pack")]
    public static void Generate()
    {
        Directory.CreateDirectory(Root + "Generated"); Directory.CreateDirectory(Root + "Resources");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string path = Root + "Resources/LocalAsphaltCatalog.asset";
        var pack = AssetDatabase.LoadAssetAtPath<LocalAssetPack>(path);
        if (!pack) { pack = ScriptableObject.CreateInstance<LocalAssetPack>(); AssetDatabase.CreateAsset(pack, path); }
        pack.playerCar = Prefab("car_bmw_m3_opt", 4.4f);
        pack.traffic = new[] { Prefab("car_traffic_car_opt", 4.4f), Prefab("car_mini_cooper_s_opt", 4.1f), Prefab("car_ford_mustang_2015_opt", 4.4f), Prefab("car_nissan_gtr_opt", 4.4f) };
        var buildings = new List<GameObject>();
        foreach (string name in new[] { "house_ita", "house_seaside_red_ita", "house_seaside_yellow_ita", "house_seaside_orange_ita", "gas_station_house", "swz_house", "aqueduct_house", "lighthouse" }) buildings.Add(Prefab(name));
        pack.buildings = buildings.ToArray();
        // The APK contains compiled Substance graphs, not their rendered city
        // textures. Activate these buildings only after importing that cache.
        pack.buildingsHaveGeneratedTextures = false;
        pack.props = new[] { Prefab("tree_joshua"), Prefab("tree_palm_a_000"), Prefab("barrel") };
        pack.fuel = Prefab("gen_prop_nitro");
        pack.music = Clip("m_bgm_rock_01"); pack.impact = Clip("sfx_med_impact_1"); pack.countdown = Clip("sfx_race_start_single");
        pack.click = Clip("sfx_ui_line_appear"); pack.pickup = Clip("sfx_lap_count");
        var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/russo_one.ttf");
        if (font && !pack.font)
        {
            pack.font = TMP_FontAsset.CreateFontAsset(font); pack.font.name = "Asphalt Russo One SDF";
            AssetDatabase.CreateAsset(pack.font, Root + "Generated/RussoOne.asset");
            pack.font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;/+-%()[]!?_→–×");
            foreach (var atlas in pack.font.atlasTextures) if (atlas) AssetDatabase.AddObjectToAsset(atlas, pack.font);
            if (pack.font.material) AssetDatabase.AddObjectToAsset(pack.font.material, pack.font);
        }
        EditorUtility.SetDirty(pack); AssetDatabase.SaveAssets();
        Debug.Log("LOCAL ASPHALT PACK READY: BMW M3, 4 traffic variants, fuel, game audio and font. 8 converted buildings await the generated texture cache.");
    }
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/" + name + ".ogg");
    public static void BuildQA()
    {
        Generate(); Build(BuildTarget.StandaloneLinux64, "Builds/Asphalt/QA/NIGHTRUN.x86_64", true);
    }
    public static void BuildAll()
    {
        Generate();
        Build(BuildTarget.StandaloneLinux64, "Builds/Asphalt/Linux/NIGHTRUN.x86_64", false);
        Build(BuildTarget.StandaloneWindows64, "Builds/Asphalt/Windows/NIGHTRUN.exe", false);
    }
    static void Build(BuildTarget target, string path, bool debug)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/NightRun/Scenes/NIGHTRUN.unity" }, path, target, debug ? BuildOptions.Development : BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Local pack build failed: " + report.summary.result);
        Debug.Log("LOCAL PACK BUILD OK: " + path);
    }
}

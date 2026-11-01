using System;
using System.IO;
using System.Linq;
using NightRun;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.LowLevel;

public static class ProjectBuilder
{
    const string Root = "Assets/NightRun/Resources/";
    const string Scene = "Assets/NightRun/Scenes/NIGHTRUN.unity";
    [MenuItem("NIGHTRUN/Rebuild Project")]
    public static void Generate()
    {
        Directory.CreateDirectory(Root + "Generated"); Directory.CreateDirectory("Assets/NightRun/Scenes");
        if (!AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            AssetDatabase.ImportPackage(Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_FontAsset).Assembly).resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
        AssetDatabase.Refresh();
        var modelImporter = AssetImporter.GetAtPath("Assets/ThirdParty/Vehicles/Sportscar.obj") as ModelImporter;
        modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        modelImporter.isReadable = false; modelImporter.importCameras = modelImporter.importLights = modelImporter.importAnimation = false;
        modelImporter.SaveAndReimport();
        var normal = AssetImporter.GetAtPath("Assets/ThirdParty/Textures/asphalt_normal.jpg") as TextureImporter;
        normal.textureType = TextureImporterType.NormalMap; normal.SaveAndReimport();
        var config = LoadOrCreate<RunConfig>(Root + "RunConfig.asset");
        var assets = LoadOrCreate<GameAssets>(Root + "NightRunAssets.asset");
        assets.config = config; assets.car = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Vehicles/Sportscar.obj");
        assets.postProcess = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
        assets.asphalt = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/asphalt_diff.jpg");
        assets.asphaltNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/asphalt_normal.jpg");
        assets.brick = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/brick_diff.jpg");
        assets.concrete = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/concrete_diff.jpg");
        assets.plaster = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/plaster_diff.jpg");
        assets.metal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Textures/metal_diff.jpg");
        assets.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "Generated/Rajdhani SDF.asset");
        if (!assets.font)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ThirdParty/Fonts/Rajdhani-Bold.ttf");
            assets.font = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            assets.font.name = "Rajdhani SDF";
            assets.font.TryAddCharacters(string.Concat(Enumerable.Range(32, 95).Select(x => (char)x)));
            AssetDatabase.CreateAsset(assets.font, Root + "Generated/Rajdhani SDF.asset");
            AssetDatabase.AddObjectToAsset(assets.font.material, assets.font);
            foreach (var texture in assets.font.atlasTextures) { texture.name = "Rajdhani atlas"; AssetDatabase.AddObjectToAsset(texture, assets.font); }
            assets.font.atlasPopulationMode = AtlasPopulationMode.Static;
        }
        assets.controls = BuildControls();
        assets.paint = Material("Hero paint", new Color(.52f, .56f, .60f), .55f, .7f);
        assets.rubber = Material("Rubber and dark trim", new Color(.035f, .045f, .055f), .2f, .45f);
        assets.glass = Material("Smoked glass", new Color(.04f, .075f, .11f), .65f, .98f);
        assets.chrome = Material("Forged alloy", new Color(.32f, .37f, .43f), .85f, .85f);
        assets.lightWhite = Material("White LEDs", new Color(.83f, .9f, 1), .1f, .6f, 1.4f);
        assets.lightRed = Material("Red LEDs", new Color(.7f, .008f, .012f), .1f, .6f, .8f);
        assets.music = Clip("nightdrive"); assets.engine = Clip("engine"); assets.coin = Clip("coin"); assets.fuel = Clip("fuel");
        assets.impact = Clip("impact"); assets.nitro = Clip("nitro"); assets.countdown = Clip("countdown");
        assets.click = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/Audio/click1.ogg") ?? Clip("click");
        EditorUtility.SetDirty(assets); EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("NIGHTRUN · GameManager").AddComponent<GameManager>();
        var preview = new GameObject("Editor Preview"); preview.tag = "EditorOnly";
        VehicleVisual.Build(preview.transform, assets, new Color(.52f, .56f, .6f), true);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.SetParent(preview.transform); ground.transform.localScale = new Vector3(2, 1, 5);
        ground.GetComponent<Renderer>().sharedMaterial = assets.rubber;
        var cam = new GameObject("Preview Camera").AddComponent<Camera>(); cam.transform.SetParent(preview.transform);
        cam.transform.position = new Vector3(5, 3, -6); cam.transform.LookAt(Vector3.up * .5f);
        var light = new GameObject("Preview Light").AddComponent<Light>(); light.transform.SetParent(preview.transform);
        light.type = LightType.Directional; light.intensity = 1.4f; light.transform.rotation = Quaternion.Euler(35, -20, 0);
        EditorSceneManager.SaveScene(scene, Scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        PlayerSettings.companyName = "NightRun"; PlayerSettings.productName = "NIGHTRUN";
        PlayerSettings.bundleVersion = "1.0.0"; PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard_2_0);
        IncludeShaders();
        AssetDatabase.SaveAssets(); Debug.Log("NIGHTRUN project generated successfully.");
    }
    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value) return value;
        value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value;
    }
    static Material Material(string name, Color color, float metallic, float smoothness, float emission = 0)
    {
        string path = Root + "Generated/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Glossiness", smoothness); mat.enableInstancing = true;
        if (emission > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission); }
        EditorUtility.SetDirty(mat); return mat;
    }
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/" + name + ".wav");
    static InputActionAsset BuildControls()
    {
        string path = "Assets/NightRun/NightRunControls.inputactions";
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var gameplay = asset.AddActionMap("Gameplay");
        gameplay.AddAction("Left", InputActionType.Button).AddBinding("<Keyboard>/a").WithGroup("KeyboardMouse");
        gameplay.FindAction("Left").AddBinding("<Keyboard>/leftArrow");
        gameplay.AddAction("Right", InputActionType.Button).AddBinding("<Keyboard>/d");
        gameplay.FindAction("Right").AddBinding("<Keyboard>/rightArrow");
        gameplay.AddAction("Boost", InputActionType.Button).AddBinding("<Keyboard>/leftShift");
        gameplay.FindAction("Boost").AddBinding("<Keyboard>/rightShift");
        gameplay.AddAction("Pause", InputActionType.Button).AddBinding("<Keyboard>/escape");
        var ui = asset.AddActionMap("UI");
        ui.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
        ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");
        ui.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
        var navigate = ui.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
        navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        ui.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter"); ui.FindAction("Submit").AddBinding("<Keyboard>/space");
        ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        File.WriteAllText(path, asset.ToJson()); UnityEngine.Object.DestroyImmediate(asset);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
    }
    static void IncludeShaders()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var list = settings.FindProperty("m_AlwaysIncludedShaders");
        string[] names = { "Standard", "NightRun/NightSky", "NightRun/Architecture", "NightRun/Foliage", "NightRun/WetRoad", "NightRun/ExhaustFlame", "Unlit/Color", "Legacy Shaders/Particles/Additive" };
        foreach (string name in names)
        {
            var shader = Shader.Find(name); bool exists = false;
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) exists = true;
            if (!exists && shader) { int index = list.arraySize; list.InsertArrayElementAtIndex(index); list.GetArrayElementAtIndex(index).objectReferenceValue = shader; }
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Build(BuildTarget target, string path, bool development = false)
    {
        if (Directory.Exists("Assets/LocalAsphalt"))
            throw new InvalidOperationException("Public builds exclude the optional local asset pack. Use Tools/build.sh to temporarily isolate it.");
        if (!File.Exists(Scene)) Generate();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report = BuildPipeline.BuildPlayer(new[] { Scene }, path, target, development ? BuildOptions.Development : BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
        Debug.Log("NIGHTRUN BUILD OK: " + path + " (" + report.summary.totalSize + " bytes)");
    }
    [MenuItem("NIGHTRUN/Build Linux QA")]
    public static void BuildLinuxQA() => Build(BuildTarget.StandaloneLinux64, "Builds/QA/NIGHTRUN.x86_64", true);
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/NIGHTRUN.exe");
    public static void BuildWindowsQA() => Build(BuildTarget.StandaloneWindows64, "Builds/WindowsQA/NIGHTRUN.exe", true);
    public static void RebuildQA() { Generate(); BuildLinuxQA(); }
    [MenuItem("NIGHTRUN/Build Windows and Linux")]
    public static void BuildAll()
    {
        Build(BuildTarget.StandaloneLinux64, "Builds/Linux/NIGHTRUN.x86_64");
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/NIGHTRUN.exe");
    }
}

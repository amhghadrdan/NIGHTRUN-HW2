using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace NightRun
{
    public sealed class GameAssets : ScriptableObject
    {
        public RunConfig config;
        public GameObject car;
        public LocalAssetPack localPack;
        public TMP_FontAsset font;
        public Texture2D asphalt, asphaltNormal;
        public Texture2D brick, concrete, plaster, metal;
        public InputActionAsset controls;
        public AudioClip music, engine, coin, fuel, impact, nitro, click, countdown;
        public Material paint, glass, rubber, chrome, lightWhite, lightRed;
        public UnityEngine.Rendering.PostProcessing.PostProcessResources postProcess;
    }
}

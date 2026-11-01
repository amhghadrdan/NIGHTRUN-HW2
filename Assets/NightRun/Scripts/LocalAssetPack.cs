using UnityEngine;

namespace NightRun
{
    /// <summary>Optional assets converted locally from a user-supplied game.</summary>
    public sealed class LocalAssetPack : ScriptableObject
    {
        public GameObject playerCar;
        public GameObject[] traffic, buildings, props;
        public bool buildingsHaveGeneratedTextures;
        public GameObject fuel;
        public TMPro.TMP_FontAsset font;
        public AudioClip music, impact, countdown, click, pickup;

        public void Apply(GameAssets assets)
        {
            assets.localPack = this;
            if (font) assets.font = font;
            if (music) assets.music = music;
            if (impact) assets.impact = impact;
            if (countdown) assets.countdown = countdown;
            if (click) assets.click = click;
            if (pickup) assets.fuel = pickup;
        }

        public Transform Vehicle(Transform parent, Color paint, bool player, int variant)
        {
            var prefab = player ? playerCar : traffic[variant % traffic.Length];
            var root = Instantiate(prefab, parent).transform;
            root.name = player ? "Imported BMW M3" : "Imported traffic vehicle";
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = player ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                if (!renderer.sharedMaterial.name.ToLowerInvariant().Contains("carpaint")) continue;
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", paint);
                renderer.SetPropertyBlock(block);
            }
            return root;
        }

        public bool Building(Transform parent, BuildingPlot plot, int segment)
        {
            if (!buildingsHaveGeneratedTextures || buildings == null || buildings.Length == 0) return false;
            int index = (segment * 7 + plot.Style + (plot.Side > 0 ? 3 : 0)) % buildings.Length;
            var root = Instantiate(buildings[index], parent).transform;
            // Prefabs have a centered footprint and floor at y=0. Fit uniformly
            // to the existing disjoint lots; preserve the model's proportions.
            var bounds = BoundsOf(root);
            float scale = Mathf.Min(plot.Size.x / bounds.size.x, plot.Size.z / bounds.size.z, 16 / bounds.size.y);
            root.localScale *= scale;
            root.localPosition = new Vector3(plot.Center.x, 0, plot.Center.z);
            if (plot.Side > 0) root.localRotation = Quaternion.Euler(0, 180, 0);
            return true;
        }

        public static Bounds BoundsOf(Transform root)
        {
            bool found = false;
            var result = new Bounds();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var b = filter.sharedMesh.bounds;
                var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = matrix.MultiplyPoint3x4(corner);
                    if (found) result.Encapsulate(p); else { result = new Bounds(p, Vector3.zero); found = true; }
                }
            }
            return result;
        }
    }
}

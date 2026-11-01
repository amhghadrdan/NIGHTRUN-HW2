using UnityEngine;
using UnityEngine.Rendering;
namespace NightRun
{
    public static class VehicleVisual
    {
        public static Transform Build(Transform parent, GameAssets assets, Color color, bool player, int variant = 0)
        {
            if (assets.localPack) return assets.localPack.Vehicle(parent, color, player, variant);
            var car = Object.Instantiate(assets.car, parent).transform;
            car.name = "Sportscar visual"; car.localPosition = Vector3.up * .08f;
            car.localRotation = Quaternion.identity;
            car.localScale = new Vector3(1, 1.18f, 1.22f);
            var paint = Visuals.Mat("Paint " + color.ToString(), color, .55f, .7f);
            foreach (var r in car.GetComponentsInChildren<MeshRenderer>())
            {
                var source = r.sharedMaterials;
                for (int i = 0; i < source.Length; i++)
                {
                    string name = source[i] ? source[i].name.ToLowerInvariant() : "";
                    if (name.Contains("paint_1")) source[i] = paint;
                    else if (name.Contains("glass") || name.Contains("mirror")) source[i] = assets.glass;
                    else if (name.Contains("headlight")) source[i] = assets.lightWhite;
                    else if (name.Contains("brakelight") || name.Contains("signallight")) source[i] = assets.lightRed;
                    else if (name.Contains("rim") || name.Contains("disc") || name.Contains("hardware")) source[i] = assets.chrome;
                    else source[i] = assets.rubber;
                }
                r.sharedMaterials = source;
                r.shadowCastingMode = player ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
            if (!player)
            {
                Visuals.Box("Traffic left taillight", parent, new Vector3(-.65f, .65f, -2.08f), new Vector3(.26f, .07f, .04f), assets.lightRed);
                Visuals.Box("Traffic right taillight", parent, new Vector3(.65f, .65f, -2.08f), new Vector3(.26f, .07f, .04f), assets.lightRed);
            }
            else
            {
                var black = Visuals.Mat("Spoiler carbon", new Color(.045f, .05f, .055f), .4f, .6f);
                for (int side = -1; side <= 1; side += 2)
                {
                    Visuals.Box("Spoiler mount", parent, new Vector3(side * .6f, 1.02f, -1.65f), new Vector3(.07f, .3f, .13f), black);

                }
                var wing = Visuals.Box("Rear wing", parent, new Vector3(0, 1.19f, -1.65f), new Vector3(1.84f, .06f, .29f), black);
                wing.transform.localRotation = Quaternion.Euler(-8, 0, 0);
                Visuals.Box("Rear plate", parent, new Vector3(0, .42f, -2.16f), new Vector3(.46f, .2f, .018f), assets.chrome);
                var plate = new GameObject("NIGHTRUN plate").AddComponent<TMPro.TextMeshPro>(); plate.transform.SetParent(parent, false);
                plate.transform.localPosition = new Vector3(0, .42f, -2.18f); plate.font = assets.font; plate.fontSize = .75f;
                plate.text = "NIGHTRUN"; plate.alignment = TMPro.TextAlignmentOptions.Center;
                plate.rectTransform.sizeDelta = new Vector2(.45f, .18f); plate.color = Color.black;
            }
            return car;
        }
    }
}

using TMPro;
using UnityEngine;

namespace NightRun
{
    /// <summary>Human-scale industrial architecture. Facade details are mesh geometry in fixed local coordinates.</summary>
    public sealed class CityBuilder
    {
        readonly GameAssets assets;
        readonly Material[] walls = new Material[6];
        readonly Material concrete, metal, darkMetal, glass, warmWindow, paleWindow, roof, leaves, bark, lamp, asphalt, line;
        public CityBuilder(GameAssets assets)
        {
            this.assets = assets;
            walls[0] = Wall("Weathered red brick", assets.brick, new Color(.58f, .49f, .43f));
            walls[1] = Wall("Dark brick", assets.brick, new Color(.39f, .38f, .37f));
            walls[2] = Wall("Gray stone", assets.concrete, new Color(.67f, .69f, .68f));
            walls[3] = Wall("Old white plaster", assets.plaster, new Color(.7f, .71f, .68f));
            walls[4] = Wall("Industrial wall", assets.metal, new Color(.49f, .53f, .55f));
            walls[5] = Wall("Worn tan brick", assets.brick, new Color(.68f, .62f, .53f));
            concrete = Wall("Aged road concrete", assets.concrete, new Color(.55f, .57f, .57f));
            asphalt = Wall("Sidewalk concrete", assets.concrete, new Color(.38f, .4f, .41f));
            metal = Wall("Galvanized shutters", assets.metal, new Color(.45f, .48f, .48f));
            darkMetal = Visuals.Mat("Iron frames", new Color(.055f, .065f, .072f), .5f, .38f);
            glass = Visuals.Mat("Unlit windows", new Color(.07f, .09f, .12f), .35f, .8f);
            warmWindow = Visuals.Mat("Warm interior windows", new Color(.65f, .49f, .29f), 0, .25f, .6f);
            paleWindow = Visuals.Mat("Neutral interior windows", new Color(.51f, .54f, .52f), 0, .3f, .5f);
            roof = Visuals.Mat("Weathered roof", new Color(.11f, .13f, .14f), .2f, .25f);
            leaves = new Material(Shader.Find("NightRun/Foliage")) { name = "Dappled night foliage" };
            bark = Visuals.Mat("Tree bark", new Color(.14f, .12f, .095f), 0, .15f);
            lamp = Visuals.Mat("Sodium streetlight lens", new Color(1, .83f, .58f), 0, .5f, 1.8f);
            line = Visuals.Mat("Natural road paint", new Color(.77f, .78f, .74f), 0, .3f);
        }
        static Material Wall(string name, Texture2D texture, Color tint)
        {
            var material = new Material(Shader.Find("NightRun/Architecture")) { name = name, enableInstancing = true };
            material.mainTexture = texture; material.SetColor("_Color", tint); material.SetFloat("_Scale", .4f); return material;
        }
        public void Build(Transform parent, int segment, Material roadMaterial)
        {
            var mesh = new EnvironmentMesh();
            mesh.Box(new Vector3(0, -.34f, 30), new Vector3(120, .12f, 60), asphalt);
            mesh.Box(new Vector3(0, -.16f, 30), new Vector3(13.4f, .3f, 60), roadMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                mesh.Box(new Vector3(side * 10.4f, -.12f, 30), new Vector3(7.4f, .35f, 60), asphalt);
                mesh.Box(new Vector3(side * 6.85f, .42f, 30), new Vector3(.45f, .85f, 60), concrete);
                mesh.Box(new Vector3(side * 6.85f, .87f, 30), new Vector3(.5f, .07f, 60), darkMetal);
                mesh.Box(new Vector3(side * 5.5f, .006f, 30), new Vector3(.12f, .012f, 60), line);
                for (int z = 0; z < 60; z += 6)
                {
                    mesh.Box(new Vector3(side * 6.58f, .52f, z + 3), new Vector3(.025f, .15f, .34f), z % 12 == 0 ? line : lamp);
                    mesh.Box(new Vector3(side * 6.85f, .84f, z + 3), new Vector3(.09f, .03f, .045f), darkMetal);
                }
                int lampZ = side < 0 ? 15 : 43;
                mesh.Cylinder(new Vector3(side * 10.1f, .08f, lampZ), .065f, 8.4f, darkMetal, 8);
                mesh.Beam(new Vector3(side * 10.1f, 8.4f, lampZ), new Vector3(side * 7.6f, 8.7f, lampZ), .055f, darkMetal);
                mesh.Box(new Vector3(side * 7.6f, 8.65f, lampZ), new Vector3(.8f, .1f, .25f), lamp);
                var light = Visuals.Light("Warm practical streetlight", parent, new Vector3(side * 7.4f, 6.9f, lampZ), new Color(1, .84f, .64f), 3.5f, 22);
                light.type = LightType.Spot; light.spotAngle = 118; light.transform.localRotation = Quaternion.Euler(75, side < 0 ? 90 : -90, 0);
                // Posts and a slack overhead utility wire, well outside the drivable lanes.
                mesh.Cylinder(new Vector3(side * 13, 0, 3), .1f, 9.5f, bark, 7);
                mesh.Box(new Vector3(side * 13, 9, 3), new Vector3(1.6f, .12f, .12f), darkMetal);
                for (int wire = 0; wire < 2; wire++) for (int j = 0; j < 6; j++)
                {
                    float z0 = j * 10, z1 = (j + 1) * 10;
                    float y0 = 9 - Mathf.Sin(z0 / 60 * Mathf.PI) * .7f, y1 = 9 - Mathf.Sin(z1 / 60 * Mathf.PI) * .7f;
                    mesh.Beam(new Vector3(side * 13 + wire * .6f, y0, z0), new Vector3(side * 13 + wire * .6f, y1, z1), .018f, darkMetal);
                }
            }
            for (int z = 0; z < 60; z += 10)
                foreach (float x in new[] { -1.8f, 1.8f }) mesh.Box(new Vector3(x, .006f, z + 2), new Vector3(.12f, .012f, 4), line);
            foreach (var plot in EnvironmentLayout.Create(segment)) BuildBuilding(mesh, parent, plot, segment);
            // Occasional solid overhead road sign, without emissive gantries.
            if (segment % 5 == 1)
            {
                mesh.Cylinder(new Vector3(-7.8f, 0, 52), .13f, 7.6f, darkMetal);
                mesh.Cylinder(new Vector3(7.8f, 0, 52), .13f, 7.6f, darkMetal);
                mesh.Box(new Vector3(0, 7.5f, 52), new Vector3(16, .18f, .2f), darkMetal);
                mesh.Box(new Vector3(0, 6.9f, 52), new Vector3(6, 1.2f, .14f), Visuals.Mat("Highway sign green", new Color(.06f, .18f, .12f), 0, .25f));
                Label(parent, "DOWNTOWN  /  NORTH", new Vector3(0, 6.9f, 51.91f), Quaternion.identity, 5.6f, .7f, .42f);
            }
            mesh.Finish(parent, "Highway block " + segment);
        }
        void BuildBuilding(EnvironmentMesh mesh, Transform parent, BuildingPlot p, int segment)
        {
            if (assets.localPack && assets.localPack.Building(parent, p, segment)) return;
            Vector3 c = p.Center, s = p.Size;
            float face = c.x - p.Side * s.x / 2;
            if (p.Kind == BuildingKind.Yard)
            {
                mesh.Box(new Vector3(c.x, .05f, c.z), new Vector3(s.x, .1f, s.z), asphalt);
                for (int j = 0; j < 3; j++)
                {
                    var pos = new Vector3(c.x + (j % 2) * 3 - 1.5f, 1.35f, c.z - 5 + j * 5);
                    mesh.Box(pos, new Vector3(3, 2.7f, 4), j % 2 == 0 ? walls[4] : walls[1]);
                    mesh.Box(pos + new Vector3(-p.Side * 1.53f, 0, 0), new Vector3(.06f, 2.5f, 3.8f), metal);
                }
                Fence(mesh, face, c.z - s.z / 2, c.z + s.z / 2);
                Tree(mesh, new Vector3(c.x + p.Side * 5, 0, c.z), segment * 11 + p.Style);
                return;
            }
            Material wall = p.Kind == BuildingKind.Garage ? walls[3] : p.Kind == BuildingKind.Warehouse ? walls[1] :
                p.Kind == BuildingKind.Office ? walls[2] : p.Kind == BuildingKind.Workshop ? walls[4] : walls[p.Style % 3 == 0 ? 5 : 0];
            mesh.Box(c, s, wall);
            mesh.Box(new Vector3(c.x, .38f, c.z), new Vector3(s.x + .1f, .76f, s.z + .1f), concrete);
            mesh.Box(new Vector3(c.x, s.y + .12f, c.z), new Vector3(s.x + .55f, .24f, s.z + .55f), roof);
            // Copings have actual separation from the wall and roof surfaces.
            mesh.Box(new Vector3(face - p.Side * .09f, s.y + .25f, c.z), new Vector3(.22f, .33f, s.z + .3f), concrete);
            mesh.Box(new Vector3(c.x, s.y + .3f, c.z - s.z / 2), new Vector3(s.x, .36f, .2f), concrete);
            mesh.Box(new Vector3(c.x, s.y + .3f, c.z + s.z / 2), new Vector3(s.x, .36f, .2f), concrete);
            if (p.Kind == BuildingKind.Garage || p.Kind == BuildingKind.Warehouse || p.Kind == BuildingKind.Workshop)
            {
                for (int j = -1; j <= 1; j++)
                {
                    float z = c.z + j * (s.z / 3.5f);
                    mesh.Box(new Vector3(face - p.Side * .04f, 2, z), new Vector3(.07f, 3.1f, 3.3f), metal);
                    mesh.Box(new Vector3(face - p.Side * .1f, 3.6f, z), new Vector3(.15f, .15f, 3.6f), concrete);
                    mesh.Box(new Vector3(face - p.Side * .11f, .6f, z - 1.75f), new Vector3(.18f, 1.2f, .13f), darkMetal);
                    mesh.Box(new Vector3(face - p.Side * .11f, .6f, z + 1.75f), new Vector3(.18f, 1.2f, .13f), darkMetal);
                    mesh.Box(new Vector3(face - p.Side * .3f, 4.15f, z), new Vector3(.6f, .13f, .45f), lamp);
                }
                string sign = p.Kind == BuildingKind.Garage ? "AUTO SERVICE" : p.Kind == BuildingKind.Workshop ? "MOTOR WORKS" : "NORTH WAREHOUSE";
                mesh.Box(new Vector3(face - p.Side * .08f, s.y - .7f, c.z), new Vector3(.12f, .9f, s.z * .85f), walls[3]);
                Label(parent, sign, new Vector3(face - p.Side * .16f, s.y - .7f, c.z), Quaternion.Euler(0, p.Side * 90, 0), s.z * .8f, .8f, .53f, true);
            }
            int floors = Mathf.Max(1, Mathf.FloorToInt(s.y / 3.2f));
            int start = p.Kind == BuildingKind.Garage || p.Kind == BuildingKind.Workshop || p.Kind == BuildingKind.Warehouse ? 1 : 0;
            int columns = Mathf.FloorToInt(s.z / 3.1f);
            for (int floor = start; floor < floors; floor++)
            {
                float y = 2.0f + floor * 3.15f;
                if (y + .9f > s.y) continue;
                for (int col = 0; col < columns; col++)
                {
                    float z = c.z + (col - (columns - 1) * .5f) * 2.9f;
                    int pattern = col * 17 + floor * 11 + p.Style * 23 + segment;
                    Window(mesh, new Vector3(face - p.Side * .055f, y, z), p.Side, pattern, false);
                }
                // End-wall windows complete the 3D silhouette, not just the road-facing facade.
                for (int col = 0; col < Mathf.FloorToInt(s.x / 3.2f); col++)
                {
                    float x = c.x + (col - Mathf.FloorToInt(s.x / 3.2f) * .5f + .5f) * 2.8f;
                    Window(mesh, new Vector3(x, y, c.z - s.z / 2 - .055f), -1, col + floor * 13 + segment * 7, true);
                    Window(mesh, new Vector3(x, y, c.z + s.z / 2 + .055f), 1, col + floor * 13 + segment * 7, true);
                }
                if (floor > 0)
                    mesh.Box(new Vector3(face - p.Side * .06f, floor * 3.15f + .45f, c.z), new Vector3(.14f, .12f, s.z), concrete);
            }
            // Ventilators and occasional rooftop water tanks make the roof profiles distinct.
            mesh.Box(new Vector3(c.x + s.x * .2f, s.y + .55f, c.z + 2), new Vector3(1.7f, .8f, 2.1f), metal);
            if ((segment + p.Style) % 4 == 0 && p.Kind != BuildingKind.Office)
            {
                Vector3 tank = new Vector3(c.x, s.y + 1.4f, c.z - 3);
                for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                    mesh.Box(tank + new Vector3(x * .8f, -.65f, z * .8f), new Vector3(.12f, 1.3f, .12f), darkMetal);
                mesh.Cylinder(tank, 1.35f, 2.1f, metal, 16);
                mesh.Cylinder(tank + Vector3.up * 2.1f, 1.4f, .13f, roof, 16);
            }
            if ((segment + p.Style) % 3 == 0)
                Tree(mesh, new Vector3(c.x + p.Side * 10, 0, c.z + s.z / 2 + 2), segment * 19 + p.Style);
        }
        void Window(EnvironmentMesh mesh, Vector3 pos, int side, int variation, bool endWall)
        {
            Material pane = variation % 8 < 2 ? warmWindow : variation % 8 == 3 ? paleWindow : glass;
            Vector3 normal = endWall ? Vector3.forward * side : Vector3.right * -side;
            Vector3 across = endWall ? Vector3.right : Vector3.forward;
            Vector3 size = endWall ? new Vector3(1.15f, 1.65f, .045f) : new Vector3(.045f, 1.65f, 1.15f);
            mesh.Box(pos, size, pane);
            Vector3 frame = pos + normal * .045f;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                mesh.Box(frame + across * (direction * .61f), endWall ? new Vector3(.07f, 1.8f, .09f) : new Vector3(.09f, 1.8f, .07f), darkMetal);
                mesh.Box(frame + Vector3.up * (direction * .86f), endWall ? new Vector3(1.3f, .07f, .09f) : new Vector3(.09f, .07f, 1.3f), darkMetal);
            }
            mesh.Box(frame, endWall ? new Vector3(.035f, 1.65f, .09f) : new Vector3(.09f, 1.65f, .035f), darkMetal);
            mesh.Box(frame - Vector3.up * .94f + normal * .04f, endWall ? new Vector3(1.4f, .13f, .25f) : new Vector3(.25f, .13f, 1.4f), concrete);
        }
        void Fence(EnvironmentMesh mesh, float x, float z0, float z1)
        {
            for (float z = z0; z < z1; z += 2.5f) mesh.Box(new Vector3(x, 1.2f, z), new Vector3(.075f, 2.4f, .075f), darkMetal);
            for (float y = .2f; y < 2.4f; y += .23f) mesh.Beam(new Vector3(x, y, z0), new Vector3(x, y, z1), .012f, darkMetal);
        }
        void Tree(EnvironmentMesh mesh, Vector3 pos, int seed)
        {
            mesh.Cylinder(pos, .15f, 2.9f, bark, 7);
            mesh.Beam(pos + Vector3.up * 2, pos + new Vector3(1.6f, 4, .2f), .1f, bark);
            mesh.Beam(pos + Vector3.up * 2.5f, pos + new Vector3(-1.5f, 4.2f, -.3f), .1f, bark);
            mesh.Foliage(pos + Vector3.up * 4, new Vector3(1.7f, 2.3f, 1.6f), leaves, seed);
        }
        void Label(Transform parent, string value, Vector3 pos, Quaternion rotation, float width, float height, float size, bool dark = false)
        {
            var go = new GameObject(value); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = rotation;
            var text = go.AddComponent<TextMeshPro>(); text.font = assets.font; text.text = value; text.fontSize = size * 10;
            text.color = dark ? new Color(.07f, .08f, .09f) : new Color(.79f, .8f, .73f); text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(width, height); text.enableWordWrapping = false;
        }
        public void BuildSkyline(Transform parent)
        {
            var mesh = new EnvironmentMesh(); var random = new System.Random(902);
            for (int i = 0; i < 24; i++)
            {
                float x = (i - 11.5f) * 13, h = 18 + (float)random.NextDouble() * 38, z = 470 + random.Next(100);
                mesh.Box(new Vector3(x, h / 2 - 4, z), new Vector3(8 + random.Next(8), h, 10 + random.Next(7)), walls[i % 3]);
                mesh.Box(new Vector3(x, h - 3.5f, z), new Vector3(4, 1, 5), roof);
                for (int j = 0; j < 5; j++)
                    mesh.Box(new Vector3(x - 2 + j, h * .65f, z - 6), new Vector3(.4f, 1.1f, .05f), j % 2 == 0 ? paleWindow : warmWindow);
            }
            mesh.Finish(parent, "Distant downtown skyline");
        }
    }
}

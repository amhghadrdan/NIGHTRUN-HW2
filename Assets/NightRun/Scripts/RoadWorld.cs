using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightRun
{
    public sealed class RoadWorld : MonoBehaviour
    {
        GameManager game;
        ObjectPool<Transform> roads;
        ObjectPool<Interactable> fixedCars, movingCars, coins, fuel;
        readonly List<Transform> segments = new List<Transform>();
        readonly List<Interactable> active = new List<Interactable>(160);
        readonly List<TrafficForecast> forecast = new List<TrafficForecast>(64);
        readonly Dictionary<Transform, Light[]> roadLights = new Dictionary<Transform, Light[]>();
        System.Random random;
        float trafficTimer, coinTimer, fuelTimer;
        Material roadMat;
        CityBuilder city;
        BatFlock bats;
        public EffectsPool Effects { get; private set; }
        public IReadOnlyList<Interactable> Active => active;
        public int PooledActive => active.Count;
        public int SpawnRejected { get; private set; }
        public int BatCount => bats.Count;
        public const int TotalPoolCapacity = 12 + 24 + 24 + 100 + 8 + 24;

        public void Initialize(GameManager owner)
        {
            game = owner;
            RenderSettings.skybox = new Material(Shader.Find("NightRun/NightSky"));
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.18f, .21f, .27f);
            RenderSettings.ambientEquatorColor = new Color(.16f, .17f, .19f);
            RenderSettings.ambientGroundColor = new Color(.06f, .065f, .075f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.048f, .058f, .075f); RenderSettings.fogDensity = .0026f;
            var moon = new GameObject("Cool moonlight").AddComponent<Light>();
            moon.type = LightType.Directional; moon.color = new Color(.77f, .84f, 1); moon.intensity = .85f;
            moon.transform.rotation = Quaternion.Euler(32, -25, 0); moon.shadows = LightShadows.Soft;
            roadMat = new Material(Shader.Find("NightRun/WetRoad")) { name = "Natural damp asphalt", enableInstancing = true };
            roadMat.SetTexture("_MainTex", game.Assets.asphalt); roadMat.SetTexture("_BumpMap", game.Assets.asphaltNormal);
            city = new CityBuilder(game.Assets);
            var skyline = new GameObject("Distant skyline").transform; skyline.SetParent(transform); city.BuildSkyline(skyline);
            bats = new GameObject("Animated bat flock").AddComponent<BatFlock>(); bats.transform.SetParent(transform); bats.Initialize();
            roads = new ObjectPool<Transform>(12, BuildSegment);
            fixedCars = new ObjectPool<Interactable>(24, i => MakeCar(false, i));
            movingCars = new ObjectPool<Interactable>(24, i => MakeCar(true, i));
            coins = new ObjectPool<Interactable>(100, i => MakePickup(false));
            fuel = new ObjectPool<Interactable>(8, i => MakePickup(true));
            Effects = new GameObject("Pooled particles").AddComponent<EffectsPool>(); Effects.transform.SetParent(transform); Effects.Initialize();
            var probe = new GameObject("Environment reflections").AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(0, 3, 15); probe.size = new Vector3(1000, 160, 1500);
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.resolution = 128; probe.intensity = .8f; probe.cullingMask = 0; probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.RenderProbe();
        }
        Transform BuildSegment(int index)
        {
            var root = new GameObject("Road segment " + index).transform; root.SetParent(transform);
            city.Build(root, index, roadMat);
            roadLights[root] = root.GetComponentsInChildren<Light>();
            return root;
        }
        Interactable MakeCar(bool moving, int index)
        {
            var root = new GameObject(moving ? "Moving traffic" : "Stopped traffic"); root.transform.SetParent(transform);
            Color[] paints = { new Color(.3f, .36f, .44f), new Color(.7f, .1f, .1f), new Color(.85f, .7f, .12f), new Color(.2f, .28f, .35f) };
            VehicleVisual.Build(root.transform, game.Assets, paints[index % paints.Length], false, index);
            return moving ? (Interactable)root.AddComponent<MovingTraffic>() : root.AddComponent<StationaryTraffic>();
        }
        Interactable MakePickup(bool isFuel)
        {
            var root = new GameObject(isFuel ? "Fuel can" : "Coin"); root.transform.SetParent(transform);
            var visual = new GameObject("Spinning pickup").transform; visual.SetParent(root.transform, false);
            if (isFuel && game.Assets.localPack && game.Assets.localPack.fuel)
            {
                var bottle = Instantiate(game.Assets.localPack.fuel, visual).transform;
                var bounds = LocalAssetPack.BoundsOf(bottle);
                float scale = .95f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                bottle.localScale = Vector3.one * scale;
                bottle.localPosition = Vector3.up * 1.2f - bounds.center * scale;
                Visuals.Light("Fuel glow", root.transform, Vector3.up, new Color(.1f, 1, .5f), .8f, 3);
            }
            else if (isFuel)
            {
                var green = Visuals.Mat("Fuel enamel", new Color(.05f, .8f, .43f), .5f, .6f, .7f);
                Visuals.Box("Can", visual, new Vector3(0, 1.2f, 0), new Vector3(.65f, .85f, .3f), green);
                Visuals.Box("Handle", visual, new Vector3(0, 1.75f, 0), new Vector3(.35f, .12f, .12f), green);
                Visuals.Box("Handle support", visual, new Vector3(-.23f, 1.65f, 0), new Vector3(.1f, .3f, .12f), green);
                Visuals.Box("Cap", visual, new Vector3(.25f, 1.7f, 0), new Vector3(.16f, .12f, .2f), game.Assets.chrome);
                Visuals.Box("Fuel symbol", visual, new Vector3(0, 1.2f, -.17f), new Vector3(.18f, .4f, .03f), game.Assets.lightWhite);
                Visuals.Light("Fuel glow", root.transform, Vector3.up, new Color(.1f, 1, .5f), .8f, 3);
            }
            else
            {
                var gold = Visuals.Mat("Coin gold", new Color(1, .7f, .08f), .7f, .7f, 1.1f);
                var disk = Visuals.Shape("Coin", PrimitiveType.Cylinder, visual, Vector3.up * 1.2f, new Vector3(.55f, .07f, .55f), gold);
                disk.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Visuals.Box("Coin stripe", visual, new Vector3(0, 1.2f, -.08f), new Vector3(.07f, .32f, .03f), game.Assets.lightWhite);
            }
            return isFuel ? (Interactable)root.AddComponent<FuelPickup>() : root.AddComponent<CoinPickup>();
        }
        public void ResetWorld(int seed = -1)
        {
            foreach (var item in active) Return(item); active.Clear();
            foreach (var segment in segments) roads.Return(segment); segments.Clear();
            for (int i = 0; i < 12; i++) { var road = roads.Rent(); road.position = new Vector3(0, 0, i * 60 - 60); segments.Add(road); }
            Effects.ResetEffects(); random = new System.Random(seed < 0 ? System.Environment.TickCount : seed);
            trafficTimer = 3; coinTimer = 2; fuelTimer = 7; SpawnRejected = 0;
        }
        public void Tick(float dt)
        {
            float speed = game.Run.Speed / 3.6f;
            foreach (var segment in segments)
            {
                segment.position -= Vector3.forward * (speed * dt);
                if (segment.position.z < -120)
                {
                    float max = -1000; foreach (var other in segments) max = Mathf.Max(max, other.position.z);
                    roads.Return(segment); var recycled = roads.Rent(); recycled.position = new Vector3(0, 0, max + 60);
                }
                foreach (var l in roadLights[segment])
                    l.enabled = l.transform.position.z > -35 && l.transform.position.z < 95;
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var item = active[i]; float oldZ = item.transform.position.z;
                item.Tick(dt, speed);
                if (!item.Consumed && SweptContact(oldZ, item.transform.position.z, game.Car.PreviousX, game.Car.transform.position.x, item.transform.position.x, item.HalfLength, item.IsObstacle ? 1.65f : 1.22f))
                {
                    item.TryInteract(game);
                    if (!item.IsObstacle) { Return(item); active.RemoveAt(i); continue; }
                }
                if (item.transform.position.z < -22) { Return(item); active.RemoveAt(i); }
            }
            trafficTimer -= dt; coinTimer -= dt; fuelTimer -= dt;
            if (trafficTimer <= 0) { SpawnTraffic(); trafficTimer += Mathf.Lerp(1.1f, .68f, Mathf.Clamp01(game.Run.Elapsed / 100)); }
            if (coinTimer <= 0) { SpawnCoins(); coinTimer += 1.8f + (float)random.NextDouble() * 1.8f; }
            if (fuelTimer <= 0) { SpawnFuel(); fuelTimer += 10 + (float)random.NextDouble() * 4; }
        }
        public static bool SweptContact(float fromZ, float toZ, float fromX, float toX, float itemX, float halfLength, float halfWidth)
        {
            float near = -halfLength - 2.1f, far = halfLength + 2.1f;
            if (Mathf.Min(fromZ, toZ) > far || Mathf.Max(fromZ, toZ) < near) return false;
            float zDelta = toZ - fromZ;
            float t0 = Mathf.Abs(zDelta) < .0001f ? 0 : Mathf.Clamp01((far - fromZ) / zDelta);
            float t1 = Mathf.Abs(zDelta) < .0001f ? 1 : Mathf.Clamp01((near - fromZ) / zDelta);
            float x0 = Mathf.Lerp(fromX, toX, Mathf.Min(t0, t1)), x1 = Mathf.Lerp(fromX, toX, Mathf.Max(t0, t1));
            return Mathf.Max(x0, x1) >= itemX - halfWidth && Mathf.Min(x0, x1) <= itemX + halfWidth;
        }
        void SpawnTraffic()
        {
            int count = game.Run.Elapsed < 20 ? 1 : (random.NextDouble() < .6 ? 2 : 1);
            int firstLane = random.Next(3);
            float z = 205 + (float)random.NextDouble() * 30;
            for (int k = 0; k < count; k++)
            {
                int lane = (firstLane + k) % 3;
                bool moving = random.NextDouble() < .4;
                var candidate = new TrafficForecast(lane, z, moving ? 22 : 0);
                if (NearPickupOrTraffic(candidate)) { SpawnRejected++; continue; }
                forecast.Clear();
                foreach (var car in active) if (car.IsObstacle && car.transform.position.z > -5) forecast.Add(new TrafficForecast(car.Lane, car.transform.position.z, car.ForwardSpeed));
                forecast.Add(candidate);
                if (!RoadPlanner.IsSafe(forecast, game.Car.TargetLane, game.Run.BaseSpeed / 3.6f)) { SpawnRejected++; continue; }
                var item = (moving ? movingCars : fixedCars).Rent(); if (!item) continue;
                item.OnSpawn(lane, z, game.Assets.config.laneWidth); active.Add(item);
            }
        }
        bool NearPickupOrTraffic(TrafficForecast candidate)
        {
            foreach (var item in active)
            {
                if (item.Lane != candidate.Lane) continue;
                float gap = Mathf.Abs(item.transform.position.z - candidate.Z);
                if (item.IsObstacle && gap < 32) return true;
                if (item is FuelPickup && gap < 65) return true;
            }
            return false;
        }
        void SpawnCoins()
        {
            int lane = random.Next(3); float z = 180;
            foreach (var item in active) if (item.IsObstacle && item.Lane == lane && Mathf.Abs(item.transform.position.z - z) < 35) return;
            for (int i = 0; i < 5; i++) { var item = coins.Rent(); if (!item) return; item.OnSpawn(lane, z + i * 5, game.Assets.config.laneWidth); active.Add(item); }
        }
        void SpawnFuel()
        {
            int lane = random.Next(3); const float z = 175;
            int firstLane = lane;
            int bestRisk = int.MaxValue;
            for (int offset = 0; offset < 3; offset++)
            {
                int candidate = (firstLane + offset) % 3, risk = 0;
                foreach (var car in active) if (car.IsObstacle && car.Lane == candidate && BlocksFuel(car, z)) risk++;
                if (risk < bestRisk) { bestRisk = risk; lane = candidate; }
                if (risk == 0) break;
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var car = active[i];
                if (car.IsObstacle && car.Lane == lane && BlocksFuel(car, z)) { Return(car); active.RemoveAt(i); }
            }
            var item = fuel.Rent(); if (!item) return;
            item.OnSpawn(lane, z, game.Assets.config.laneWidth); active.Add(item);
        }
        bool BlocksFuel(Interactable car, float z)
        {
            float speed = game.Run.BaseSpeed / 3.6f;
            float early = z / (speed * 1.25f), late = z / speed;
            float first = car.transform.position.z + car.ForwardSpeed * early;
            float last = car.transform.position.z + car.ForwardSpeed * late;
            return (first < z + 25 && last > z - 25) || Mathf.Abs(car.transform.position.z - z) < 30;
        }
        void Return(Interactable item)
        {
            if (item is StationaryTraffic) fixedCars.Return(item);
            else if (item is MovingTraffic) movingCars.Return(item);
            else if (item is CoinPickup) coins.Return(item);
            else fuel.Return(item);
        }
        public void TickDecor(float dt) { Effects.TickFireworks(dt); bats.Tick(dt); }
    }
}

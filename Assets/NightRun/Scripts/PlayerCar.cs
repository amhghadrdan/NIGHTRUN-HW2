using UnityEngine;
namespace NightRun
{
    public sealed class PlayerCar : MonoBehaviour
    {
        GameManager game;
        Transform body;
        Light neon;
        Light showroomKey, showroomRim;
        ParticleSystem exhaust;
        readonly ExhaustJet[] jets = new ExhaustJet[2];
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        float velocity, lean;
        public int TargetLane { get; private set; } = 1;
        public float PreviousX { get; private set; }
        public bool JetsVisible => jets[0] && jets[1] && jets[0].Visible && jets[1].Visible;
        public void Initialize(GameManager owner)
        {
            game = owner;
            body = VehicleVisual.Build(transform, game.Assets, new Color(.52f, .56f, .60f), true);
            properties = new MaterialPropertyBlock(); renderers = body.GetComponentsInChildren<Renderer>();
            for (int side = -1; side <= 1; side += 2)
            {
                var headlight = Visuals.Light("Projector headlight", transform, new Vector3(side * .65f, .65f, 2.01f), new Color(.85f, .92f, 1), 2.5f, 75);
                headlight.type = LightType.Spot; headlight.spotAngle = 58;
                headlight.transform.localRotation = Quaternion.Euler(9, side * 4, 0);
            }
            neon = Visuals.Light("Subtle speed reactive underglow", transform, new Vector3(0, .16f, 0), new Color(.8f, .83f, .88f), .4f, 3);
            showroomKey = Visuals.Light("Showroom softbox", transform, new Vector3(2.5f, 3, -3), new Color(.9f, .95f, 1), 2.5f, 9);
            showroomRim = Visuals.Light("Showroom rim", transform, new Vector3(-2, 2, 1), new Color(1, .85f, .65f), .6f, 6);
            var pipe = Visuals.Mat("Exhaust chrome", new Color(.22f, .25f, .3f), .9f, .9f);
            for (int side = -1; side <= 1; side += 2)
            {
                if (!game.Assets.localPack)
                {
                var p = Visuals.Shape("Visible exhaust pipe", PrimitiveType.Cylinder, transform, new Vector3(side * .57f, .31f, -2.08f), new Vector3(.21f, .15f, .21f), pipe);
                p.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var inner = Visuals.Shape("Dark pipe opening", PrimitiveType.Cylinder, transform, new Vector3(side * .57f, .31f, -2.235f), new Vector3(.16f, .005f, .16f), game.Assets.rubber);
                inner.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
                var jet = new GameObject("Exhaust jet " + side).AddComponent<ExhaustJet>(); jet.transform.SetParent(transform, false);
                jet.transform.localPosition = new Vector3(side * (game.Assets.localPack ? .43f : .57f), .31f, -2.25f); jet.transform.localRotation = Quaternion.Euler(0, 180, 0); jet.Initialize();
                jets[side < 0 ? 0 : 1] = jet;
            }
            exhaust = EffectsPool.MakeParticle("Continuous exhaust", transform, 100);
            exhaust.transform.localPosition = new Vector3(0, .32f, -2.22f);
            exhaust.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var main = exhaust.main; main.loop = true; main.startLifetime = .35f; main.startSpeed = 4;
            main.startSize = .18f; main.startColor = new Color(.3f, .35f, .45f, .3f);
            var shape = exhaust.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(1.2f, .03f, .01f);
            var emission = exhaust.emission; emission.enabled = true; emission.rateOverTime = 16;
            exhaust.Play();
        }
        public void ResetCar()
        {
            TargetLane = 1; PreviousX = velocity = lean = 0; transform.position = Vector3.zero;
            if (body) body.localRotation = Quaternion.identity;
        }
        public void ChangeLane(int direction) { TargetLane = Mathf.Clamp(TargetLane + direction, 0, 2); }
        public void Tick(float dt)
        {
            PreviousX = transform.position.x;
            float target = (TargetLane - 1) * game.Assets.config.laneWidth;
            float x = Mathf.SmoothDamp(PreviousX, target, ref velocity, game.Assets.config.laneChangeSeconds * .38f, 60, dt);
            transform.position = new Vector3(x, 0, 0);
            lean = Mathf.Lerp(lean, Mathf.Clamp(velocity * .45f, -8, 8), 1 - Mathf.Exp(-dt * 16));
        }
        public void TickVisual(float dt)
        {
            if (!body) return;
            body.localRotation = Quaternion.Euler(0, lean, -lean * .45f);
            body.localPosition = new Vector3(0, .08f + (game.IsRunning ? Mathf.Sin(game.Run.Elapsed * 32) * .009f : 0), 0);
            neon.intensity = Mathf.Lerp(.4f, 1.15f, Mathf.InverseLerp(140, 300, game.Run.Speed));
            bool boost = game.IsRunning && game.Run.Boosting;
            showroomKey.enabled = showroomRim.enabled = game.State == GameState.Menu;
            var main = exhaust.main;
            main.startColor = new Color(.35f, .37f, .4f, .08f);
            main.startSpeed = 1.3f; main.startSize = .085f; main.startLifetime = .3f;
            var particleRenderer = exhaust.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.lengthScale = 1.4f; particleRenderer.velocityScale = .04f;
            var emission = exhaust.emission; emission.rateOverTime = game.IsRunning ? 12 : 4;
            foreach (var jet in jets) jet.SetBoost(boost);
            bool blink = game.Run.Invulnerable && Mathf.FloorToInt(game.Run.Elapsed * 14) % 2 == 0;
            foreach (var r in renderers) r.enabled = !blink;
        }
    }
}

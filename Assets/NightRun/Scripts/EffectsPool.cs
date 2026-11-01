using UnityEngine;
namespace NightRun
{
    public sealed class EffectsPool : MonoBehaviour
    {
        static Material particleMaterial;
        readonly ParticleSystem[] systems = new ParticleSystem[24];
        readonly float[] remaining = new float[24];
        float fireworksTimer;
        float rocketTimer = -1;
        Vector3 rocketTarget;
        public void Initialize()
        {
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i] = MakeParticle("Pooled burst " + i, transform, 180);
                systems[i].gameObject.SetActive(false);
            }
            fireworksTimer = 5;
        }
        public static ParticleSystem MakeParticle(string name, Transform parent, int max)
        {
            if (!particleMaterial)
            {
                particleMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { name = "Soft additive particles" };
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    float a = Mathf.Pow(Mathf.Clamp01(1 - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f), 2);
                    texture.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                texture.Apply(); particleMaterial.mainTexture = texture;
                particleMaterial.SetColor("_TintColor", Color.white * 2);
            }
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = .8f;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var color = ps.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = particleMaterial;
            return ps;
        }
        public void Burst(Vector3 position, Color color, int kind)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                if (remaining[i] > 0) continue;
                var ps = systems[i]; ps.gameObject.SetActive(true); ps.transform.position = position;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.gravityModifier = kind == 2 ? .35f : .8f;
                var trails = ps.trails; trails.enabled = kind == 2;
                if (kind == 2)
                {
                    trails.lifetime = .45f; trails.widthOverTrail = .11f; trails.minVertexDistance = .15f;
                    trails.dieWithParticles = true; trails.inheritParticleColor = true;
                    ps.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;
                }
                main.startLifetime = kind == 2 ? 2.6f : .65f;
                main.startSize = kind == 2 ? .7f : .12f;
                remaining[i] = kind == 2 ? 3 : 1;
                int count = kind == 2 ? 110 : (kind == 1 ? 45 : 22);
                ps.Play();
                for (int p = 0; p < count; p++)
                {
                    Vector3 direction = Random.onUnitSphere;
                    var emit = new ParticleSystem.EmitParams
                    {
                        position = position,
                        velocity = direction * Random.Range(kind == 2 ? 8 : 2, kind == 2 ? 22 : 7) + (kind == 1 ? Vector3.up * 2 : Vector3.zero),
                        startColor = color,
                        startSize = kind == 2 ? Random.Range(.25f, .8f) : Random.Range(.06f, .22f),
                        startLifetime = kind == 2 ? Random.Range(1.4f, 2.8f) : Random.Range(.25f, .8f)
                    };
                    ps.Emit(emit, 1);
                }
                return;
            }
        }
        void Update()
        {
            for (int i = 0; i < systems.Length; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] -= Time.deltaTime;
                if (remaining[i] <= 0) { systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); systems[i].gameObject.SetActive(false); }
            }
        }
        public void TickFireworks(float dt)
        {
            if (rocketTimer >= 0)
            {
                rocketTimer -= dt;
                if (rocketTimer <= 0) { Burst(rocketTarget, Color.Lerp(new Color(1, .75f, .3f), new Color(1, .3f, .18f), Random.value), 2); rocketTimer = -1; }
            }
            fireworksTimer -= dt;
            if (fireworksTimer > 0) return;
            fireworksTimer = Random.Range(5f, 9f);
            rocketTarget = new Vector3(Random.Range(-65f, -25f), Random.Range(36f, 49f), Random.Range(130f, 190f));
            rocketTimer = 1.4f;
            LaunchRocket(rocketTarget);
        }
        void LaunchRocket(Vector3 target)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                if (remaining[i] > 0) continue;
                var ps = systems[i]; ps.gameObject.SetActive(true); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.gravityModifier = 0;
                var trails = ps.trails; trails.enabled = true; trails.lifetime = .3f; trails.widthOverTrail = .35f;
                ps.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;
                Vector3 start = new Vector3(target.x - 8, 2, target.z);
                remaining[i] = 1.8f; ps.Play();
                ps.Emit(new ParticleSystem.EmitParams { position = start, velocity = (target - start) / 1.4f, startLifetime = 1.4f, startColor = new Color(1, .7f, .3f), startSize = .45f }, 1);
                return;
            }
        }
        public void ResetEffects()
        {
            for (int i = 0; i < systems.Length; i++)
            {
                remaining[i] = 0; systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); systems[i].gameObject.SetActive(false);
            }
            fireworksTimer = 4;
            rocketTimer = -1;
        }
    }
}

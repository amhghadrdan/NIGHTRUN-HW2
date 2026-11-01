using UnityEngine;
namespace NightRun
{
    public sealed class NightRunAudio : MonoBehaviour
    {
        GameManager game; AudioSource music, engine, boost, effects;
        float musicLevel, sfxLevel;
        AudioSource Source(AudioClip clip, bool loop)
        {
            var a = gameObject.AddComponent<AudioSource>(); a.clip = clip; a.loop = loop;
            a.playOnAwake = false; a.spatialBlend = 0; a.volume = 0; return a;
        }
        public void Initialize(GameManager owner)
        {
            game = owner;
            music = Source(game.Assets.music, true); music.Play();
            engine = Source(game.Assets.engine, true); engine.Play();
            boost = Source(game.Assets.nitro, true); boost.Play();
            effects = Source(null, false);
            ApplyVolumes();
        }
        public void ApplyVolumes()
        {
            musicLevel = PlayerPrefs.GetFloat("nightrun.music", .6f);
            sfxLevel = PlayerPrefs.GetFloat("nightrun.sfx", .8f);
            if (effects) effects.volume = sfxLevel * .65f;
        }
        public void Tick()
        {
            bool driving = game.IsRunning;
            float dt = Time.unscaledDeltaTime;
            music.volume = Mathf.Lerp(music.volume, musicLevel * (game.State == GameState.Paused ? .15f : .5f), dt * 4);
            engine.volume = Mathf.Lerp(engine.volume, sfxLevel * (driving ? .42f : (game.State == GameState.Menu ? .08f : 0)), dt * 7);
            engine.pitch = Mathf.Lerp(engine.pitch, Mathf.Lerp(.9f, 2.6f, Mathf.InverseLerp(120, 300, game.Run.Speed)), dt * 4);
            boost.volume = Mathf.Lerp(boost.volume, driving && game.Run.Boosting ? sfxLevel * .7f : 0, dt * 12);
        }
        void Play(AudioClip clip, float volume = 1) { if (clip) effects.PlayOneShot(clip, volume); }
        public void Pickup(bool fuel) => Play(fuel ? game.Assets.fuel : game.Assets.coin, .65f);
        public void Impact() => Play(game.Assets.impact);
        public void Click() => Play(game.Assets.click, .55f);
        public void Count() => Play(game.Assets.countdown, .5f);
    }
}

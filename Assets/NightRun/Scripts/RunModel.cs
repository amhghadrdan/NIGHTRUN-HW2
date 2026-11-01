using System;
using UnityEngine;

namespace NightRun
{
    public enum GameState { Menu, Playing, Paused, GameOver }
    public enum EndReason { None, Wrecked, OutOfFuel }

    /// <summary>Frame-independent run rules. No scene, input, physics or persistence dependencies.</summary>
    public sealed class RunModel
    {
        readonly RunConfig config;
        float damageCooldown, sinceBoost;
        bool needsRelease, boostHeld;
        public float Elapsed { get; private set; }
        public float Distance { get; private set; }
        public float Fuel { get; private set; }
        public float Nitro { get; private set; }
        public int Lives { get; private set; }
        public int Coins { get; private set; }
        public bool Boosting { get; private set; }
        public EndReason Reason { get; private set; }
        public float BaseSpeed => Mathf.Lerp(config.startSpeed, config.topSpeed, Mathf.Clamp01(Elapsed / config.rampSeconds));
        public float Speed => BaseSpeed * (Boosting ? config.boostMultiplier : 1);
        public int Score => Mathf.FloorToInt(Distance) + Coins * config.coinScore;
        public bool Invulnerable => damageCooldown > 0;
        public bool Ended => Reason != EndReason.None;

        public RunModel(RunConfig config) { this.config = config; Reset(); }
        public void Reset()
        {
            Elapsed = Distance = damageCooldown = 0;
            Fuel = Nitro = 100; Lives = 3; Coins = 0;
            Boosting = boostHeld = needsRelease = false;
            sinceBoost = config.rechargeDelay;
            Reason = EndReason.None;
        }
        public void SetBoost(bool held)
        {
            boostHeld = held;
            if (!held) needsRelease = false;
        }
        public void Tick(float dt)
        {
            if (Ended || dt <= 0) return;
            // Substeps make exhaustion, recharge delay and fuel consumption stable across frame rates.
            while (dt > .00001f && !Ended)
            {
                float step = Mathf.Min(dt, 1f / 120f); dt -= step;
                damageCooldown = Mathf.Max(0, damageCooldown - step);
                bool canBoost = Boosting ? Nitro > 0 : Nitro >= 25;
                Boosting = boostHeld && !needsRelease && canBoost;
                if (Boosting)
                {
                    Nitro = Mathf.Max(0, Nitro - config.nitroDrain * step);
                    sinceBoost = 0;
                    if (Nitro <= .0001f) { Nitro = 0; needsRelease = true; }
                }
                else
                {
                    sinceBoost += step;
                    if (sinceBoost >= config.rechargeDelay)
                        Nitro = Mathf.Min(100, Nitro + config.nitroRecharge * step);
                }
                Fuel = Mathf.Max(0, Fuel - config.fuelDrain * (Boosting ? config.boostFuelMultiplier : 1) * step);
                Distance += Speed / 3.6f * step;
                Elapsed += step;
                if (Fuel <= 0) Finish(EndReason.OutOfFuel);
            }
        }
        public bool Hit()
        {
            if (Ended || Invulnerable) return false;
            Lives--; damageCooldown = config.invulnerabilitySeconds;
            if (Lives <= 0) Finish(EndReason.Wrecked);
            return true;
        }
        public void CollectCoin() { if (!Ended) Coins++; }
        public void CollectFuel() { if (!Ended) Fuel = Mathf.Min(100, Fuel + config.fuelPickup); }
        void Finish(EndReason reason) { Reason = reason; Boosting = false; }
    }
}

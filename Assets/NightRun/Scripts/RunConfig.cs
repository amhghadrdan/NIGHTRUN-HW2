using UnityEngine;
namespace NightRun
{
    [CreateAssetMenu(menuName = "NIGHTRUN/Balance")]
    public sealed class RunConfig : ScriptableObject
    {
        public float startSpeed = 140, topSpeed = 240, rampSeconds = 100;
        public float laneWidth = 3.6f, laneChangeSeconds = .2f;
        public float fuelDrain = 2, fuelPickup = 35, boostFuelMultiplier = 1.5f;
        public float boostMultiplier = 1.25f, nitroDrain = 25, nitroRecharge = 10, rechargeDelay = 1.5f;
        public float invulnerabilitySeconds = 1.2f;
        public int coinScore = 100;
    }
}

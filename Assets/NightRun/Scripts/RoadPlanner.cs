using System.Collections.Generic;
using UnityEngine;

namespace NightRun
{
    public struct TrafficForecast
    {
        public int Lane;
        public float Z, Speed;
        public TrafficForecast(int lane, float z, float speed) { Lane = lane; Z = z; Speed = speed; }
    }

    /// <summary>Checks a continuous lane route through traffic, including lateral crossing time.</summary>
    public static class RoadPlanner
    {
        const float Step = .1f, CarClearance = 6.2f;
        // Forecasts normal driving, sustained boost, boost release, and late boost activation.
        public static bool IsSafe(IReadOnlyList<TrafficForecast> traffic, int currentLane, float baseSpeed)
        {
            for (int profile = 0; profile < 4; profile++)
                if (!Reachable(traffic, currentLane, baseSpeed, profile)) return false;
            return true;
        }
        static bool Reachable(IReadOnlyList<TrafficForecast> traffic, int lane, float baseSpeed, int profile)
        {
            // States are (lane, time since last move), with a .2s adjacent-lane transition.
            int reachable = 1 << lane;
            float traveled = 0;
            // Moving traffic at 140 km/h can take >12s to reach the player from the spawn line.
            for (int frame = 0; frame < 160; frame++)
            {
                float t = frame * Step;
                float speed = Mathf.Min(240 / 3.6f, baseSpeed + t * .28f);
                bool boost = profile == 1 || (profile == 2 && t < 2) || (profile == 3 && t >= 1.5f && t < 5.5f);
                speed *= boost ? 1.25f : 1;
                traveled += speed * Step;
                int blocked = 0;
                foreach (var car in traffic)
                {
                    float z = car.Z + car.Speed * t - traveled;
                    // Expand by the distance traveled during the lane change and next sample.
                    if (z > -CarClearance && z < CarClearance + speed * .25f)
                        blocked |= 1 << car.Lane;
                }
                reachable &= ~blocked;
                if (reachable == 0) return false;
                // Reserve the first 1.2 seconds for reaction, then allow one adjacent move per .2s.
                if (frame >= 12 && frame % 2 == 0)
                    reachable = (reachable | ((reachable << 1) & 7) | (reachable >> 1)) & ~blocked;
            }
            return true;
        }
    }
}

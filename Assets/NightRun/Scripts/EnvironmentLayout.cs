using System.Collections.Generic;
using UnityEngine;

namespace NightRun
{
    public enum BuildingKind { Garage, Warehouse, BrickLoft, Apartments, Office, Workshop, Yard }

    public struct BuildingPlot
    {
        public BuildingKind Kind;
        public int Side, Style;
        public Vector3 Center, Size;
        public Bounds Footprint => new Bounds(Center, Size + new Vector3(1, 0, 1));
    }

    /// <summary>Buildings stay inside disjoint 30m lots, including the seam between recycled road pieces.</summary>
    public static class EnvironmentLayout
    {
        public const float SegmentLength = 60;
        public static List<BuildingPlot> Create(int segment, int seed = 731)
        {
            var random = new System.Random(seed + segment * 1013);
            var plots = new List<BuildingPlot>(4);
            for (int side = -1; side <= 1; side += 2)
                for (int slot = 0; slot < 2; slot++)
                {
                    var kind = (BuildingKind)((segment * 3 + slot * 2 + (side > 0 ? 1 : 0)) % 7);
                    float width = 9 + (float)random.NextDouble() * 7;
                    float depth = 16 + (float)random.NextDouble() * 6;
                    float height = kind == BuildingKind.Garage ? 5 : kind == BuildingKind.Warehouse ? 7 :
                        kind == BuildingKind.BrickLoft ? 9.7f : kind == BuildingKind.Apartments ? 12.8f :
                        kind == BuildingKind.Office ? 14.4f : kind == BuildingKind.Workshop ? 8 : 2.6f;
                    float setback = 22 + (float)random.NextDouble() * 6;
                    float z = slot * 30 + 15 + ((float)random.NextDouble() * 4 - 2);
                    plots.Add(new BuildingPlot { Kind = kind, Side = side, Style = random.Next(6),
                        Center = new Vector3(side * (setback + width / 2), height / 2, z), Size = new Vector3(width, height, depth) });
                }
            return plots;
        }
        public static bool Overlaps(BuildingPlot a, BuildingPlot b, float segmentOffset = 0)
        {
            var bounds = b.Footprint; bounds.center += Vector3.forward * segmentOffset;
            return a.Footprint.Intersects(bounds);
        }
    }
}

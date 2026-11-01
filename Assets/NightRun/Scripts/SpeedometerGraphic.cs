using UnityEngine;
using UnityEngine.UI;
namespace NightRun
{
    public sealed class SpeedometerGraphic : MaskableGraphic
    {
        float speed;
        public float Speed { set { if (Mathf.Abs(speed - value) > .2f) { speed = value; SetVerticesDirty(); } } }
        void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width / 2;
            int start = vh.currentVertCount;
            vh.AddVert(a - n, c, Vector2.zero); vh.AddVert(a + n, c, Vector2.zero);
            vh.AddVert(b + n, c, Vector2.zero); vh.AddVert(b - n, c, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start + 2, start + 3, start);
        }
        Vector2 Point(float radius, float progress)
        {
            float angle = (210 - 240 * progress) * Mathf.Deg2Rad;
            return new Vector2(180, 155) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); float progress = Mathf.Clamp01(speed / 300);
            for (int i = 0; i < 90; i++)
            {
                float t = i / 90f;
                Line(vh, Point(139, t), Point(139, (i + 1) / 90f), t > .82f ? 5 : 1.5f,
                    t > .82f ? new Color(.85f, .2f, .1f) : new Color(.88f, .9f, .89f, .6f));
                Line(vh, Point(149, t), Point(149, (i + 1) / 90f), .8f, new Color(1, 1, 1, .22f));
            }
            for (int i = 0; i <= 30; i++)
                Line(vh, Point(i % 5 == 0 ? 121 : 128, i / 30f), Point(133, i / 30f), i % 5 == 0 ? 2 : 1, new Color(1, 1, 1, .65f));
            Line(vh, new Vector2(180, 155), Point(128, progress), 3.4f, NightRunUI.Amber);
            Line(vh, new Vector2(174, 155), new Vector2(186, 155), 8, new Color(.5f, .52f, .52f));
        }
    }
}

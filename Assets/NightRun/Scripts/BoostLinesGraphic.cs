using UnityEngine;
using UnityEngine.UI;
namespace NightRun
{
    public sealed class BoostLinesGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect; Vector2 center = r.center;
            for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 9; i++)
            {
                float y = r.yMin + r.height * (.05f + i * .11f);
                Vector2 outer = new Vector2(side < 0 ? r.xMin : r.xMax, y);
                Vector2 delta = outer - center; Vector2 inner = center + delta * .77f;
                int k = vh.currentVertCount;
                vh.AddVert(inner, Color.clear, Vector2.zero);
                vh.AddVert(outer + Vector2.up * 2.5f, new Color(.85f, .88f, .9f, .09f), Vector2.zero);
                vh.AddVert(outer - Vector2.up * 2.5f, new Color(.85f, .88f, .9f, .09f), Vector2.zero);
                vh.AddTriangle(k, k + 1, k + 2);
            }
        }
    }
}

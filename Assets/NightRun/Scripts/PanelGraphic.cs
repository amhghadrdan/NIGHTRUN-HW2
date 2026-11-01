using UnityEngine;
using UnityEngine.UI;
namespace NightRun
{
    public sealed class PanelGraphic : MaskableGraphic
    {
        public Color rightColor = Color.clear;
        public float skew;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect;
            vh.AddVert(new Vector3(r.xMin, r.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), color, Vector2.up);
            vh.AddVert(new Vector3(r.xMax - skew, r.yMax), rightColor, Vector2.one);
            vh.AddVert(new Vector3(r.xMax, r.yMin), rightColor, Vector2.right);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
        }
    }
}

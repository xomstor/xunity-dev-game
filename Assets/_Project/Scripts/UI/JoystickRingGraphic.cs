using UnityEngine;
using UnityEngine.UI;

public class JoystickRingGraphic : Graphic
{
    [Range(0.01f, 0.5f)] public float thickness = 0.08f;
    [Range(16, 128)] public int segments = 64;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float innerRadius = radius * (1f - thickness);
        Vector2 center = rect.center;
        Color32 c = color;

        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector2 outer0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector2 outer1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            Vector2 inner0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * innerRadius;
            Vector2 inner1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * innerRadius;

            int start = vh.currentVertCount;
            vh.AddVert(outer0, c, Vector2.zero);
            vh.AddVert(outer1, c, Vector2.zero);
            vh.AddVert(inner1, c, Vector2.zero);
            vh.AddVert(inner0, c, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}

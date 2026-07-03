using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ScreenEdgeFade : Graphic
{
    [Range(0f, 0.5f)] public float horizontalFade = 0.12f;
    [Range(0f, 0.5f)] public float verticalFade = 0.12f;
    [Range(0f, 1f)] public float centerAlpha = 0f;
    [Range(0f, 1f)] public float edgeAlpha = 0.85f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = GetPixelAdjustedRect();
        float left = r.xMin;
        float right = r.xMax;
        float bottom = r.yMin;
        float top = r.yMax;

        float innerLeft = Mathf.Lerp(left, right, horizontalFade);
        float innerRight = Mathf.Lerp(right, left, horizontalFade);
        float innerBottom = Mathf.Lerp(bottom, top, verticalFade);
        float innerTop = Mathf.Lerp(top, bottom, verticalFade);

        Color32 edge = color;
        edge.a = (byte)(edgeAlpha * 255f);
        Color32 center = color;
        center.a = (byte)(centerAlpha * 255f);

        AddQuad(vh, new Vector2(left, bottom), new Vector2(right, innerBottom), edge, edge, center, center);
        AddQuad(vh, new Vector2(left, innerTop), new Vector2(right, top), center, center, edge, edge);
        AddQuad(vh, new Vector2(left, innerBottom), new Vector2(innerLeft, innerTop), edge, center, center, edge);
        AddQuad(vh, new Vector2(innerRight, innerBottom), new Vector2(right, innerTop), center, edge, edge, center);
    }

    void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 bottomLeft, Color32 bottomRight, Color32 topRight, Color32 topLeft)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(min.x, min.y), bottomLeft, Vector2.zero);
        vh.AddVert(new Vector3(max.x, min.y), bottomRight, Vector2.zero);
        vh.AddVert(new Vector3(max.x, max.y), topRight, Vector2.zero);
        vh.AddVert(new Vector3(min.x, max.y), topLeft, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}

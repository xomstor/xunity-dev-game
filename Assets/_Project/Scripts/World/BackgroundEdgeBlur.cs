using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundEdgeBlur : MonoBehaviour
{
    [Header("Lightweight Edge Smoke")]
    [Tooltip("Smoke opacity. 0 = off, 1 = strong black edge.")]
    [Range(0f, 1f)] public float edgeOpacity = 0.35f;
    [Tooltip("Smoke width in UV space. 0.08-0.18 is usually enough.")]
    [Range(0.001f, 0.5f)] public float edgeWidth = 0.12f;
    [Tooltip("Noise detail amount.")]
    [Range(1f, 80f)] public float noiseScale = 24f;
    [Tooltip("Pixel block amount. Higher = chunkier pixel smoke.")]
    [Range(1f, 64f)] public float pixelSize = 12f;

    [Header("Shader")]
    public Shader edgeSmokeShader;

    private SpriteRenderer spriteRenderer;
    private Material runtimeMaterial;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        CleanupOldChildren();
        ApplyMaterial();
    }

    void OnValidate()
    {
        if (runtimeMaterial != null)
            ApplySettings();
    }

    void ApplyMaterial()
    {
        Shader shader = edgeSmokeShader != null ? edgeSmokeShader : Shader.Find("Custom/BackgroundEdgeBlur");
        if (shader == null)
        {
            Debug.LogError("[BackgroundEdgeBlur] Shader 'Custom/BackgroundEdgeBlur' not found.");
            return;
        }

        runtimeMaterial = new Material(shader);
        spriteRenderer.material = runtimeMaterial;
        ApplySettings();
    }

    void ApplySettings()
    {
        runtimeMaterial.SetFloat("_EdgeOpacity", edgeOpacity);
        runtimeMaterial.SetFloat("_EdgeWidth", edgeWidth);
        runtimeMaterial.SetFloat("_NoiseScale", noiseScale);
        runtimeMaterial.SetFloat("_PixelSize", pixelSize);
    }

    void CleanupOldChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;
            if (child.name.StartsWith("BG_Edge_") || child.name.StartsWith("BG_Smoke_") || child.name == "BlackFlameOverlay" || child.name == "SmokeVignetteOverlay")
                Destroy(child.gameObject);
        }
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}

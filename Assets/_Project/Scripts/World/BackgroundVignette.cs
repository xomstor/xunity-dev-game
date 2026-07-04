using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundVignette : MonoBehaviour
{
    [Header("Black Flame Edges")]
    [Tooltip("Opacity of the black edges. 0 = invisible, 1 = solid black.")]
    [Range(0f, 1f)] public float flameOpacity = 0.6f;
    [Tooltip("How far the black flame reaches from the edges. 0.5 = halfway, 1.0 = full screen.")]
    [Range(0.1f, 1.5f)] public float edgeReach = 0.75f;
    [Tooltip("How soft the edge is. Lower = hard line, higher = soft fade.")]
    [Range(0.5f, 4f)] public float edgeSoftness = 1.5f;
    [Tooltip("Pixel block size — higher = more pixelated look.")]
    [Range(1, 32)] public int pixelBlock = 8;
    [Tooltip("Noise scale for flame shape.")]
    [Range(0.01f, 0.5f)] public float noiseScale = 0.12f;
    [Tooltip("How many 'tongues' of flame stick inward.")]
    [Range(1, 8)] public int flameCount = 4;
    [Tooltip("Seed for the flame pattern. 0 = random each run.")]
    public int seed = 0;

    [Header("Layering")]
    [Tooltip("Sorting order relative to the background sprite.")]
    public int orderOffset = 1;

    private SpriteRenderer bgRenderer;
    private SpriteRenderer overlayRenderer;
    private Texture2D flameTexture;

    void Awake()
    {
        bgRenderer = GetComponent<SpriteRenderer>();
        if (bgRenderer == null) return;

        CreateOverlay();
    }

    void CreateOverlay()
    {
        GameObject overlayObject = new GameObject("BlackFlameOverlay");
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;

        overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        overlayRenderer.sprite = GenerateFlameSprite();
        overlayRenderer.color = Color.white;
        overlayRenderer.sortingLayerID = bgRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = bgRenderer.sortingOrder + orderOffset;
        overlayRenderer.drawMode = SpriteDrawMode.Simple;

        // Place slightly in front of the BG and scale to match it
        Vector2 worldSize = GetSpriteWorldSize();
        overlayObject.transform.localPosition = new Vector3(0, 0, -0.01f);
        overlayObject.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
    }

    Vector2 GetSpriteWorldSize()
    {
        if (bgRenderer == null || bgRenderer.sprite == null) return Vector2.one * 10f;
        return new Vector2(
            bgRenderer.sprite.bounds.size.x * transform.localScale.x,
            bgRenderer.sprite.bounds.size.y * transform.localScale.y);
    }

    Sprite GenerateFlameSprite()
    {
        int size = 128;
        flameTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        flameTexture.wrapMode = TextureWrapMode.Clamp;
        flameTexture.filterMode = FilterMode.Point; // pixelated look

        int currentSeed = seed != 0 ? seed : Random.Range(1, 100000);

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Pixelate coordinates
                int px = (x / pixelBlock) * pixelBlock;
                int py = (y / pixelBlock) * pixelBlock;
                Vector2 p = new Vector2(px, py);
                float dist = Vector2.Distance(center, p);
                float normalizedDist = dist / maxDist;

                // Radial edge mask: BLACK at edges, CLEAR at center
                float reach = Mathf.Max(0.01f, edgeReach);
                float edgeMask = Mathf.Pow(Mathf.Clamp01(normalizedDist / reach), edgeSoftness);

                if (edgeMask <= 0.01f)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                // Flame tongues: combine sine waves with noise for jagged edges
                float angle = Mathf.Atan2(py - center.y, px - center.x);
                float flameShape = 0f;
                for (int i = 0; i < flameCount; i++)
                {
                    float freq = 1f + i * 0.7f;
                    float phase = i * 3.14159f / flameCount;
                    flameShape += Mathf.Sin(angle * freq + phase) * 0.5f + 0.5f;
                }
                flameShape /= flameCount;

                // Fast hash noise (no Perlin lag)
                float noise = FastHash(px * noiseScale + currentSeed, py * noiseScale + currentSeed);
                noise = Mathf.Pow(noise, 1.5f);

                float alpha = edgeMask * flameShape * noise * flameOpacity;
                alpha = Mathf.Clamp01(alpha);

                pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        flameTexture.SetPixels(pixels);
        flameTexture.Apply();

        return Sprite.Create(flameTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
    }

    float FastHash(float x, float y)
    {
        float v = Mathf.Sin(x * 12.9898f + y * 78.233f + 0.5f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    void OnDestroy()
    {
        if (overlayRenderer != null && overlayRenderer.sprite != null)
        {
            if (overlayRenderer.sprite.texture != null)
                Destroy(overlayRenderer.sprite.texture);
            Destroy(overlayRenderer.sprite);
        }
        if (flameTexture != null)
            Destroy(flameTexture);
    }
}

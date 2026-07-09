using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TeleportEffect : MonoBehaviour
{
    public static TeleportEffect Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private int sortOrder = 1000;
    [SerializeField] private int textureSize = 256;
    [SerializeField] private float starDensity = 0.05f;

    [Header("Effect")]
    [SerializeField] private float inDuration = 0.8f;
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private float outDuration = 1.5f;
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float maxSpeed = 14f;
    [SerializeField] private float maxWarp = 7f;
    [SerializeField] private float maxGlow = 2.5f;
    [SerializeField] private float pixelSize = 24f;
    [SerializeField] private Color colorA = new Color(0.0f, 1.0f, 0.95f, 1f);
    [SerializeField] private Color colorB = new Color(1.0f, 0.25f, 0.85f, 1f);

    [Header("Time Freeze")]
    [SerializeField] private float timeScaleMin = 0.01f;
    [SerializeField] private AnimationCurve unfreezeEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private GameObject root;
    private RawImage rawImage;
    private Material material;
    private bool isPlaying;
    private float originalTimeScale = 1f;

    void Awake()
    {
        Initialize(this);
    }

    static void Initialize(TeleportEffect instance)
    {
        if (Instance != null && Instance != instance)
        {
            Destroy(instance.gameObject);
            return;
        }

        Instance = instance;
        DontDestroyOnLoad(instance.gameObject);
        if (Instance.root == null)
            Instance.CreateOverlay();
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("TeleportEffect");
        go.AddComponent<TeleportEffect>();
    }

    void OnDisable()
    {
        if (isPlaying)
        {
            Time.timeScale = originalTimeScale;
            isPlaying = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void CreateOverlay()
    {
        root = new GameObject("TeleportEffectOverlay");
        root.transform.SetParent(transform, false);
        root.SetActive(false);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        canvas.overrideSorting = true;

        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        GameObject imageGO = new GameObject("TunnelImage");
        imageGO.transform.SetParent(root.transform, false);
        RectTransform rt = imageGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rawImage = imageGO.AddComponent<RawImage>();
        rawImage.raycastTarget = true;

        material = new Material(Shader.Find("Custom/TeleportTunnel"));
        material.SetColor("_ColorA", colorA);
        material.SetColor("_ColorB", colorB);
        material.SetFloat("_PixelSize", pixelSize);
        material.SetFloat("_Chromatic", 0.25f);
        material.SetFloat("_Layers", 3f);
        material.SetTexture("_MainTex", CreateStarTexture());

        rawImage.material = material;
    }

    Texture2D CreateStarTexture()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.black;

        System.Random rng = new System.Random(42);
        int count = Mathf.Max(1, Mathf.RoundToInt(textureSize * textureSize * starDensity));
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(0, textureSize);
            int y = rng.Next(0, textureSize);
            pixels[y * textureSize + x] = Color.white;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static void Play(Action onMid = null, Action onComplete = null)
    {
        EnsureInstance();
        Instance?.PlayInternal(onMid, onComplete);
    }

    void PlayInternal(Action onMid, Action onComplete)
    {
        if (isPlaying)
        {
            StopAllCoroutines();
            Time.timeScale = originalTimeScale;
            isPlaying = false;
        }

        if (root == null)
            CreateOverlay();

        originalTimeScale = Time.timeScale;
        StopAllCoroutines();
        StartCoroutine(PlayRoutine(onMid, onComplete));
    }

    IEnumerator PlayRoutine(Action onMid, Action onComplete)
    {
        isPlaying = true;
        root.SetActive(true);

        // --- IN ---
        float elapsed = 0f;
        while (elapsed < inDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / inDuration);
            float curve = intensityCurve.Evaluate(t);
            ApplyEffect(curve);
            Time.timeScale = Mathf.Lerp(originalTimeScale, timeScaleMin, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        ApplyEffect(1f);
        Time.timeScale = timeScaleMin;

        onMid?.Invoke();

        // --- HOLD ---
        yield return new WaitForSecondsRealtime(holdDuration);

        // --- OUT ---
        elapsed = 0f;
        while (elapsed < outDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / outDuration);
            float curve = 1f - intensityCurve.Evaluate(t);
            ApplyEffect(curve);
            Time.timeScale = Mathf.Lerp(timeScaleMin, originalTimeScale, unfreezeEase.Evaluate(t));
            yield return null;
        }

        ApplyEffect(0f);
        root.SetActive(false);
        isPlaying = false;
        Time.timeScale = originalTimeScale;
        onComplete?.Invoke();
    }

    void ApplyEffect(float curve)
    {
        float timeY = Time.unscaledTime;
        material.SetFloat("_TimeY", timeY);
        material.SetFloat("_Speed", maxSpeed * curve);
        material.SetFloat("_Warp", maxWarp * curve);
        material.SetFloat("_Opacity", curve);
        material.SetFloat("_Glow", 1f + (maxGlow - 1f) * curve);
        material.SetColor("_ColorA", colorA);
        material.SetColor("_ColorB", colorB);
    }

    public bool IsPlaying => isPlaying;
}

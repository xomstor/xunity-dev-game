using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TeleportEffect : MonoBehaviour
{
    public static TeleportEffect Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private int sortOrder = 1000;
    [SerializeField] private Color fadeColor = Color.white;

    [Header("Effect")]
    [SerializeField] private float inDuration = 0.8f;
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private float outDuration = 1.5f;
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Time Freeze")]
    [SerializeField] private float timeScaleMin = 0.01f;
    [SerializeField] private AnimationCurve unfreezeEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private GameObject root;
    private Image image;
    private CanvasGroup canvasGroup;
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

        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(root.transform, false);
        RectTransform rt = imageGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        image = imageGO.AddComponent<Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        image.raycastTarget = false;

        canvasGroup = imageGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    // Custom star texture removed; using a simple CanvasGroup color fade instead.

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
        if (canvasGroup != null)
            canvasGroup.alpha = curve;
        if (image != null)
            image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
    }

    public bool IsPlaying => isPlaying;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHintManager : MonoBehaviour
{
    public static TutorialHintManager Instance { get; private set; }

    [Header("Timing")]
    public float showDuration = 4f;
    public float fadeDuration = 0.25f;
    public float minimumGap = 1f;

    private readonly Queue<string> pendingHints = new Queue<string>();
    private readonly HashSet<string> shownHints = new HashSet<string>();
    private Canvas canvas;
    private CanvasGroup group;
    private Text label;
    private Coroutine routine;
    private float nextAllowedTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("TutorialHintManager");
        Instance = go.AddComponent<TutorialHintManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildUI();
        LoadShownHints();
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("TutorialCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2280f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("HintPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.04f, 0.05f, 0.08f, 0.9f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -80f);
        panelRect.sizeDelta = new Vector2(780f, 150f);

        group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject textGO = new GameObject("HintText");
        textGO.transform.SetParent(panel.transform, false);
        label = textGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 34;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 18f);
        textRect.offsetMax = new Vector2(-28f, -18f);
    }

    public static void ShowHint(string hintId, string message)
    {
        Instance?.QueueHint(hintId, message);
    }

    public void QueueHint(string hintId, string message)
    {
        if (string.IsNullOrEmpty(hintId) || string.IsNullOrEmpty(message)) return;
        if (shownHints.Contains(hintId) || pendingHints.Contains(hintId)) return;
        shownHints.Add(hintId);
        PlayerPrefs.SetInt($"EirHold_Tutorial_{hintId}", 1);
        PlayerPrefs.Save();
        pendingHints.Enqueue(message);
        if (routine == null)
            routine = StartCoroutine(ProcessQueue());
    }

    IEnumerator ProcessQueue()
    {
        while (pendingHints.Count > 0)
        {
            if (Time.unscaledTime < nextAllowedTime)
                yield return null;
            if (label == null || group == null) break;

            label.text = pendingHints.Dequeue();
            yield return Fade(1f);
            yield return new WaitForSecondsRealtime(showDuration);
            yield return Fade(0f);
            nextAllowedTime = Time.unscaledTime + minimumGap;
        }
        routine = null;
    }

    IEnumerator Fade(float target)
    {
        float start = group.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = target;
    }

    void LoadShownHints()
    {
        string[] ids = { "movement", "jump", "interact", "skill", "settings" };
        foreach (string id in ids)
            if (PlayerPrefs.GetInt($"EirHold_Tutorial_{id}", 0) == 1)
                shownHints.Add(id);
    }

    public void ResetTutorial()
    {
        string[] ids = { "movement", "jump", "interact", "skill", "settings" };
        foreach (string id in ids)
        {
            shownHints.Remove(id);
            PlayerPrefs.DeleteKey($"EirHold_Tutorial_{id}");
        }
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

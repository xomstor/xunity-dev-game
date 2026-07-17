using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Game Scene")]
    public string gameSceneName = "GameScene";

    [Header("Auto Create (leave empty = create everything)")]
    public Canvas existingCanvas;

    [Header("Font")]
    public TMP_FontAsset menuFont;

    [Header("Colors")]
    public Color panelBgColor = new Color(0.05f, 0.02f, 0.02f, 0.96f);
    public Color panelBorderColor = new Color(0.35f, 0.2f, 0.08f, 1f);
    public Color buttonColor = new Color(0.12f, 0.1f, 0.1f, 0.95f);
    public Color buttonHoverColor = new Color(0.25f, 0.08f, 0.04f, 1f);
    public Color buttonBorderColor = new Color(0.6f, 0.45f, 0.12f, 1f);
    public Color buttonHighlightColor = new Color(0.9f, 0.15f, 0.05f, 1f);
    public Color titleColor = new Color(0.9f, 0.75f, 0.4f, 1f);
    public Color introTitleColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    [Header("Intro Animation")]
    public float introDelay = 1.5f;
    public float introDuration = 0.8f;
    public float introTitleStartSize = 360f;
    public float introTitleEndSize = 72f;
    public float introTitleEndY = 380f;

    [Header("BGM")]
    public string bgmPath = "Custom/MainMenu/BGM";
    public float bgmVolume = 0.7f;
    public float bgmFadeDuration = 1.5f;

    private Canvas canvas;
    private AudioSource bgmSource;
    private AudioClip[] bgmClips;
    private int lastBgmIndex = -1;
    private Coroutine bgmLoopCoroutine;
    private GameObject mainPanel;
    private GameObject loadPanel;
    private GameObject settingsPanel;
    private GameObject creditsPanel;
    private GameObject languagePanel;

    private Button[] slotButtons;
    private TextMeshProUGUI[] slotLabels;
    private Toggle floatingJoystickToggle;
    private Toggle heartStyleToggle;

    private GameObject introTitleGO;
    private TextMeshProUGUI introTitleText;
    private RectTransform introTitleRt;
    private bool introPlaying;
    private bool introSkipRequested;
    private bool isTransitioning;
    private Coroutine panelAnimationCoroutine;

    private static readonly Vector2 MainButtonSize = new Vector2(520, 100);
    private static readonly Vector2 PanelSize = new Vector2(560, 640);
    private static readonly float ButtonSpacing = 20f;

    void Start()
    {
        LocalizationManager.EnsureInstance();
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;

        EnsureCanvas();
        CreateIntroTitle();
        CreateMainPanel();
        CreateLoadPanel();
        CreateSettingsPanel();
        CreateCreditsPanel();

        if (GetComponent<MainMenuSpriteSpawner>() == null)
            gameObject.AddComponent<MainMenuSpriteSpawner>();

        // Ensure BGM folder exists for user-provided tracks
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, "_Project/Resources/Custom/MainMenu/BGM"));

        HideAllPanels();
        if (mainPanel != null) mainPanel.SetActive(false);

        SetupBGM();
        StartCoroutine(IntroTitleAnimation());
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    #region BGM

    void SetupBGM()
    {
        bgmClips = Resources.LoadAll<AudioClip>(bgmPath);
        if (bgmClips.Length == 0) return;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = false;
        bgmSource.volume = 0f;

        bgmLoopCoroutine = StartCoroutine(BgmPlaylistLoop());
    }

    IEnumerator BgmPlaylistLoop()
    {
        while (true)
        {
            int nextIndex = 0;
            if (bgmClips.Length > 1)
            {
                nextIndex = lastBgmIndex;
                while (nextIndex == lastBgmIndex)
                    nextIndex = Random.Range(0, bgmClips.Length);
            }
            lastBgmIndex = nextIndex;
            bgmSource.clip = bgmClips[nextIndex];

            bgmSource.Play();
            yield return StartCoroutine(FadeBGMVolume(bgmVolume, bgmFadeDuration));

            while (bgmSource.isPlaying && bgmSource.time < bgmSource.clip.length - bgmFadeDuration)
                yield return null;

            yield return StartCoroutine(FadeBGMVolume(0f, bgmFadeDuration));
            bgmSource.Stop();
        }
    }

    IEnumerator FadeBGMVolume(float target, float duration)
    {
        if (bgmSource == null) yield break;
        float start = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        bgmSource.volume = target;
    }

    public void StopBGM()
    {
        if (bgmLoopCoroutine != null)
        {
            StopCoroutine(bgmLoopCoroutine);
            bgmLoopCoroutine = null;
        }
        if (bgmSource != null)
            StartCoroutine(FadeBGMVolume(0f, 0.5f));
    }

    #endregion

    void OnLanguageChanged()
    {
        RefreshLoadSlots();
        if (introTitleText != null)
            introTitleText.text = Loc("Menu.Title");
        if (creditsTextComp != null)
        {
            creditsTextComp.text = Loc("Menu.CreditsText");
            if (creditsPanel != null && creditsPanel.activeSelf)
            {
                creditsTextComp.ForceMeshUpdate();
                float textHeight = creditsTextComp.preferredHeight;
                creditsScrollRt.sizeDelta = new Vector2(creditsScrollRt.sizeDelta.x, textHeight);
            }
        }
    }

    #region Canvas

    void EnsureCanvas()
    {
        if (existingCanvas != null)
        {
            canvas = existingCanvas;
            return;
        }

        GameObject canvasGO = new GameObject("MainMenuCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    #endregion

    #region Main Panel

    void CreateMainPanel()
    {
        mainPanel = new GameObject("MainPanel", typeof(RectTransform));
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = mainPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(560, 640);

        VerticalLayoutGroup vlg = mainPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = ButtonSpacing;
        vlg.padding = new RectOffset(20, 20, 30, 30);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateMainButton("PlayButton", "Menu.Play", OnPlayClicked);
        CreateMainButton("LoadButton", "Menu.Load", OnLoadClicked);
        CreateMainButton("SettingsButton", "Menu.Settings", OnSettingsClicked);
        CreateMainButton("CreditsButton", "Menu.Credits", OnCreditsClicked);
        CreateMainButton("ExitButton", "Menu.Exit", OnExitClicked);
    }

    void CreateMainButton(string name, string locKey, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform));
        btnGO.transform.SetParent(mainPanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Button btn = btnGO.AddComponent<Button>();

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = MainButtonSize;

        CreateDiabloButtonVisuals(btnGO, btn);

        btn.onClick.AddListener(onClick);

        GameObject textGO = CreateTextElement(btnGO.transform, "Text", locKey, 48, new Color(0.95f, 0.85f, 0.65f, 1f));
        textGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        AddButtonAnimations(btn, rt);
    }

    void CreateDiabloButtonVisuals(GameObject btnGO, Button btn)
    {
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(btnGO.transform, false);
        bgGO.AddComponent<CanvasRenderer>();
        Image bg = bgGO.AddComponent<Image>();
        bg.color = buttonColor;
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        btn.targetGraphic = bg;

        GameObject borderGO = new GameObject("Border", typeof(RectTransform));
        borderGO.transform.SetParent(btnGO.transform, false);
        borderGO.AddComponent<CanvasRenderer>();
        Image border = borderGO.AddComponent<Image>();
        border.color = buttonBorderColor;
        border.type = Image.Type.Sliced;
        RectTransform borderRt = borderGO.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-4, -4);
        borderRt.offsetMax = new Vector2(4, 4);

        GameObject accentGO = new GameObject("BottomAccent", typeof(RectTransform));
        accentGO.transform.SetParent(btnGO.transform, false);
        accentGO.AddComponent<CanvasRenderer>();
        Image accent = accentGO.AddComponent<Image>();
        accent.color = buttonHighlightColor;
        RectTransform accentRt = accentGO.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0, 0);
        accentRt.anchorMax = new Vector2(1, 0);
        accentRt.pivot = new Vector2(0.5f, 0);
        accentRt.anchoredPosition = new Vector2(0, 4);
        accentRt.sizeDelta = new Vector2(-8, 6);

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        btn.colors = cb;

        btn.transition = Selectable.Transition.None;
    }

    void AddButtonAnimations(Button btn, RectTransform rt)
    {
        var anim = btn.gameObject.AddComponent<DiabloButtonAnim>();
        anim.Init(rt);
    }

    #endregion

    #region Intro Title Animation

    void CreateIntroTitle()
    {
        introTitleGO = new GameObject("IntroTitle", typeof(RectTransform));
        introTitleGO.transform.SetParent(canvas.transform, false);

        introTitleRt = introTitleGO.GetComponent<RectTransform>();
        introTitleRt.anchorMin = new Vector2(0.5f, 0.5f);
        introTitleRt.anchorMax = new Vector2(0.5f, 0.5f);
        introTitleRt.pivot = new Vector2(0.5f, 0.5f);
        introTitleRt.anchoredPosition = Vector2.zero;
        introTitleRt.sizeDelta = new Vector2(2000f, 0f);

        introTitleGO.AddComponent<CanvasRenderer>();
        introTitleText = introTitleGO.AddComponent<TextMeshProUGUI>();
        introTitleText.text = Loc("Menu.Title");
        introTitleText.fontSize = introTitleStartSize;
        introTitleText.alignment = TextAlignmentOptions.Center;
        introTitleText.color = introTitleColor;
        introTitleText.fontStyle = FontStyles.Bold;
        introTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        introTitleText.raycastTarget = false;
        if (menuFont != null) introTitleText.font = menuFont;
    }

    IEnumerator IntroTitleAnimation()
    {
        introPlaying = true;
        introSkipRequested = false;

        float waitTimer = 0f;
        while (waitTimer < introDelay && !introSkipRequested)
        {
            waitTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            RectTransform panelRt = mainPanel.GetComponent<RectTransform>();
            panelRt.anchoredPosition = new Vector2(0, -80f);
            panelRt.localScale = new Vector3(0.9f, 0.9f, 1f);
            CanvasGroup panelGroup = mainPanel.GetComponent<CanvasGroup>();
            if (panelGroup == null) panelGroup = mainPanel.AddComponent<CanvasGroup>();
            panelGroup.alpha = 0f;
        }

        float duration = introDuration;
        float elapsed = 0f;
        float startFontSize = introTitleStartSize;
        float endFontSize = introTitleEndSize;
        Color startColor = introTitleColor;
        Color endColor = titleColor;
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = new Vector2(0, introTitleEndY);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0, 1, t);

            introTitleText.fontSize = Mathf.Lerp(startFontSize, endFontSize, smooth);
            introTitleText.color = Color.Lerp(startColor, endColor, smooth);
            introTitleRt.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);

            if (mainPanel != null)
            {
                RectTransform panelRt = mainPanel.GetComponent<RectTransform>();
                panelRt.localScale = Vector3.Lerp(new Vector3(0.9f, 0.9f, 1f), Vector3.one, smooth);
                CanvasGroup panelGroup = mainPanel.GetComponent<CanvasGroup>();
                if (panelGroup != null)
                    panelGroup.alpha = Mathf.Lerp(0f, 1f, smooth);
            }

            yield return null;
        }

        introTitleText.fontSize = endFontSize;
        introTitleText.color = endColor;
        introTitleRt.anchoredPosition = endPos;

        if (mainPanel != null)
        {
            RectTransform panelRt = mainPanel.GetComponent<RectTransform>();
            panelRt.localScale = Vector3.one;
            CanvasGroup panelGroup = mainPanel.GetComponent<CanvasGroup>();
            if (panelGroup != null)
                panelGroup.alpha = 1f;
        }

        introPlaying = false;
    }

    #endregion

    #region Load Panel

    void CreateLoadPanel()
    {
        loadPanel = CreatePanel("LoadPanel", "SavePanel.Title");

        slotButtons = new Button[SaveManager.SlotCount + 1];
        slotLabels = new TextMeshProUGUI[SaveManager.SlotCount + 1];

        for (int i = 0; i <= SaveManager.SlotCount; i++)
        {
            int slot = (i == 0) ? SaveManager.AutoSaveSlot : i;
            CreateSlotButton(loadPanel.transform, i, slot);
        }

        CreatePanelButton(loadPanel.transform, "Common.Close", new Vector2(300, 60), () => TogglePanel(loadPanel, false));
    }

    void CreateSlotButton(Transform parent, int index, int slot)
    {
        GameObject btnGO = new GameObject($"Slot_{slot}", typeof(RectTransform));
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<CanvasRenderer>();
        Button btn = btnGO.AddComponent<Button>();
        int captured = slot;
        btn.onClick.AddListener(() => OnSlotClicked(captured));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);
        slotButtons[index] = btn;

        CreateDiabloButtonVisuals(btnGO, btn);
        AddButtonAnimations(btn, rt);

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 34;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.95f, 0.85f, 0.65f, 1f);
        text.raycastTarget = false;
        if (menuFont != null) text.font = menuFont;
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        slotLabels[index] = text;
    }

    void OnSlotClicked(int slot)
    {
        if (SaveManager.Instance == null) return;
        if (!SaveManager.Instance.HasSave(slot)) return;
        TogglePanel(loadPanel, false);
        SaveManager.Instance.LoadGame(slot);
    }

    void RefreshLoadSlots()
    {
        if (slotButtons == null || SaveManager.Instance == null) return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = (i == 0) ? SaveManager.AutoSaveSlot : i;
            if (slotButtons[i] != null)
                slotButtons[i].interactable = SaveManager.Instance.HasSave(slot);

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
            {
                string label = SaveManager.Instance.GetSlotLabel(slot);
                if (SaveManager.Instance.HasSave(slot))
                {
                    try
                    {
                        string path = SaveManager.Instance.GetSlotPath(slot);
                        System.IO.FileInfo fi = new System.IO.FileInfo(path);
                        label += $"\n<size=70%>{fi.LastWriteTime:dd.MM.yy HH:mm}</size>";
                    }
                    catch { }
                }
                else
                    label += "\n<size=70%>—</size>";
                slotLabels[i].text = label;
            }
        }
    }

    public void OnLoadClicked()
    {
        TogglePanel(loadPanel, true);
        RefreshLoadSlots();
    }

    #endregion

    #region Settings Panel

    void CreateSettingsPanel()
    {
        settingsPanel = CreatePanel("SettingsPanel", "Settings.Title");

        floatingJoystickToggle = CreateToggle(settingsPanel.transform, "FloatingJoystickToggle", "Settings.FloatingJoystick", OnFloatingJoystickToggle);
        heartStyleToggle = CreateToggle(settingsPanel.transform, "HeartStyleToggle", "Settings.HeartStyle", OnHeartStyleToggle);

        CreatePanelButton(settingsPanel.transform, "Settings.Language", new Vector2(300, 60), OnLanguageButtonClicked);
        CreatePanelButton(settingsPanel.transform, "Common.Close", new Vector2(300, 60), () => TogglePanel(settingsPanel, false));
    }

    Toggle CreateToggle(Transform parent, string name, string locKey, UnityEngine.Events.UnityAction<bool> onValueChanged)
    {
        GameObject toggleGO = new GameObject(name, typeof(RectTransform));
        toggleGO.transform.SetParent(parent, false);
        toggleGO.AddComponent<CanvasRenderer>();
        Image toggleBg = toggleGO.AddComponent<Image>();
        toggleBg.color = new Color(0.12f, 0.1f, 0.1f, 0.95f);
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBg;
        RectTransform toggleRt = toggleGO.GetComponent<RectTransform>();
        toggleRt.sizeDelta = new Vector2(0, 80);

        GameObject checkGO = new GameObject("Checkmark", typeof(RectTransform));
        checkGO.transform.SetParent(toggleGO.transform, false);
        checkGO.AddComponent<CanvasRenderer>();
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = buttonHighlightColor;
        RectTransform checkRt = checkGO.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0, 0.5f);
        checkRt.anchorMax = new Vector2(0, 0.5f);
        checkRt.pivot = new Vector2(0.5f, 0.5f);
        checkRt.anchoredPosition = new Vector2(35, 0);
        checkRt.sizeDelta = new Vector2(44, 44);
        toggle.graphic = checkImg;
        toggle.isOn = false;

        GameObject labelGO = CreateTextElement(toggleGO.transform, "Label", locKey, 38, new Color(0.9f, 0.85f, 0.75f, 1f));
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Left;
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0.5f);
        labelRt.anchorMax = new Vector2(1, 0.5f);
        labelRt.pivot = new Vector2(0, 0.5f);
        labelRt.offsetMin = new Vector2(80, -40);
        labelRt.offsetMax = new Vector2(-10, 40);

        toggle.onValueChanged.AddListener(onValueChanged);
        return toggle;
    }

    public void OnSettingsClicked()
    {
        TogglePanel(settingsPanel, true);
        if (floatingJoystickToggle != null)
            floatingJoystickToggle.isOn = PlayerPrefs.GetInt("FloatingJoystick", 0) == 1;
        if (heartStyleToggle != null)
            heartStyleToggle.isOn = PlayerPrefs.GetInt(HealthBarUI.HealthBarStyleKey, 0) == 1;
    }

    void OnFloatingJoystickToggle(bool value)
    {
        PlayerPrefs.SetInt("FloatingJoystick", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnHeartStyleToggle(bool value)
    {
        PlayerPrefs.SetInt(HealthBarUI.HealthBarStyleKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    #endregion

    #region Language Panel

    void OnLanguageButtonClicked()
    {
        if (languagePanel == null)
            CreateLanguagePanel();
        if (languagePanel != null)
        {
            languagePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
    }

    void CreateLanguagePanel()
    {
        languagePanel = CreatePanel("LanguagePanel", "Language.Title");
        languagePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 480);

        CreateLanguageButton("Language.Russian", "ru");
        CreateLanguageButton("Language.English", "en");
        CreateLanguageButton("Language.Ukrainian", "ua");

        CreatePanelButton(languagePanel.transform, "Common.Close", new Vector2(300, 60), () =>
        {
            languagePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        });

        languagePanel.SetActive(false);
    }

    void CreateLanguageButton(string key, string code)
    {
        GameObject btnGO = new GameObject(key, typeof(RectTransform));
        btnGO.transform.SetParent(languagePanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Button btn = btnGO.AddComponent<Button>();
        string capturedCode = code;
        btn.onClick.AddListener(() => OnLanguageSelected(capturedCode));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        CreateDiabloButtonVisuals(btnGO, btn);
        AddButtonAnimations(btn, rt);

        GameObject textGO = CreateTextElement(btnGO.transform, "Text", key, 38, new Color(0.95f, 0.85f, 0.65f, 1f));
        textGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    void OnLanguageSelected(string code)
    {
        LocalizationManager.Instance?.SetLanguage(code);
        if (languagePanel != null) languagePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    #endregion

    #region Credits Panel

    private RectTransform creditsScrollRt;
    private TextMeshProUGUI creditsTextComp;
    private bool creditsScrolling;
    private float creditsScrollSpeed = 180f;

    void CreateCreditsPanel()
    {
        creditsPanel = new GameObject("CreditsPanel", typeof(RectTransform));
        creditsPanel.transform.SetParent(canvas.transform, false);
        RectTransform prt = creditsPanel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = Vector2.zero;

        Image bg = creditsPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.05f, 0.98f);

        Button closeBtn = creditsPanel.AddComponent<Button>();
        closeBtn.targetGraphic = bg;
        closeBtn.onClick.AddListener(() => TogglePanel(creditsPanel, false));

        GameObject scrollGO = new GameObject("CreditsScroll", typeof(RectTransform));
        scrollGO.transform.SetParent(creditsPanel.transform, false);
        creditsScrollRt = scrollGO.GetComponent<RectTransform>();
        creditsScrollRt.anchorMin = new Vector2(0.5f, 0);
        creditsScrollRt.anchorMax = new Vector2(0.5f, 0);
        creditsScrollRt.pivot = new Vector2(0.5f, 0);
        creditsScrollRt.anchoredPosition = Vector2.zero;
        creditsScrollRt.sizeDelta = new Vector2(700, 0);

        scrollGO.AddComponent<CanvasRenderer>();
        creditsTextComp = scrollGO.AddComponent<TextMeshProUGUI>();
        creditsTextComp.text = Loc("Menu.CreditsText");
        creditsTextComp.fontSize = 100;
        creditsTextComp.alignment = TextAlignmentOptions.Center;
        creditsTextComp.color = new Color(0.9f, 0.85f, 0.75f, 1f);
        creditsTextComp.raycastTarget = false;
        creditsTextComp.textWrappingMode = TextWrappingModes.Normal;
        creditsTextComp.enableAutoSizing = false;
        if (menuFont != null) creditsTextComp.font = menuFont;

        creditsPanel.SetActive(false);
    }

    public void OnCreditsClicked()
    {
        TogglePanel(creditsPanel, true);
        if (creditsTextComp != null)
        {
            creditsTextComp.ForceMeshUpdate();
            float textHeight = creditsTextComp.preferredHeight;
            creditsScrollRt.sizeDelta = new Vector2(creditsScrollRt.sizeDelta.x, textHeight);
            creditsScrollRt.anchoredPosition = new Vector2(0, -textHeight - 100);
        }
        creditsScrolling = true;
    }

    void Update()
    {
        if (creditsScrolling && creditsScrollRt != null && creditsPanel != null && creditsPanel.activeSelf)
        {
            creditsScrollRt.anchoredPosition += new Vector2(0, creditsScrollSpeed * Time.unscaledDeltaTime);
            if (creditsScrollRt.anchoredPosition.y > Screen.height + 200)
                creditsScrolling = false;
        }

        if (introPlaying && !introSkipRequested)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var touchscreen = Touchscreen.current;

            bool anyKey = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
            bool mouseDown = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool touch = touchscreen != null && touchscreen.press.wasPressedThisFrame;

            if (anyKey || mouseDown || touch)
                introSkipRequested = true;
        }
    }

    #endregion

    #region Play / Exit

    public void OnPlayClicked()
    {
        if (isTransitioning) return;
        StartCoroutine(SpiralOutAndPlay());
    }

    IEnumerator SpiralOutAndPlay()
    {
        isTransitioning = true;
        CanvasGroup panelGroup = mainPanel != null ? mainPanel.GetComponent<CanvasGroup>() : null;
        if (panelGroup != null) panelGroup.interactable = false;

        float duration = 0.55f;
        float elapsed = 0f;

        Vector3 titleStartScale = introTitleRt.localScale;
        Vector2 titleStartPos = introTitleRt.anchoredPosition;
        Vector3 titleStartEuler = introTitleRt.localEulerAngles;

        RectTransform panelRt = mainPanel != null ? mainPanel.GetComponent<RectTransform>() : null;
        Vector3 panelStartScale = panelRt != null ? panelRt.localScale : Vector3.one;
        Vector2 panelStartPos = panelRt != null ? panelRt.anchoredPosition : Vector2.zero;
        Vector3 panelStartEuler = panelRt != null ? panelRt.localEulerAngles : Vector3.zero;
        float panelStartAlpha = panelGroup != null ? panelGroup.alpha : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0, 1, t);

            introTitleRt.localScale = Vector3.Lerp(titleStartScale, Vector3.zero, smooth);
            introTitleRt.anchoredPosition = Vector2.Lerp(titleStartPos, Vector2.zero, smooth);
            introTitleRt.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(titleStartEuler.z, titleStartEuler.z - 720f, smooth));

            if (panelRt != null)
            {
                panelRt.localScale = Vector3.Lerp(panelStartScale, Vector3.zero, smooth);
                panelRt.anchoredPosition = Vector2.Lerp(panelStartPos, Vector2.zero, smooth);
                panelRt.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(panelStartEuler.z, panelStartEuler.z - 720f, smooth));
            }
            if (panelGroup != null)
                panelGroup.alpha = Mathf.Lerp(panelStartAlpha, 0f, smooth);

            yield return null;
        }

        introTitleRt.localScale = Vector3.zero;
        introTitleRt.anchoredPosition = Vector2.zero;
        if (panelRt != null)
        {
            panelRt.localScale = Vector3.zero;
            panelRt.anchoredPosition = Vector2.zero;
        }
        if (panelGroup != null) panelGroup.alpha = 0f;

        yield return StartCoroutine(FadeBGMVolume(0f, 0.5f));
        ExecutePlayLogic();
    }

    void ExecutePlayLogic()
    {
        StopBGM();

        if (canvas != null)
            Destroy(canvas.gameObject);

        bool autoSaveIsMainMenu = false;
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave(SaveManager.AutoSaveSlot))
        {
            try
            {
                string json = System.IO.File.ReadAllText(SaveManager.Instance.GetSlotPath(SaveManager.AutoSaveSlot));
                GameData data = JsonUtility.FromJson<GameData>(json);
                autoSaveIsMainMenu = data != null && data.sceneName == SceneManager.GetActiveScene().name;
            }
            catch { }
        }

        if (SaveManager.Instance != null && SaveManager.Instance.HasSave(SaveManager.AutoSaveSlot) && !autoSaveIsMainMenu)
        {
            bool loaded = SaveManager.Instance.LoadGame(SaveManager.AutoSaveSlot);
            if (!loaded)
                LoadGameScene();
        }
        else
        {
            LoadGameScene();
        }
    }

    void LoadGameScene()
    {
        TeleportEffect.Play(
            () => SceneManager.LoadScene(gameSceneName),
            null);
        this.Invoke(nameof(LoadGameSceneDirect), 3f);
    }

    void LoadGameSceneDirect()
    {
        if (SceneManager.GetActiveScene().name != gameSceneName)
            SceneManager.LoadScene(gameSceneName);
    }

    public void OnExitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Helpers

    GameObject CreatePanel(string name, string titleKey)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = PanelSize;

        Image bg = panel.AddComponent<Image>();
        bg.color = panelBgColor;

        GameObject borderGO = new GameObject("Border", typeof(RectTransform));
        borderGO.transform.SetParent(panel.transform, false);
        borderGO.AddComponent<CanvasRenderer>();
        Image border = borderGO.AddComponent<Image>();
        border.color = panelBorderColor;
        RectTransform borderRt = borderGO.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-6, -6);
        borderRt.offsetMax = new Vector2(6, 6);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleGO = CreateTextElement(panel.transform, "Title", titleKey, 48, titleColor);
        TextMeshProUGUI title = titleGO.GetComponent<TextMeshProUGUI>();
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 60);

        return panel;
    }

    void CreatePanelButton(Transform parent, string locKey, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGO = new GameObject(locKey, typeof(RectTransform));
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<CanvasRenderer>();
        Button btn = btnGO.AddComponent<Button>();

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        CreateDiabloButtonVisuals(btnGO, btn);
        AddButtonAnimations(btn, rt);

        btn.onClick.AddListener(onClick);

        GameObject textGO = CreateTextElement(btnGO.transform, "Text", locKey, 40, new Color(0.95f, 0.85f, 0.65f, 1f));
        textGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    GameObject CreateTextElement(Transform parent, string name, string locKey, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = Loc(locKey);
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        if (menuFont != null) text.font = menuFont;
        LocalizedText loc = go.AddComponent<LocalizedText>();
        loc.key = locKey;
        return go;
    }

    void TogglePanel(GameObject panel, bool show)
    {
        if (panel == null) return;
        if (show)
        {
            if (loadPanel != null && loadPanel != panel) loadPanel.SetActive(false);
            if (settingsPanel != null && settingsPanel != panel) settingsPanel.SetActive(false);
            if (creditsPanel != null && creditsPanel != panel) creditsPanel.SetActive(false);
            if (languagePanel != null && languagePanel != panel) languagePanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(false);
        }
        else
        {
            if (mainPanel != null) mainPanel.SetActive(true);
        }
        if (show)
        {
            if (panelAnimationCoroutine != null)
                StopCoroutine(panelAnimationCoroutine);
            panelAnimationCoroutine = StartCoroutine(AnimatePanelIn(panel));
        }
        else
        {
            panel.SetActive(false);
        }
    }

    IEnumerator AnimatePanelIn(GameObject panel)
    {
        panel.SetActive(true);
        RectTransform rect = panel.GetComponent<RectTransform>();
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();
        Vector3 targetScale = rect != null ? rect.localScale : Vector3.one;
        Vector2 targetPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
        if (rect != null)
        {
            rect.localScale = targetScale * 0.96f;
            rect.anchoredPosition = targetPosition + new Vector2(0f, -18f);
        }
        group.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.18f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.18f);
            group.alpha = t;
            if (rect != null)
            {
                rect.localScale = Vector3.Lerp(targetScale * 0.96f, targetScale, t);
                rect.anchoredPosition = Vector2.Lerp(targetPosition + new Vector2(0f, -18f), targetPosition, t);
            }
            yield return null;
        }
        group.alpha = 1f;
        if (rect != null)
        {
            rect.localScale = targetScale;
            rect.anchoredPosition = targetPosition;
        }
        panelAnimationCoroutine = null;
    }

    void HideAllPanels()
    {
        if (loadPanel != null) loadPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    static string Loc(string key)
    {
        if (LocalizationManager.Instance != null)
        {
            string s = LocalizationManager.Instance.Get(key, null);
            if (!string.IsNullOrEmpty(s)) return s;
            Debug.LogWarning($"[MainMenu] Missing localization key: {key}");
        }
        return key;
    }

    #endregion
}

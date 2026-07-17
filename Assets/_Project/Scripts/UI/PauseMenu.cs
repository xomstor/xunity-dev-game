using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI skillPointsText;
    public Transform buttonsParent;

    [Header("References")]
    public PlayerStats playerStats;
    public AutoCombat playerCombat;
    public Inventory playerInventory;
    public EquipmentManager equipmentManager;
    public PlayerSkillsManager skillsManager;

    [Header("Inventory Panel (auto-created if null)")]
    public Transform inventoryContainer;
    public TextMeshProUGUI equippedText;
    public Sprite discardButtonIcon;

    [Header("Pause Buttons Icons")]
    public Sprite saveButtonIcon;
    public Sprite skillTreeButtonIcon;
    public Sprite statisticsButtonIcon;
    public Sprite bestiaryButtonIcon;

    [Header("Resume/Quit Buttons (optional - will find if null)")]
    public Button resumeButton;
    public Button quitButton;

    [Header("Settings Panel (auto-created)")]
    private GameObject settingsPanel;
    private Toggle floatingJoystickToggle;
    private Toggle heartStyleToggle;
    private Slider skillHudSizeSlider;
    private SkillHudLayoutController skillHudLayout;
    private GameObject skillHudEditorOverlay;
    private BlockButton cachedBlockButton;
    private bool blockButtonWasActive;

    [Header("Save Panel (auto-created)")]
    private GameObject savePanel;
    private readonly GameObject[] slotButtons = new GameObject[SaveManager.SlotCount + 1];
    private int selectedSlot = -1;

    [Header("Skill Tree Panel (auto-created)")]
    private GameObject skillTreePanel;
    private Transform skillTreeContent;
    private TextMeshProUGUI skillTreeTalentPointsText;
    private GameObject skillDescriptionPanel;
    private TextMeshProUGUI skillDescriptionText;
    private TextMeshProUGUI skillDescriptionStatsText;
    private PlayerSkillInstance selectedSkill;

    private class SkillTreeRow
    {
        public GameObject root;
        public PlayerSkillInstance skill;
        public Toggle selectedToggle;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public Button plusButton;
        public Image icon;
    }
    private readonly List<SkillTreeRow> skillTreeRows = new List<SkillTreeRow>();

    [Header("Loadout Panel (auto-created)")]
    private GameObject loadoutPanel;
    private int selectedLoadoutSlot = -1;
    private Transform loadoutSkillListContent;
    private readonly List<GameObject> loadoutSkillButtons = new List<GameObject>();
    private readonly Image[] loadoutSlotIcons = new Image[4];
    private readonly Button[] loadoutSlotButtons = new Button[4];

    [Header("Pause Menu Audio")]
    public AudioClip pauseMenuMusic;
    public float pauseMusicVolume = 0.7f;
    public float levelMusicDuckVolume = 0.3f;
    
    private AudioSource pauseMusicSource;
    private AudioSource levelMusicSource;
    private float originalLevelMusicVolume;

    [Header("Bestiary Panel (auto-created)")]
    private GameObject bestiaryPanel;
    private Transform bestiaryContainer;
    private readonly List<GameObject> bestiaryButtons = new List<GameObject>();

    [Header("Statistics Panel (auto-created)")]
    private GameObject statisticsPanel;
    private TextMeshProUGUI statisticsText;

    [Header("Bestiary Description Panel (auto-created)")]
    private GameObject bestiaryDescriptionPanel;
    private TextMeshProUGUI descriptionText;
    private Image descriptionIcon;
    private TextMeshProUGUI descriptionTitle;

    [Header("Language Panel (auto-created)")]
    private GameObject languagePanel;

    private static PauseMenu instance;
    public static PauseMenu Instance => instance;

    private bool isPaused;
    public static bool IsPaused { get; private set; }
    private bool inventoryVisible;
    private readonly string[] statNames = { "HP", "ATK", "DEF", "SPD", "LCK" };
    private readonly List<GameObject> inventoryButtons = new List<GameObject>();
    private int selectedInvIndex;

    string Loc(string key) => LocalizationManager.GetText(key);

    void Awake()
    {
        Debug.Log($"[PauseMenu] Awake: instance==null={instance == null}, instance==this={instance == this}, pausePanel==null={pausePanel == null}");
        // Синглтон: при повторной загрузке GameScene уничтожаем дубли (вместе с дублем Canvas)
        if (instance != null && instance != this)
        {
            Debug.Log("[PauseMenu] Duplicate detected, destroying this gameObject and its Canvas");
            if (pausePanel != null)
            {
                Canvas dupCanvas = pausePanel.GetComponentInParent<Canvas>(true);
                if (dupCanvas != null && PersistentUI.Instance != null && dupCanvas.gameObject != PersistentUI.Instance.gameObject)
                    Destroy(dupCanvas.gameObject);
            }
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;

        // Делаем Canvas с UI постоянным между сценами (меню, HUD, цифры урона)
        if (pausePanel != null)
        {
            Canvas rootCanvas = pausePanel.GetComponentInParent<Canvas>(true);
            if (rootCanvas != null && rootCanvas.GetComponent<PersistentUI>() == null)
                rootCanvas.gameObject.AddComponent<PersistentUI>();
        }

        SceneManager.sceneLoaded += OnSceneLoadedRebind;
        PlayerSpawner.OnPlayerReady += OnPlayerReady;

        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<Inventory>();
        if (skillsManager == null)
            skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        skillHudLayout = FindAnyObjectByType<SkillHudLayoutController>();
        if (skillsManager != null)
            skillsManager.OnChanged += OnSkillTreeChanged;

        RebindPlayerReferences();

        if (buttonsParent == null)
            CreateStatButtons();

        if (inventoryContainer == null)
            CreateInventoryPanel();

        CreateSettingsPanel();
        CreateSettingsButton();

        CreateSavePanel();
        CreateSaveButton();

        CreateSkillTreeButton();
        CreateLoadoutButton();

        CreateBestiaryPanel();
        CreateBestiaryButton();

        FindAndStyleResumeQuitButtons();

        Inventory.OnInventoryChanged += OnInventoryChangedHandler;
    }

    void Start()
    {
        if (skillTreePanel == null)
            CreateSkillTreePanel();
        if (loadoutPanel == null)
            CreateLoadoutPanel();

        AddStartingItems();
        UpdateStatsDisplay();
    }

    void OnDestroy()
    {
        Inventory.OnInventoryChanged -= OnInventoryChangedHandler;
        if (skillsManager != null)
            skillsManager.OnChanged -= OnSkillTreeChanged;
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        PlayerSpawner.OnPlayerReady -= OnPlayerReady;
        if (instance == this)
        {
            instance = null;
            SceneManager.sceneLoaded -= OnSceneLoadedRebind;
        }
    }

    void RebindPlayerReferences()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        playerCombat = playerStats != null ? playerStats.GetComponent<AutoCombat>() : null;
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();

        playerInventory = playerStats != null ? playerStats.GetComponent<Inventory>() : null;
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<Inventory>();

        equipmentManager = playerStats != null ? playerStats.GetComponent<EquipmentManager>() : null;
        if (equipmentManager == null)
            equipmentManager = FindAnyObjectByType<EquipmentManager>();

        if (skillsManager != null)
            skillsManager.OnChanged -= OnSkillTreeChanged;
        skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager != null)
            skillsManager.OnChanged += OnSkillTreeChanged;

        skillHudLayout = FindAnyObjectByType<SkillHudLayoutController>();
        if (skillHudLayout == null)
            EnsureSkillHudExists();
    }

    void EnsureSkillHudExists()
    {
        if (skillHudLayout != null) return;

        // Используем уже размещённые в сцене слоты (SkillSlots), если они есть.
        SkillSlotButton[] existingSlots = FindObjectsByType<SkillSlotButton>(FindObjectsInactive.Include);
        if (existingSlots != null && existingSlots.Length > 0)
        {
            Transform hudParent = existingSlots[0].transform.parent;
            if (hudParent != null)
            {
                skillHudLayout = hudParent.GetComponent<SkillHudLayoutController>();
                if (skillHudLayout == null)
                    skillHudLayout = hudParent.gameObject.AddComponent<SkillHudLayoutController>();
                Debug.Log($"[PauseMenu] EnsureSkillHudExists: using scene skill HUD container {hudParent.name}");
                return;
            }
        }

        // Если в сцене нет готовых слотов — HUD не создаём.
        Debug.Log("[PauseMenu] EnsureSkillHudExists: no skill slot buttons found, skipping HUD creation");
    }

    public void OnPlayerReady()
    {
        RebindPlayerReferences();
        ShowHUD();
        UpdateStatsDisplay();
        RefreshSkillHud();
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
        Debug.Log("[PauseMenu] Created missing EventSystem");
    }

    void OnSceneLoadedRebind(Scene scene, LoadSceneMode mode)
    {
        if (instance != this) return;

        EnsureEventSystem();

        if (scene.name == "MainMenu")
        {
            HideHUD();
            HideSkillButtons();
            StopPauseMusic();
            if (pausePanel != null) pausePanel.SetActive(false);
            // Hide pause button in MainMenu
            PauseButton[] pauseButtons = FindObjectsByType<PauseButton>(FindObjectsInactive.Include);
            foreach (PauseButton pb in pauseButtons)
                if (pb != null) pb.gameObject.SetActive(false);
            return;
        }

        // Перепривязываем ссылки на объекты новой сцены
        RebindPlayerReferences();

        RebuildSkillTreeRows();

        Debug.Log($"[PauseMenu] Scene loaded: {scene.name}, inventory items: {(playerInventory != null ? playerInventory.items.Count : 0)}");

        // Меню закрыто после смены сцены
        isPaused = false;
        IsPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        else
            Debug.LogWarning("[PauseMenu] pausePanel is null in OnSceneLoadedRebind!");
        Time.timeScale = 1f;
        CloseAllSubPanels();

        // Обновляем инвентарь после смены сцены
        RefreshInventory();
        RefreshSkillHud();
    }

    void RefreshSkillHud()
    {
        SkillHudLayoutController[] layouts = FindObjectsByType<SkillHudLayoutController>(FindObjectsInactive.Include);
        Debug.Log($"[PauseMenu.RefreshSkillHud] found {layouts.Length} SkillHudLayoutController instance(s)");
        SkillHudLayoutController chosen = null;

        // Prefer the scene "SkillSlots" container if present
        foreach (SkillHudLayoutController l in layouts)
        {
            if (l == null) continue;
            if (l.gameObject.name == "SkillSlots")
            {
                chosen = l;
                break;
            }
        }

        // Fallback: choose the controller with the most skill slot children
        if (chosen == null && layouts.Length > 0)
        {
            int bestCount = -1;
            foreach (SkillHudLayoutController l in layouts)
            {
                if (l == null) continue;
                int count = l.GetComponentsInChildren<SkillSlotButton>(true).Length;
                if (count > bestCount)
                {
                    bestCount = count;
                    chosen = l;
                }
            }
        }
        if (chosen == null) return;

        // Clean up duplicate HUDs (especially old runtime SkillHudLayout objects)
        foreach (SkillHudLayoutController l in layouts)
        {
            if (l == chosen || l == null) continue;
            Debug.LogWarning($"[PauseMenu.RefreshSkillHud] Destroying duplicate SkillHudLayout: {l.gameObject.name}");
            l.gameObject.SetActive(false);
            Destroy(l.gameObject);
        }

        skillHudLayout = chosen;
        skillHudLayout.gameObject.SetActive(true);
        skillHudLayout.ForceRefresh();

        // Log block button and skill slot positions
        SkillSlotButton[] slots = skillHudLayout.GetComponentsInChildren<SkillSlotButton>(true);
        BlockButton[] blocks = FindObjectsByType<BlockButton>(FindObjectsInactive.Include);
        System.Text.StringBuilder sb = new System.Text.StringBuilder("[PauseMenu.RefreshSkillHud] positions:");
        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform r = slots[i].transform as RectTransform;
            if (r != null) sb.Append($" {slots[i].name}={r.anchoredPosition}");
        }
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] == null) continue;
            RectTransform r = blocks[i].transform as RectTransform;
            if (r != null) sb.Append($" Block_{i}={r.anchoredPosition}");
        }
        Debug.Log(sb.ToString());
    }

    void OnInventoryChangedHandler()
    {
        if (isPaused)
            RefreshInventory();
    }

    void AddStartingItems()
    {
        if (playerInventory == null)
        {
            Debug.LogError("[PauseMenu] playerInventory is NULL in AddStartingItems!");
            return;
        }

        ItemData healerPotion = null;

        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (ItemData item in allItems)
        {
            if (item.itemId == "healer_potion") { healerPotion = item; break; }
        }

        Debug.Log($"[PauseMenu] Resources.FindObjectsOfTypeAll found {allItems.Length} ItemData assets. healerPotion={(healerPotion != null ? healerPotion.itemName : "NULL")}");

#if UNITY_EDITOR
        if (healerPotion == null)
        {
            healerPotion = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Custom/Items/HealerPotion.asset");
            Debug.Log($"[PauseMenu] AssetDatabase.LoadAssetAtPath result: {(healerPotion != null ? healerPotion.itemName : "NULL")}");
        }
#endif

        if (healerPotion != null)
        {
            int current = playerInventory.GetItemCount(healerPotion);
            int missing = Mathf.Max(0, 2 - current);
            Debug.Log($"[PauseMenu] HealerPotion found. Current count={current}, adding {missing}. Inventory items before: {playerInventory.items.Count}");
            if (missing > 0)
                playerInventory.AddItem(healerPotion, missing);
            Debug.Log($"[PauseMenu] Inventory items after: {playerInventory.items.Count}");
        }
        else
        {
            Debug.LogError("[PauseMenu] HealerPotion ItemData not found anywhere!");
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        Debug.Log($"[PauseMenu] TogglePause: this==null={this == null}, pausePanel==null={pausePanel == null}, gameObject.activeInHierarchy={gameObject.activeInHierarchy}");
        if (this == null || pausePanel == null) return;
        isPaused = !isPaused;
        IsPaused = isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
        {
            HideHUD();
            UpdateStatsDisplay();
            RefreshInventory();
            PlayPauseMusic();
        }
        else
        {
            CloseSkillHudEditor();
            ShowHUD();
            CloseAllSubPanels();
            StopPauseMusic();
        }
    }

    public void Resume()
    {
        CloseSkillHudEditor();
        isPaused = false;
        IsPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        ShowHUD();
        CloseAllSubPanels();
        StopPauseMusic();
    }

    void PlayPauseMusic()
    {
        if (pauseMenuMusic == null) return;

        // Находим основную музыку уровня
        if (levelMusicSource == null)
        {
            AudioSource[] allAudioSources = FindObjectsByType<AudioSource>();
            foreach (AudioSource source in allAudioSources)
            {
                if (source.clip != null && source.isPlaying && source.loop)
                {
                    levelMusicSource = source;
                    originalLevelMusicVolume = source.volume;
                    break;
                }
            }
        }

        // Приглушаем основную музыку
        if (levelMusicSource != null)
        {
            levelMusicSource.volume = levelMusicDuckVolume;
        }

        // Создаём AudioSource для музыки паузы если его нет
        if (pauseMusicSource == null)
        {
            GameObject audioGO = new GameObject("PauseMenuAudio");
            audioGO.transform.SetParent(transform);
            pauseMusicSource = audioGO.AddComponent<AudioSource>();
            pauseMusicSource.clip = pauseMenuMusic;
            pauseMusicSource.volume = pauseMusicVolume;
            pauseMusicSource.loop = true;
        }

        pauseMusicSource.Play();
        Debug.Log("[PauseMenu] Pause music started");
    }

    void StopPauseMusic()
    {
        if (pauseMusicSource != null)
        {
            pauseMusicSource.Stop();
            Debug.Log("[PauseMenu] Pause music stopped");
        }

        // Восстанавливаем громкость основной музыки
        if (levelMusicSource != null)
        {
            levelMusicSource.volume = originalLevelMusicVolume;
        }
    }

    void CloseAllSubPanels()
    {
        if (skillHudLayout != null && skillHudLayout.IsEditing)
            skillHudLayout.FinishEdit();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (languagePanel != null) languagePanel.SetActive(false);
        if (savePanel != null) savePanel.SetActive(false);
        if (skillTreePanel != null) skillTreePanel.SetActive(false);
        if (loadoutPanel != null) loadoutPanel.SetActive(false);
        CloseSkillDescription();
        if (bestiaryPanel != null) bestiaryPanel.SetActive(false);
        selectedSlot = -1;
        inventoryVisible = true;
    }

    private readonly List<GameObject> hiddenHUD = new List<GameObject>();

    void HideHUD()
    {
        hiddenHUD.Clear();

        foreach (HealthBarUI hb in FindObjectsByType<HealthBarUI>(FindObjectsInactive.Exclude))
        {
            if (hb != null && hb.gameObject.activeSelf)
            {
                hiddenHUD.Add(hb.gameObject);
                hb.gameObject.SetActive(false);
            }
        }

        foreach (ThrowSkillButton btn in FindObjectsByType<ThrowSkillButton>(FindObjectsInactive.Exclude))
        {
            if (btn != null && btn.gameObject.activeSelf)
            {
                hiddenHUD.Add(btn.gameObject);
                btn.gameObject.SetActive(false);
            }
        }

        SkillHudLayoutController skillHud = FindAnyObjectByType<SkillHudLayoutController>();
        if (skillHud != null && skillHud.gameObject.activeSelf)
        {
            skillHudLayout = skillHud;
            hiddenHUD.Add(skillHud.gameObject);
            skillHud.gameObject.SetActive(false);
        }

        BlockButton blockBtn = FindAnyObjectByType<BlockButton>();
        if (blockBtn != null && blockBtn.gameObject.activeSelf)
        {
            hiddenHUD.Add(blockBtn.gameObject);
            blockBtn.gameObject.SetActive(false);
        }
    }

    void ShowHUD()
    {
        foreach (GameObject go in hiddenHUD)
        {
            if (go != null)
                go.SetActive(true);
        }
        hiddenHUD.Clear();

        // Re-enable pause button
        PauseButton[] pauseButtons = FindObjectsByType<PauseButton>(FindObjectsInactive.Include);
        foreach (PauseButton pb in pauseButtons)
            if (pb != null) pb.gameObject.SetActive(true);

        foreach (HealthBarUI hb in FindObjectsByType<HealthBarUI>(FindObjectsInactive.Include))
            if (hb != null) hb.gameObject.SetActive(true);

        foreach (ThrowSkillButton btn in FindObjectsByType<ThrowSkillButton>(FindObjectsInactive.Include))
            if (btn != null) btn.gameObject.SetActive(true);

        foreach (SkillHudLayoutController hud in FindObjectsByType<SkillHudLayoutController>(FindObjectsInactive.Include))
        {
            if (hud == null) continue;
            if (hud.gameObject.scene.IsValid() && !hud.gameObject.scene.isLoaded) continue;
            hud.gameObject.SetActive(true);
        }

        foreach (BlockButton block in FindObjectsByType<BlockButton>(FindObjectsInactive.Include))
            if (block != null) block.gameObject.SetActive(true);
    }

    void HideSkillButtons()
    {
        if (skillHudLayout == null)
        {
            SkillHudLayoutController[] layouts = FindObjectsByType<SkillHudLayoutController>(FindObjectsInactive.Include);
            skillHudLayout = layouts.Length > 0 ? layouts[0] : null;
        }
        if (skillHudLayout != null && skillHudLayout.gameObject.activeSelf)
        {
            skillHudLayout.gameObject.SetActive(false);
        }
    }

    void ShowSkillButtons()
    {
        RefreshSkillHud();
        if (skillHudLayout == null)
        {
            SkillHudLayoutController[] layouts = FindObjectsByType<SkillHudLayoutController>(FindObjectsInactive.Include);
            skillHudLayout = layouts.Length > 0 ? layouts[0] : null;
        }
        if (skillHudLayout != null)
        {
            skillHudLayout.gameObject.SetActive(true);
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        CloseAllSubPanels();
        IsPaused = false;
        isPaused = false;
        StopPauseMusic();

        // Hide the pause button so it's not clickable in MainMenu
        PauseButton[] pauseButtons = FindObjectsByType<PauseButton>(FindObjectsInactive.Include);
        foreach (PauseButton pb in pauseButtons)
            if (pb != null) pb.gameObject.SetActive(false);

        TeleportEffect.Play(
            () =>
            {
                SceneManager.LoadScene("MainMenu");
            },
            null);
    }

    public void OpenSettings()
    {
        HideInventory();
        HideSkillButtons();
        if (skillHudLayout == null)
        {
            SkillHudLayoutController[] layouts = FindObjectsByType<SkillHudLayoutController>(FindObjectsInactive.Include);
            skillHudLayout = layouts.Length > 0 ? layouts[0] : null;
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (skillHudSizeSlider != null && skillHudLayout != null)
                skillHudSizeSlider.SetValueWithoutNotify(skillHudLayout.ButtonSize);
            if (floatingJoystickToggle != null)
                floatingJoystickToggle.isOn = PlayerPrefs.GetInt("FloatingJoystick", 0) == 1;
            if (heartStyleToggle != null)
                heartStyleToggle.isOn = PlayerPrefs.GetInt(HealthBarUI.HealthBarStyleKey, 0) == 1;
        }
    }

    public void CloseSettings()
    {
        CloseSkillHudEditor();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        ShowInventory();
    }

    public void OpenLanguagePanel()
    {
        HideInventory();
        if (languagePanel == null)
            CreateLanguagePanel();
        if (languagePanel != null)
            languagePanel.SetActive(true);
    }

    public void CloseLanguagePanel()
    {
        if (languagePanel != null)
            languagePanel.SetActive(false);
        ShowInventory();
    }

    void CreateLanguagePanel()
    {
        if (pausePanel == null) return;

        languagePanel = new GameObject("LanguagePanel");
        languagePanel.transform.SetParent(pausePanel.transform, false);
        RectTransform rt = languagePanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(420, 480);

        Image bg = languagePanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = languagePanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(languagePanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = Loc("Language.Title");
        titleGO.AddComponent<LocalizedText>().key = "Language.Title";
        title.fontSize = 44;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 60);

        CreateLanguageButton("Language.Russian", "ru");
        CreateLanguageButton("Language.English", "en");
        CreateLanguageButton("Language.Ukrainian", "ua");

        CreateSmallButton(languagePanel.transform, "Common.Close", new Vector2(300, 60), CloseLanguagePanel, true);

        languagePanel.SetActive(false);
    }

    void CreateLanguageButton(string key, string code)
    {
        GameObject btnGO = new GameObject(key);
        btnGO.transform.SetParent(languagePanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.65f, 0.95f);
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        string capturedCode = code;
        btn.onClick.AddListener(() => OnLanguageSelected(capturedCode));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 70);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = Loc(key);
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        LocalizedText loc = textGO.AddComponent<LocalizedText>();
        loc.key = key;
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    void OnLanguageSelected(string code)
    {
        LocalizationManager.Instance?.SetLanguage(code);
        CloseLanguagePanel();
    }

    void OnLanguageChanged()
    {
        RefreshActivePanels();
    }

    void RefreshActivePanels()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            if (floatingJoystickToggle != null)
                floatingJoystickToggle.isOn = PlayerPrefs.GetInt("FloatingJoystick", 0) == 1;
            if (heartStyleToggle != null)
                heartStyleToggle.isOn = PlayerPrefs.GetInt(HealthBarUI.HealthBarStyleKey, 0) == 1;
        }
        if (savePanel != null && savePanel.activeSelf)
            RefreshSlotButtons();
        if (skillTreePanel != null && skillTreePanel.activeSelf)
            RefreshSkillTree();
        if (loadoutPanel != null && loadoutPanel.activeSelf)
            RefreshLoadoutSlots();
        if (bestiaryPanel != null && bestiaryPanel.activeSelf)
            RefreshBestiary();
        if (statisticsPanel != null && statisticsPanel.activeSelf)
            RefreshStatistics();
        if (inventoryVisible)
            RefreshInventory();
        UpdateStatsDisplay();
    }

    public void OpenSavePanel()
    {
        HideInventory();
        HideSkillButtons();
        if (savePanel != null)
        {
            savePanel.SetActive(true);
            RefreshSlotButtons();
        }
    }

    public void CloseSavePanel()
    {
        if (savePanel != null)
            savePanel.SetActive(false);
        selectedSlot = -1;
        ShowInventory();
    }

    public void SelectSlot(int slot)
    {
        selectedSlot = slot;
        RefreshSlotButtons();
    }

    public void SaveSelectedSlot()
    {
        if (selectedSlot < 0) return;
        if (selectedSlot == 0) return;
        SaveManager.Instance?.SaveGame(selectedSlot);
        RefreshSlotButtons();
    }

    public void LoadSelectedSlot()
    {
        if (selectedSlot < 0) return;
        int realSlot = (selectedSlot == 0) ? SaveManager.AutoSaveSlot : selectedSlot;
        SaveManager.Instance?.LoadGame(realSlot);
        CloseSavePanel();
    }

    public void DeleteSelectedSlot()
    {
        if (selectedSlot < 0) return;
        if (selectedSlot == 0) return;
        SaveManager.Instance?.DeleteSlot(selectedSlot);
        RefreshSlotButtons();
    }

    public void OpenSkillTree()
    {
        HideInventory();
        HideSkillButtons();
        if (skillTreePanel != null)
        {
            skillTreePanel.SetActive(true);
            CloseSkillDescription();
            RefreshSkillTree();
        }
    }

    public void CloseSkillTree()
    {
        if (skillTreePanel != null)
            skillTreePanel.SetActive(false);
        CloseSkillDescription();
        ShowInventory();
    }

    public void OnHeartStyleToggle(bool value)
    {
        PlayerPrefs.SetInt(HealthBarUI.HealthBarStyleKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnCameraOffsetXChanged(float value)
    {
        PlayerPrefs.SetFloat("CameraOffsetX", value);
        PlayerPrefs.Save();
        CameraOffsetApplier.ApplyOffset();
    }

    public void OnCameraOffsetYChanged(float value)
    {
        PlayerPrefs.SetFloat("CameraOffsetY", value);
        PlayerPrefs.Save();
        CameraOffsetApplier.ApplyOffset();
    }

    public void OnFloatingJoystickToggle(bool value)
    {
        Debug.Log($"[PauseMenu] OnFloatingJoystickToggle called with value={value}");
        VirtualJoystick joystick = null;
        VirtualJoystick[] joysticks = FindObjectsByType<VirtualJoystick>();
        foreach (VirtualJoystick candidate in joysticks)
        {
            if (candidate != null && candidate.IsUsable)
            {
                joystick = candidate;
                break;
            }
        }
        Debug.Log($"[PauseMenu] VirtualJoystick found: {joystick != null}");
        if (joystick != null)
            joystick.SetFloating(value);
        else
        {
            PlayerPrefs.SetInt("FloatingJoystick", value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.LogWarning("[PauseMenu] VirtualJoystick not found in scene! Saved to PlayerPrefs only.");
        }
    }

    void CreateSettingsPanel()
    {
        if (pausePanel == null) return;

        settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(pausePanel.transform, false);
        RectTransform srt = settingsPanel.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(520, 620);

        Image bg = settingsPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = settingsPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(settingsPanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = Loc("Settings.Title");
        titleGO.AddComponent<LocalizedText>().key = "Settings.Title";
        title.fontSize = 48;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 60);

        GameObject toggleGO = new GameObject("FloatingJoystickToggle");
        toggleGO.transform.SetParent(settingsPanel.transform, false);
        toggleGO.AddComponent<CanvasRenderer>();
        Image toggleBg = toggleGO.AddComponent<Image>();
        toggleBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBg;
        RectTransform toggleRt = toggleGO.GetComponent<RectTransform>();
        toggleRt.sizeDelta = new Vector2(0, 70);

        GameObject checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(toggleGO.transform, false);
        checkGO.AddComponent<CanvasRenderer>();
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = new Color(0f, 1f, 0.5f, 1f);
        RectTransform checkRt = checkGO.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0, 0.5f);
        checkRt.anchorMax = new Vector2(0, 0.5f);
        checkRt.pivot = new Vector2(0.5f, 0.5f);
        checkRt.anchoredPosition = new Vector2(35, 0);
        checkRt.sizeDelta = new Vector2(40, 40);
        toggle.graphic = checkImg;
        toggle.isOn = false;
        floatingJoystickToggle = toggle;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(toggleGO.transform, false);
        labelGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = Loc("Settings.FloatingJoystick");
        labelGO.AddComponent<LocalizedText>().key = "Settings.FloatingJoystick";
        label.fontSize = 36;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0.5f);
        labelRt.anchorMax = new Vector2(1, 0.5f);
        labelRt.pivot = new Vector2(0, 0.5f);
        labelRt.offsetMin = new Vector2(80, -35);
        labelRt.offsetMax = new Vector2(-10, 35);

        toggle.onValueChanged.AddListener(OnFloatingJoystickToggle);

        GameObject heartToggleGO = new GameObject("HeartStyleToggle");
        heartToggleGO.transform.SetParent(settingsPanel.transform, false);
        heartToggleGO.AddComponent<CanvasRenderer>();
        Image heartToggleBg = heartToggleGO.AddComponent<Image>();
        heartToggleBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Toggle heartToggle = heartToggleGO.AddComponent<Toggle>();
        heartToggle.targetGraphic = heartToggleBg;
        RectTransform heartToggleRt = heartToggleGO.GetComponent<RectTransform>();
        heartToggleRt.sizeDelta = new Vector2(0, 70);

        GameObject heartCheckGO = new GameObject("Checkmark");
        heartCheckGO.transform.SetParent(heartToggleGO.transform, false);
        heartCheckGO.AddComponent<CanvasRenderer>();
        Image heartCheckImg = heartCheckGO.AddComponent<Image>();
        heartCheckImg.color = new Color(0f, 1f, 0.5f, 1f);
        RectTransform heartCheckRt = heartCheckGO.GetComponent<RectTransform>();
        heartCheckRt.anchorMin = new Vector2(0, 0.5f);
        heartCheckRt.anchorMax = new Vector2(0, 0.5f);
        heartCheckRt.pivot = new Vector2(0.5f, 0.5f);
        heartCheckRt.anchoredPosition = new Vector2(35, 0);
        heartCheckRt.sizeDelta = new Vector2(40, 40);
        heartToggle.graphic = heartCheckImg;
        heartToggle.isOn = false;
        heartStyleToggle = heartToggle;

        GameObject heartLabelGO = new GameObject("Label");
        heartLabelGO.transform.SetParent(heartToggleGO.transform, false);
        heartLabelGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI heartLabel = heartLabelGO.AddComponent<TextMeshProUGUI>();
        heartLabel.text = Loc("Settings.HeartStyle");
        heartLabelGO.AddComponent<LocalizedText>().key = "Settings.HeartStyle";
        heartLabel.fontSize = 36;
        heartLabel.alignment = TextAlignmentOptions.Left;
        heartLabel.color = Color.white;
        RectTransform heartLabelRt = heartLabelGO.GetComponent<RectTransform>();
        heartLabelRt.anchorMin = new Vector2(0, 0.5f);
        heartLabelRt.anchorMax = new Vector2(1, 0.5f);
        heartLabelRt.pivot = new Vector2(0, 0.5f);
        heartLabelRt.offsetMin = new Vector2(80, -35);
        heartLabelRt.offsetMax = new Vector2(-10, 35);

        heartToggle.onValueChanged.AddListener(OnHeartStyleToggle);

        skillHudSizeSlider = CreateSkillHudSizeSlider(settingsPanel.transform);
        CreateSmallButton(settingsPanel.transform, "Settings.ChangeLayout", new Vector2(300, 60), ToggleSkillHudEditor, true);
        CreateSmallButton(settingsPanel.transform, "Settings.ResetSkillLayout", new Vector2(300, 60), ResetSkillHudLayout, true);

        // Ползунки смещения камеры отключены, но скрипты остаются работающими
        /*
        GameObject camOffsetXGO = new GameObject("CameraOffsetXSlider");
        camOffsetXGO.transform.SetParent(settingsPanel.transform, false);
        camOffsetXGO.AddComponent<CanvasRenderer>();
        Image camOffsetXBg = camOffsetXGO.AddComponent<Image>();
        camOffsetXBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Slider camOffsetXSlider = camOffsetXGO.AddComponent<Slider>();
        camOffsetXSlider.minValue = -5f;
        camOffsetXSlider.maxValue = 5f;
        camOffsetXSlider.wholeNumbers = false;
        camOffsetXSlider.value = PlayerPrefs.GetFloat("CameraOffsetX", 0f);
        RectTransform camOffsetXRt = camOffsetXGO.GetComponent<RectTransform>();
        camOffsetXRt.sizeDelta = new Vector2(0, 70);

        GameObject camOffsetXLabelGO = new GameObject("Label");
        camOffsetXLabelGO.transform.SetParent(camOffsetXGO.transform, false);
        camOffsetXLabelGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI camOffsetXLabel = camOffsetXLabelGO.AddComponent<TextMeshProUGUI>();
        camOffsetXLabel.text = "Смещение камеры X";
        camOffsetXLabel.fontSize = 28;
        camOffsetXLabel.alignment = TextAlignmentOptions.Left;
        camOffsetXLabel.color = Color.white;
        RectTransform camOffsetXLabelRt = camOffsetXLabelGO.GetComponent<RectTransform>();
        camOffsetXLabelRt.anchorMin = new Vector2(0, 0.5f);
        camOffsetXLabelRt.anchorMax = new Vector2(1, 0.5f);
        camOffsetXLabelRt.pivot = new Vector2(0, 0.5f);
        camOffsetXLabelRt.offsetMin = new Vector2(10, -35);
        camOffsetXLabelRt.offsetMax = new Vector2(-10, 35);

        AddSliderVisuals(camOffsetXSlider);
        camOffsetXSlider.onValueChanged.AddListener(OnCameraOffsetXChanged);

        GameObject camOffsetYGO = new GameObject("CameraOffsetYSlider");
        camOffsetYGO.transform.SetParent(settingsPanel.transform, false);
        camOffsetYGO.AddComponent<CanvasRenderer>();
        Image camOffsetYBg = camOffsetYGO.AddComponent<Image>();
        camOffsetYBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Slider camOffsetYSlider = camOffsetYGO.AddComponent<Slider>();
        camOffsetYSlider.minValue = -5f;
        camOffsetYSlider.maxValue = 5f;
        camOffsetYSlider.wholeNumbers = false;
        camOffsetYSlider.value = PlayerPrefs.GetFloat("CameraOffsetY", 0f);
        RectTransform camOffsetYRt = camOffsetYGO.GetComponent<RectTransform>();
        camOffsetYRt.sizeDelta = new Vector2(0, 70);

        GameObject camOffsetYLabelGO = new GameObject("Label");
        camOffsetYLabelGO.transform.SetParent(camOffsetYGO.transform, false);
        camOffsetYLabelGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI camOffsetYLabel = camOffsetYLabelGO.AddComponent<TextMeshProUGUI>();
        camOffsetYLabel.text = "Смещение камеры Y";
        camOffsetYLabel.fontSize = 28;
        camOffsetYLabel.alignment = TextAlignmentOptions.Left;
        camOffsetYLabel.color = Color.white;
        RectTransform camOffsetYLabelRt = camOffsetYLabelGO.GetComponent<RectTransform>();
        camOffsetYLabelRt.anchorMin = new Vector2(0, 0.5f);
        camOffsetYLabelRt.anchorMax = new Vector2(1, 0.5f);
        camOffsetYLabelRt.pivot = new Vector2(0, 0.5f);
        camOffsetYLabelRt.offsetMin = new Vector2(10, -35);
        camOffsetYLabelRt.offsetMax = new Vector2(-10, 35);

        AddSliderVisuals(camOffsetYSlider);
        camOffsetYSlider.onValueChanged.AddListener(OnCameraOffsetYChanged);
        */

        CreateSmallButton(settingsPanel.transform, "Settings.Language", new Vector2(300, 60), OpenLanguagePanel, true);
        CreateSmallButton(settingsPanel.transform, "Common.Close", new Vector2(300, 60), CloseSettings, true);

        settingsPanel.SetActive(false);
    }

    Slider CreateSkillHudSizeSlider(Transform parent)
    {
        GameObject sliderGO = new GameObject("SkillHudSizeSlider");
        sliderGO.transform.SetParent(parent, false);
        sliderGO.AddComponent<CanvasRenderer>();
        Image background = sliderGO.AddComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 72f;
        slider.maxValue = 180f;
        slider.wholeNumbers = true;
        slider.value = skillHudLayout != null ? skillHudLayout.ButtonSize : 110f;
        slider.onValueChanged.AddListener(OnSkillHudSizeChanged);
        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 70f);
        AddSliderVisuals(slider);

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(sliderGO.transform, false);
        labelGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "Размер кнопок навыков";
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(1f, 0.5f);
        labelRect.offsetMin = new Vector2(10f, -28f);
        labelRect.offsetMax = new Vector2(-10f, 28f);
        return slider;
    }

    void OnSkillHudSizeChanged(float value)
    {
        skillHudLayout?.ApplySize(value);
    }

    public void OpenSkillHudEditor()
    {
        RefreshSkillHud();

        if (skillHudLayout == null)
        {
            SkillHudLayoutController[] layouts = FindObjectsByType<SkillHudLayoutController>(FindObjectsInactive.Include);
            skillHudLayout = layouts.Length > 0 ? layouts[0] : null;
        }
        Debug.Log($"[PauseMenu] OpenSkillHudEditor: skillHudLayout={skillHudLayout != null}");
        if (skillHudLayout == null)
        {
            EnsureSkillHudExists();
            if (skillHudLayout != null)
                skillHudLayout.gameObject.SetActive(true);
        }
        if (skillHudLayout == null) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        skillHudLayout.gameObject.SetActive(true);
        skillHudLayout.transform.SetAsLastSibling();
        BlockButton[] blockButtons = FindObjectsByType<BlockButton>(FindObjectsInactive.Include);
        cachedBlockButton = blockButtons.Length > 0 ? blockButtons[0] : null;
        if (cachedBlockButton != null)
        {
            blockButtonWasActive = cachedBlockButton.gameObject.activeSelf;
            cachedBlockButton.gameObject.SetActive(true);
            cachedBlockButton.transform.SetAsLastSibling();
        }
        skillHudLayout.BeginEdit();
        Debug.Log($"[PauseMenu] OpenSkillHudEditor: edit mode active={skillHudLayout.IsEditing}");
        CreateSkillHudEditorOverlay();
    }

    public void ToggleSkillHudEditor()
    {
        if (skillHudLayout != null && skillHudLayout.IsEditing)
        {
            CloseSkillHudEditor();
            return;
        }
        OpenSkillHudEditor();
    }

    void CreateSkillHudEditorOverlay()
    {
        if (skillHudEditorOverlay != null || pausePanel == null) return;
        Canvas canvas = pausePanel.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        skillHudEditorOverlay = new GameObject("SkillHudEditorOverlay");
        skillHudEditorOverlay.transform.SetParent(canvas.transform, false);
        RectTransform overlayRect = skillHudEditorOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(skillHudEditorOverlay.transform, false);
        closeGO.AddComponent<CanvasRenderer>();
        Image closeImage = closeGO.AddComponent<Image>();
        closeImage.color = new Color(0.75f, 0.12f, 0.12f, 0.95f);
        Button closeButton = closeGO.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(CloseSkillHudEditor);
        RectTransform closeRect = closeGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -24f);
        closeRect.sizeDelta = new Vector2(84f, 84f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "×";
        text.fontSize = 64f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        skillHudEditorOverlay.transform.SetAsLastSibling();
    }

    public void CloseSkillHudEditor()
    {
        bool wasEditing = skillHudLayout != null && skillHudLayout.IsEditing;
        if (wasEditing) skillHudLayout.FinishEdit();
        if (skillHudLayout != null)
            skillHudLayout.gameObject.SetActive(false);
        if (cachedBlockButton != null)
        {
            cachedBlockButton.gameObject.SetActive(blockButtonWasActive);
            cachedBlockButton = null;
        }
        if (skillHudEditorOverlay != null)
        {
            Destroy(skillHudEditorOverlay);
            skillHudEditorOverlay = null;
        }
        if (wasEditing && isPaused && pausePanel != null)
        {
            pausePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }
    }

    public void ResetSkillHudLayout()
    {
        skillHudLayout?.ResetLayout();
        if (skillHudSizeSlider != null)
            skillHudSizeSlider.value = skillHudLayout != null ? skillHudLayout.ButtonSize : 110f;
    }

    void AddSliderVisuals(Slider slider)
    {
        GameObject fillAreaGO = new GameObject("FillArea");
        fillAreaGO.transform.SetParent(slider.transform, false);
        RectTransform fillAreaRt = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0);
        fillAreaRt.anchorMax = new Vector2(1, 0);
        fillAreaRt.pivot = new Vector2(0.5f, 0);
        fillAreaRt.anchoredPosition = new Vector2(0, 8);
        fillAreaRt.sizeDelta = new Vector2(-30, 12);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        fillGO.AddComponent<CanvasRenderer>();
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.85f, 1f, 1f);
        RectTransform fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        GameObject handleAreaGO = new GameObject("HandleArea");
        handleAreaGO.transform.SetParent(slider.transform, false);
        RectTransform handleAreaRt = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0, 0);
        handleAreaRt.anchorMax = new Vector2(1, 0);
        handleAreaRt.pivot = new Vector2(0.5f, 0);
        handleAreaRt.anchoredPosition = new Vector2(0, 4);
        handleAreaRt.sizeDelta = new Vector2(-30, 20);

        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        handleGO.AddComponent<CanvasRenderer>();
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRt = handleGO.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(24, 24);

        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
    }

    void CreateSettingsButton()
    {
        if (pausePanel == null) return;

        GameObject btnGO = new GameObject("SettingsButton");
        btnGO.transform.SetParent(pausePanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.8f);

#if UNITY_EDITOR
        Sprite icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Custom/Prefabs/UI/settings-icon-14950.png");
        if (icon != null)
            img.sprite = icon;
        else
            img.color = new Color(0.2f, 0.45f, 0.65f, 0.76f);
#else
        img.color = new Color(0.2f, 0.45f, 0.65f, 0.76f);
#endif

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OpenSettings);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-180, -20);
        rt.sizeDelta = new Vector2(80, 80);
    }

    void CreateSaveButton()
    {
        if (pausePanel == null) return;

        GameObject btnGO = new GameObject("SaveButton");
        btnGO.transform.SetParent(pausePanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        img.sprite = saveButtonIcon;
        img.color = saveButtonIcon != null ? Color.white : new Color(0.25f, 0.65f, 0.45f, 0.76f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OpenSavePanel);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-280, -20);
        rt.sizeDelta = new Vector2(80, 80);
    }

    void CreateSavePanel()
    {
        if (pausePanel == null) return;

        savePanel = new GameObject("SavePanel");
        savePanel.transform.SetParent(pausePanel.transform, false);
        RectTransform srt = savePanel.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(560, 520);

        Image bg = savePanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = savePanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(savePanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = Loc("SavePanel.Title");
        titleGO.AddComponent<LocalizedText>().key = "SavePanel.Title";
        title.fontSize = 44;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 60);

        for (int i = 0; i <= SaveManager.SlotCount; i++)
        {
            int slotIndex = i;
            bool isAuto = (i == 0);
            GameObject slotGO = new GameObject(isAuto ? "AutoSaveButton" : $"Slot{i}Button");
            slotGO.transform.SetParent(savePanel.transform, false);
            slotGO.AddComponent<CanvasRenderer>();
            Image slotImg = slotGO.AddComponent<Image>();
            slotImg.color = isAuto ? new Color(0.1f, 0.25f, 0.35f, 0.9f) : new Color(0.15f, 0.15f, 0.2f, 0.9f);
            Button slotBtn = slotGO.AddComponent<Button>();
            slotBtn.targetGraphic = slotImg;
            slotBtn.onClick.AddListener(() => SelectSlot(slotIndex));
            RectTransform slotRt = slotGO.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(0, 70);

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(slotGO.transform, false);
            textGO.AddComponent<CanvasRenderer>();
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = isAuto ? Loc("SavePanel.AutoSave") : string.Format(Loc("SavePanel.Slot"), i);
            text.fontSize = 30;
            text.alignment = TextAlignmentOptions.Center;
            text.color = isAuto ? new Color(0.6f, 0.9f, 1f) : Color.white;
            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            slotButtons[i] = slotGO;
        }

        GameObject infoGO = new GameObject("InfoText");
        infoGO.transform.SetParent(savePanel.transform, false);
        infoGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI infoText = infoGO.AddComponent<TextMeshProUGUI>();
        infoText.name = "SaveInfoText";
        infoText.text = Loc("Common.SelectSlot");
        infoText.gameObject.AddComponent<LocalizedText>().key = "Common.SelectSlot";
        infoText.fontSize = 24;
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        RectTransform infoRt = infoGO.GetComponent<RectTransform>();
        infoRt.sizeDelta = new Vector2(0, 40);

        CreateSmallButton(savePanel.transform, "SavePanel.Save", new Vector2(220, 55), SaveSelectedSlot, true);
        CreateSmallButton(savePanel.transform, "SavePanel.Load", new Vector2(220, 55), LoadSelectedSlot, true);
        CreateSmallButton(savePanel.transform, "SavePanel.Delete", new Vector2(220, 55), DeleteSelectedSlot, true);
        CreateSmallButton(savePanel.transform, "Common.Close", new Vector2(220, 55), CloseSavePanel, true);

        savePanel.SetActive(false);
    }

    void RefreshSlotButtons()
    {
        if (savePanel == null) return;
        TextMeshProUGUI infoText = savePanel.transform.Find("SaveInfoText")?.GetComponent<TextMeshProUGUI>();

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null) continue;
            TextMeshProUGUI text = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            bool isAuto = (i == 0);
            int realSlot = isAuto ? SaveManager.AutoSaveSlot : i;
            bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave(realSlot);
            bool isSelected = selectedSlot == i;
            string label = isAuto ? Loc("SavePanel.AutoSave") : string.Format(Loc("SavePanel.Slot"), i);
            string state = hasSave ? Loc("Common.Yes") : Loc("Common.No");
            text.text = $"{label} {state}{(isSelected ? " <<" : "")}";
            Image img = slotButtons[i].GetComponent<Image>();
            if (isSelected)
                img.color = new Color(0.2f, 0.5f, 0.4f, 0.9f);
            else if (isAuto)
                img.color = new Color(0.1f, 0.25f, 0.35f, 0.9f);
            else
                img.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        }

        if (infoText != null)
        {
            if (selectedSlot >= 0)
            {
                bool isAuto = (selectedSlot == 0);
                int realSlot = isAuto ? SaveManager.AutoSaveSlot : selectedSlot;
                bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave(realSlot);
                string label = isAuto ? Loc("SavePanel.AutoSave") : string.Format(Loc("SavePanel.Slot"), selectedSlot);
                string state = hasSave ? Loc("Common.Occupied") : Loc("Common.Empty");
                infoText.text = $"{label}: {state}";
            }
            else
            {
                infoText.text = Loc("Common.SelectSlot");
            }
        }
    }

    void CreateSkillTreeButton()
    {
        if (pausePanel == null) return;

        GameObject btnGO = new GameObject("SkillTreeButton");
        btnGO.transform.SetParent(pausePanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        img.sprite = skillTreeButtonIcon;
        img.color = skillTreeButtonIcon != null ? Color.white : new Color(0.65f, 0.45f, 0.25f, 0.76f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OpenSkillTree);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-80, -20);
        rt.sizeDelta = new Vector2(80, 80);
    }

    void CreateSkillTreePanel()
    {
        if (pausePanel == null) return;
        if (skillsManager == null)
            skillsManager = FindAnyObjectByType<PlayerSkillsManager>();

        skillTreePanel = new GameObject("SkillTreePanel");
        skillTreePanel.transform.SetParent(pausePanel.transform, false);
        RectTransform srt = skillTreePanel.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(120, 120);
        srt.offsetMax = new Vector2(-120, -120);

        Image bg = skillTreePanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        // Header
        GameObject header = new GameObject("Header");
        header.transform.SetParent(skillTreePanel.transform, false);
        RectTransform headerRt = header.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0, 70);
        header.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 1f);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        RectTransform titleRt = titleGO.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.offsetMin = new Vector2(20, 0);
        titleRt.offsetMax = new Vector2(-70, 0);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = Loc("SkillTree.Title");
        titleGO.AddComponent<LocalizedText>().key = "SkillTree.Title";
        title.fontSize = 32;
        title.alignment = TextAlignmentOptions.Left;
        title.color = Color.white;

        GameObject closeBtnGO = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(header.transform, false);
        RectTransform closeRt = closeBtnGO.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 0.5f);
        closeRt.anchorMax = new Vector2(1, 0.5f);
        closeRt.pivot = new Vector2(1, 0.5f);
        closeRt.anchoredPosition = new Vector2(-10, 0);
        closeRt.sizeDelta = new Vector2(50, 50);
        Image closeImg = closeBtnGO.AddComponent<Image>();
        closeImg.color = new Color(0.5f, 0.15f, 0.1f, 1f);
        Button closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(CloseSkillTree);

        GameObject closeTextGO = new GameObject("Text");
        closeTextGO.transform.SetParent(closeBtnGO.transform, false);
        RectTransform closeTextRt = closeTextGO.AddComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTxt = closeTextGO.AddComponent<TextMeshProUGUI>();
        closeTxt.text = "X";
        closeTxt.fontSize = 24;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.color = Color.white;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(skillTreePanel.transform, false);
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 0);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.offsetMin = new Vector2(20, 20);
        contentRt.offsetMax = new Vector2(-20, -80);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        skillTreeContent = content.transform;

        // Talent points label
        GameObject pointsGO = new GameObject("TalentPointsLabel");
        pointsGO.transform.SetParent(content.transform, false);
        pointsGO.AddComponent<CanvasRenderer>();
        skillTreeTalentPointsText = pointsGO.AddComponent<TextMeshProUGUI>();
        skillTreeTalentPointsText.fontSize = 28;
        skillTreeTalentPointsText.alignment = TextAlignmentOptions.Center;
        skillTreeTalentPointsText.color = new Color(1f, 0.85f, 0.4f, 1f);
        RectTransform pointsRt = pointsGO.GetComponent<RectTransform>();
        pointsRt.sizeDelta = new Vector2(0, 50);

        // Skill rows
        if (skillsManager == null)
            skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager != null)
        {
            foreach (PlayerSkillInstance skill in skillsManager.Skills)
                CreateSkillRow(skill, content.transform);
        }

        CreateSkillDescriptionPopup();

        skillTreePanel.SetActive(false);
    }

    void CreateSkillDescriptionPopup()
    {
        skillDescriptionPanel = new GameObject("SkillDescriptionPanel");
        skillDescriptionPanel.transform.SetParent(skillTreePanel.transform, false);
        RectTransform rt = skillDescriptionPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(560, 380);

        Image bg = skillDescriptionPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

        VerticalLayoutGroup vlg = skillDescriptionPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(25, 25, 25, 25);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(skillDescriptionPanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "Fireball";
        title.fontSize = 36;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 50);

        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(skillDescriptionPanel.transform, false);
        descGO.AddComponent<CanvasRenderer>();
        skillDescriptionText = descGO.AddComponent<TextMeshProUGUI>();
        skillDescriptionText.fontSize = 24;
        skillDescriptionText.alignment = TextAlignmentOptions.Center;
        skillDescriptionText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        RectTransform descRt = descGO.GetComponent<RectTransform>();
        descRt.sizeDelta = new Vector2(0, 160);

        GameObject statsGO = new GameObject("Stats");
        statsGO.transform.SetParent(skillDescriptionPanel.transform, false);
        statsGO.AddComponent<CanvasRenderer>();
        skillDescriptionStatsText = statsGO.AddComponent<TextMeshProUGUI>();
        skillDescriptionStatsText.fontSize = 26;
        skillDescriptionStatsText.alignment = TextAlignmentOptions.Center;
        skillDescriptionStatsText.color = new Color(1f, 0.85f, 0.4f, 1f);
        RectTransform statsRt = statsGO.GetComponent<RectTransform>();
        statsRt.sizeDelta = new Vector2(0, 60);

        CreateSmallButton(skillDescriptionPanel.transform, "Common.Close", new Vector2(220, 55), CloseSkillDescription, true);

        skillDescriptionPanel.SetActive(false);
    }

    void RebuildSkillTreeRows()
    {
        if (skillTreeContent == null) return;

        foreach (var row in skillTreeRows)
        {
            if (row.root != null)
                Destroy(row.root);
        }
        skillTreeRows.Clear();

        if (skillsManager == null)
            skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager == null) return;

        foreach (PlayerSkillInstance skill in skillsManager.Skills)
            CreateSkillRow(skill, skillTreeContent);

        RefreshSkillTree();
    }

    void CreateSkillRow(PlayerSkillInstance skill, Transform parent)
    {
        if (skill == null || skill.Data == null) return;

        GameObject rowGO = new GameObject($"{skill.Data.skillName}Row");
        rowGO.transform.SetParent(parent, false);
        RectTransform rowRt = rowGO.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, 90);
        HorizontalLayoutGroup rowHlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 15;
        rowHlg.padding = new RectOffset(15, 15, 10, 10);
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = false;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = false;

        // Selection toggle
        GameObject toggleGO = new GameObject("SelectedToggle");
        toggleGO.transform.SetParent(rowGO.transform, false);
        RectTransform toggleRt = toggleGO.AddComponent<RectTransform>();
        toggleRt.sizeDelta = new Vector2(40, 40);
        LayoutElement toggleLe = toggleGO.AddComponent<LayoutElement>();
        toggleLe.minWidth = 40f;
        toggleLe.minHeight = 40f;
        Image toggleBg = toggleGO.AddComponent<Image>();
        toggleBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBg;
        toggle.isOn = skill.IsSelected;

        GameObject checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(toggleGO.transform, false);
        RectTransform checkRt = checkGO.AddComponent<RectTransform>();
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.offsetMin = Vector2.zero;
        checkRt.offsetMax = Vector2.zero;
        TextMeshProUGUI checkText = checkGO.AddComponent<TextMeshProUGUI>();
        checkText.text = "✓";
        checkText.fontSize = 28;
        checkText.alignment = TextAlignmentOptions.Center;
        checkText.color = new Color(0.2f, 0.95f, 0.25f, 1f);
        toggle.graphic = checkText;

        toggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn && skillsManager != null)
                skillsManager.SelectSkill(skill);
        });

        Image rowImg = rowGO.AddComponent<Image>();
        rowImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Button rowBtn = rowGO.AddComponent<Button>();
        rowBtn.targetGraphic = rowImg;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(rowGO.transform, false);
        RectTransform iconRt = iconGO.AddComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(70, 70);
        LayoutElement iconLe = iconGO.AddComponent<LayoutElement>();
        iconLe.minWidth = 70f;
        iconLe.minHeight = 70f;
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(1f, 0.45f, 0f, 1f);

        GameObject nameGO = new GameObject("NameLabel");
        nameGO.transform.SetParent(rowGO.transform, false);
        RectTransform nameRt = nameGO.AddComponent<RectTransform>();
        nameRt.sizeDelta = new Vector2(0, 70);
        nameGO.AddComponent<CanvasRenderer>();
        LayoutElement nameLe = nameGO.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1f;
        nameLe.minWidth = 140f;
        nameLe.minHeight = 70f;
        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 28;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = Color.white;
        nameText.overflowMode = TextOverflowModes.Overflow;

        GameObject costGO = new GameObject("CostLabel");
        costGO.transform.SetParent(rowGO.transform, false);
        RectTransform costRt = costGO.AddComponent<RectTransform>();
        costRt.sizeDelta = new Vector2(100, 70);
        costGO.AddComponent<CanvasRenderer>();
        LayoutElement costLe = costGO.AddComponent<LayoutElement>();
        costLe.minWidth = 100f;
        costLe.minHeight = 70f;
        TextMeshProUGUI costText = costGO.AddComponent<TextMeshProUGUI>();
        costText.fontSize = 26;
        costText.alignment = TextAlignmentOptions.Right;
        costText.color = new Color(1f, 0.85f, 0.4f, 1f);
        costText.overflowMode = TextOverflowModes.Overflow;

        GameObject plusGO = new GameObject("PlusButton");
        plusGO.transform.SetParent(rowGO.transform, false);
        RectTransform plusRt = plusGO.AddComponent<RectTransform>();
        plusRt.sizeDelta = new Vector2(80, 70);
        LayoutElement plusLe = plusGO.AddComponent<LayoutElement>();
        plusLe.minWidth = 80f;
        plusLe.minHeight = 70f;
        Image plusImg = plusGO.AddComponent<Image>();
        plusImg.sprite = CreateCircleGradientSprite(64, new Color(0.25f, 0.95f, 0.35f, 1f), new Color(0.05f, 0.55f, 0.15f, 1f));
        plusImg.type = Image.Type.Simple;
        plusImg.preserveAspect = true;
        Button plusBtn = plusGO.AddComponent<Button>();
        plusBtn.targetGraphic = plusImg;

        GameObject plusTextGO = new GameObject("Text");
        plusTextGO.transform.SetParent(plusGO.transform, false);
        RectTransform plusTextRt = plusTextGO.AddComponent<RectTransform>();
        plusTextRt.anchorMin = Vector2.zero;
        plusTextRt.anchorMax = Vector2.one;
        plusTextRt.offsetMin = Vector2.zero;
        plusTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI plusText = plusTextGO.AddComponent<TextMeshProUGUI>();
        plusText.text = "+";
        plusText.fontSize = 48;
        plusText.alignment = TextAlignmentOptions.Center;
        plusText.color = Color.white;

        SkillTreeRow row = new SkillTreeRow
        {
            root = rowGO,
            skill = skill,
            selectedToggle = toggle,
            nameText = nameText,
            costText = costText,
            plusButton = plusBtn,
            icon = iconImg
        };
        skillTreeRows.Add(row);

        rowBtn.onClick.AddListener(() => OpenSkillDescription(row.skill));
        plusBtn.onClick.AddListener(() =>
        {
            if (row.skill != null && row.skill.TryUpgrade())
                RefreshSkillTree();
        });
    }

    void RefreshSkillTree()
    {
        if (skillsManager == null) skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager == null) return;

        int totalPoints = 0;
        foreach (var s in skillsManager.Skills)
            totalPoints += s.TalentPoints;

        if (skillTreeTalentPointsText != null)
            skillTreeTalentPointsText.text = string.Format(Loc("SkillTree.TalentPoints"), totalPoints);

        foreach (var row in skillTreeRows)
        {
            if (row.skill == null || row.skill.Data == null) continue;
            row.nameText.text = $"{row.skill.Data.skillName} {string.Format(Loc("SkillTree.Level"), row.skill.Level)}";
            row.costText.text = string.Format(Loc("SkillTree.Cost"), row.skill.GetUpgradeCost());
            row.plusButton.interactable = row.skill.CanUpgrade();
            if (row.skill.Data.icon != null)
                row.icon.sprite = row.skill.Data.icon;
            if (row.selectedToggle != null)
                row.selectedToggle.SetIsOnWithoutNotify(row.skill.IsSelected);
        }
    }

    void OpenSkillDescription(PlayerSkillInstance skill)
    {
        if (skill == null || skill.Data == null || skillDescriptionPanel == null) return;
        selectedSkill = skill;

        TextMeshProUGUI title = skillDescriptionPanel.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        if (title != null) title.text = skill.Data.skillName;

        skillDescriptionText.text = skill.Data.description;
        skillDescriptionStatsText.text = string.Format(Loc("SkillTree.Damage"), skill.GetCurrentDamage()) + "\n" +
            string.Format(Loc("SkillTree.Cooldown"), skill.GetCurrentCooldown());
        skillDescriptionPanel.SetActive(true);
    }

    void CloseSkillDescription()
    {
        if (skillDescriptionPanel != null)
            skillDescriptionPanel.SetActive(false);
    }

    void OnSkillTreeChanged()
    {
        if (skillTreePanel != null && skillTreePanel.activeSelf)
            RefreshSkillTree();
        if (loadoutPanel != null && loadoutPanel.activeSelf)
            RefreshLoadoutSlots();
    }

    #region Loadout Panel

    void CreateLoadoutButton()
    {
        if (pausePanel == null) return;

        GameObject btnGO = new GameObject("LoadoutButton");
        btnGO.transform.SetParent(pausePanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        img.sprite = skillTreeButtonIcon;
        img.color = skillTreeButtonIcon != null ? Color.white : new Color(0.3f, 0.55f, 0.8f, 0.76f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OpenLoadout);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-380, -20);
        rt.sizeDelta = new Vector2(80, 80);
    }

    void CreateLoadoutPanel()
    {
        if (pausePanel == null) return;
        if (skillsManager == null)
            skillsManager = FindAnyObjectByType<PlayerSkillsManager>();

        loadoutPanel = new GameObject("LoadoutPanel");
        loadoutPanel.transform.SetParent(pausePanel.transform, false);
        RectTransform srt = loadoutPanel.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(120, 120);
        srt.offsetMax = new Vector2(-120, -120);

        Image bg = loadoutPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = loadoutPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(25, 25, 25, 25);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header
        GameObject header = new GameObject("Header");
        header.transform.SetParent(loadoutPanel.transform, false);
        RectTransform headerRt = header.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.sizeDelta = new Vector2(0, 60);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        RectTransform titleRt = titleGO.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.offsetMax = new Vector2(-70, 0);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = Loc("Loadout.Title");
        titleGO.AddComponent<LocalizedText>().key = "Loadout.Title";
        title.fontSize = 32;
        title.alignment = TextAlignmentOptions.Left;
        title.color = Color.white;

        GameObject closeBtnGO = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(header.transform, false);
        RectTransform closeRt = closeBtnGO.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 0);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 0.5f);
        closeRt.sizeDelta = new Vector2(60, 60);
        closeBtnGO.AddComponent<CanvasRenderer>();
        Image closeImg = closeBtnGO.AddComponent<Image>();
        closeImg.color = new Color(0.5f, 0.15f, 0.1f, 1f);
        Button closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(CloseLoadout);

        GameObject closeTextGO = new GameObject("Text");
        closeTextGO.transform.SetParent(closeBtnGO.transform, false);
        RectTransform closeTextRt = closeTextGO.AddComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;
        closeTextGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
        closeText.text = "×";
        closeText.fontSize = 48;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;

        // Slot row
        GameObject slotRowGO = new GameObject("SlotRow");
        slotRowGO.transform.SetParent(loadoutPanel.transform, false);
        RectTransform slotRowRt = slotRowGO.AddComponent<RectTransform>();
        slotRowRt.sizeDelta = new Vector2(0, 120);
        HorizontalLayoutGroup slotHlg = slotRowGO.AddComponent<HorizontalLayoutGroup>();
        slotHlg.spacing = 20;
        slotHlg.childAlignment = TextAnchor.MiddleCenter;
        slotHlg.childControlWidth = false;
        slotHlg.childControlHeight = false;
        slotHlg.childForceExpandWidth = false;
        slotHlg.childForceExpandHeight = false;

        LayoutElement slotRowLe = slotRowGO.AddComponent<LayoutElement>();
        slotRowLe.minHeight = 120f;
        slotRowLe.preferredHeight = 120f;

        for (int i = 0; i < 4; i++)
        {
            int slotIndex = i;
            GameObject slotGO = new GameObject($"Slot{i + 1}");
            slotGO.transform.SetParent(slotRowGO.transform, false);
            RectTransform slotRect = slotGO.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(100, 100);
            LayoutElement slotLe = slotGO.AddComponent<LayoutElement>();
            slotLe.minWidth = 100f;
            slotLe.minHeight = 100f;
            slotLe.preferredWidth = 100f;
            slotLe.preferredHeight = 100f;

            Image slotBg = slotGO.AddComponent<Image>();
            slotBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            Button slotBtn = slotGO.AddComponent<Button>();
            slotBtn.targetGraphic = slotBg;
            loadoutSlotButtons[i] = slotBtn;

            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRt = iconGO.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(10, 10);
            iconRt.offsetMax = new Vector2(-10, -10);
            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            iconImg.enabled = false;
            loadoutSlotIcons[i] = iconImg;

            GameObject labelGO = new GameObject("SlotLabel");
            labelGO.transform.SetParent(slotGO.transform, false);
            RectTransform labelRt = labelGO.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0);
            labelRt.pivot = new Vector2(0.5f, 0);
            labelRt.sizeDelta = new Vector2(0, 24);
            labelGO.AddComponent<CanvasRenderer>();
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = (i + 1).ToString();
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            slotBtn.onClick.AddListener(() => ShowLoadoutSkillList(slotIndex));
        }

        // Skill list (scrollable)
        GameObject scrollGO = new GameObject("SkillListScroll");
        scrollGO.transform.SetParent(loadoutPanel.transform, false);
        RectTransform scrollRt = scrollGO.AddComponent<RectTransform>();
        scrollRt.sizeDelta = new Vector2(0, 300);
        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

        LayoutElement scrollLe = scrollGO.AddComponent<LayoutElement>();
        scrollLe.minHeight = 300f;
        scrollLe.preferredHeight = 300f;
        scrollLe.flexibleHeight = 1f;

        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRt = viewportGO.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        Image viewportBg = viewportGO.AddComponent<Image>();
        viewportBg.color = Color.clear;
        viewportGO.AddComponent<RectMask2D>();

        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(-10, 0);

        VerticalLayoutGroup contentVlg = contentGO.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 8;
        contentVlg.padding = new RectOffset(10, 10, 10, 10);
        contentVlg.childAlignment = TextAnchor.UpperCenter;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = false;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        loadoutSkillListContent = contentGO.transform;

        // Hint text (shown when no slot selected)
        GameObject hintGO = new GameObject("HintText");
        hintGO.transform.SetParent(loadoutPanel.transform, false);
        RectTransform hintRt = hintGO.AddComponent<RectTransform>();
        hintRt.sizeDelta = new Vector2(0, 40);
        hintGO.AddComponent<CanvasRenderer>();
        LayoutElement hintLe = hintGO.AddComponent<LayoutElement>();
        hintLe.minHeight = 40f;
        hintLe.preferredHeight = 40f;
        TextMeshProUGUI hint = hintGO.AddComponent<TextMeshProUGUI>();
        hint.text = Loc("Loadout.SelectSlot");
        hintGO.AddComponent<LocalizedText>().key = "Loadout.SelectSlot";
        hint.fontSize = 24;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.6f, 0.6f, 0.6f, 1f);

        loadoutPanel.SetActive(false);
    }

    public void OpenLoadout()
    {
        if (loadoutPanel == null) return;
        HideInventory();
        ShowSkillButtons();
        loadoutPanel.SetActive(true);
        loadoutPanel.transform.SetAsLastSibling();
        Transform canvasTransform = pausePanel != null ? pausePanel.GetComponentInParent<Canvas>()?.transform : null;
        if (canvasTransform != null && transform.parent == canvasTransform)
            transform.SetAsLastSibling();

        selectedLoadoutSlot = 0;
        RefreshLoadoutSlots();
        ShowLoadoutSkillList(0);
    }

    public void CloseLoadout()
    {
        if (loadoutPanel != null)
            loadoutPanel.SetActive(false);
        HideSkillButtons();
        ShowInventory();
    }

    void RefreshLoadoutSlots()
    {
        if (skillsManager == null) skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager == null) return;

        for (int i = 0; i < 4; i++)
        {
            PlayerSkillInstance skill = skillsManager.GetSlot(i);
            bool enabled = skillsManager.IsSlotEnabled(i) && skill != null;

            if (loadoutSlotIcons[i] != null)
            {
                if (skill != null && skill.Data != null && skill.Data.icon != null)
                {
                    loadoutSlotIcons[i].sprite = skill.Data.icon;
                    loadoutSlotIcons[i].enabled = true;
                }
                else
                {
                    loadoutSlotIcons[i].enabled = false;
                }
            }

            if (loadoutSlotButtons[i] != null)
            {
                ColorBlock colors = loadoutSlotButtons[i].colors;
                colors.normalColor = (i == selectedLoadoutSlot)
                    ? new Color(0.3f, 0.6f, 0.9f, 1f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.9f);
                loadoutSlotButtons[i].colors = colors;
            }
        }
    }

    void ShowLoadoutSkillList(int slotIndex)
    {
        selectedLoadoutSlot = slotIndex;
        RefreshLoadoutSlots();
        ClearLoadoutSkillButtons();

        if (skillsManager == null) skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager == null) return;

        PlayerSkillInstance currentSkill = skillsManager.GetSlot(slotIndex);
        Color selectedRowColor = new Color(0.2f, 0.55f, 0.85f, 0.95f);
        Color normalRowColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        foreach (PlayerSkillInstance skill in skillsManager.Skills)
        {
            if (skill == null || skill.Data == null) continue;

            PlayerSkillInstance captured = skill;
            GameObject rowGO = new GameObject($"Skill_{skill.Data.skillName}");
            rowGO.transform.SetParent(loadoutSkillListContent, false);
            RectTransform rowRt = rowGO.AddComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, 80);
            HorizontalLayoutGroup rowHlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 12;
            rowHlg.padding = new RectOffset(10, 10, 8, 8);
            rowHlg.childAlignment = TextAnchor.MiddleLeft;
            rowHlg.childControlWidth = false;
            rowHlg.childControlHeight = false;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;

            LayoutElement rowLe = rowGO.AddComponent<LayoutElement>();
            rowLe.minHeight = 80f;
            rowLe.preferredHeight = 80f;

            Image rowBg = rowGO.AddComponent<Image>();
            bool isSelected = currentSkill != null && currentSkill == skill;
            rowBg.color = isSelected ? selectedRowColor : normalRowColor;
            Button rowBtn = rowGO.AddComponent<Button>();
            rowBtn.transition = Selectable.Transition.None;
            rowBtn.targetGraphic = rowBg;
            ColorBlock rowColors = rowBtn.colors;
            rowColors.normalColor = rowBg.color;
            rowColors.highlightedColor = rowBg.color;
            rowColors.pressedColor = new Color(rowBg.color.r * 0.85f, rowBg.color.g * 0.85f, rowBg.color.b * 0.85f, rowBg.color.a);
            rowColors.selectedColor = rowBg.color;
            rowBtn.colors = rowColors;

            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);
            RectTransform iconRt = iconGO.AddComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(60, 60);
            LayoutElement iconLe = iconGO.AddComponent<LayoutElement>();
            iconLe.minWidth = 60f;
            iconLe.minHeight = 60f;
            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            if (skill.Data.icon != null)
            {
                iconImg.sprite = skill.Data.icon;
                iconImg.enabled = true;
            }
            else
            {
                iconImg.color = new Color(1f, 0.45f, 0f, 1f);
                iconImg.enabled = true;
            }

            GameObject nameGO = new GameObject("Name");
            nameGO.transform.SetParent(rowGO.transform, false);
            RectTransform nameRt = nameGO.AddComponent<RectTransform>();
            nameRt.sizeDelta = new Vector2(0, 60);
            nameGO.AddComponent<CanvasRenderer>();
            LayoutElement nameLe = nameGO.AddComponent<LayoutElement>();
            nameLe.flexibleWidth = 1f;
            nameLe.minWidth = 120f;
            nameLe.minHeight = 60f;
            TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = skill.Data.skillName;
            nameText.fontSize = 26;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.color = Color.white;
            nameText.overflowMode = TextOverflowModes.Overflow;

            rowBtn.onClick.AddListener(() => AssignSkillToSlot(captured, slotIndex));
            loadoutSkillButtons.Add(rowGO);
        }

        // Clear slot button
        GameObject clearGO = new GameObject("ClearSlotButton");
        clearGO.transform.SetParent(loadoutSkillListContent, false);
        RectTransform clearRt = clearGO.AddComponent<RectTransform>();
        clearRt.sizeDelta = new Vector2(0, 60);
        clearGO.AddComponent<CanvasRenderer>();

        LayoutElement clearLe = clearGO.AddComponent<LayoutElement>();
        clearLe.minHeight = 60f;
        clearLe.preferredHeight = 60f;

        Image clearImg = clearGO.AddComponent<Image>();
        bool slotEmpty = currentSkill == null;
        clearImg.color = slotEmpty ? new Color(0.9f, 0.3f, 0.25f, 0.95f) : new Color(0.6f, 0.15f, 0.1f, 0.9f);
        Button clearBtn = clearGO.AddComponent<Button>();
        clearBtn.transition = Selectable.Transition.None;
        clearBtn.targetGraphic = clearImg;
        clearBtn.onClick.AddListener(() => AssignSkillToSlot(null, slotIndex));

        GameObject clearTextGO = new GameObject("Text");
        clearTextGO.transform.SetParent(clearGO.transform, false);
        RectTransform clearTextRt = clearTextGO.AddComponent<RectTransform>();
        clearTextRt.anchorMin = Vector2.zero;
        clearTextRt.anchorMax = Vector2.one;
        clearTextRt.offsetMin = Vector2.zero;
        clearTextRt.offsetMax = Vector2.zero;
        clearTextGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI clearText = clearTextGO.AddComponent<TextMeshProUGUI>();
        clearText.text = Loc("Loadout.ClearSlot");
        clearTextGO.AddComponent<LocalizedText>().key = "Loadout.ClearSlot";
        clearText.fontSize = 28;
        clearText.alignment = TextAlignmentOptions.Center;
        clearText.color = Color.white;

        loadoutSkillButtons.Add(clearGO);

        if (loadoutSkillListContent is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Canvas.ForceUpdateCanvases();
        }
    }

    void AssignSkillToSlot(PlayerSkillInstance skill, int slotIndex)
    {
        if (skillsManager == null) skillsManager = FindAnyObjectByType<PlayerSkillsManager>();
        if (skillsManager == null) return;
        skillsManager.SetSlot(slotIndex, skill);
        RefreshLoadoutSlots();
        ShowLoadoutSkillList(slotIndex);
        UpdateStatsDisplay();
    }

    void ClearLoadoutSkillButtons()
    {
        foreach (GameObject go in loadoutSkillButtons)
            if (go != null) Destroy(go);
        loadoutSkillButtons.Clear();
    }

    #endregion

    Sprite CreateCircleGradientSprite(int size, Color center, Color edge)
    {
        Texture2D tex = new Texture2D(size, size);
        Vector2 centerPos = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centerPos);
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float t = dist / radius;
                    tex.SetPixel(x, y, Color.Lerp(center, edge, t));
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void CreateBestiaryPanel()
    {
        if (pausePanel == null) return;

        bestiaryPanel = new GameObject("BestiaryPanel");
        bestiaryPanel.transform.SetParent(pausePanel.transform, false);
        RectTransform srt = bestiaryPanel.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(600, 500);

        Image bg = bestiaryPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = bestiaryPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(25, 25, 25, 25);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(bestiaryPanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = Loc("Bestiary.Title");
        titleGO.AddComponent<LocalizedText>().key = "Bestiary.Title";
        title.fontSize = 48;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 60);

        GameObject scrollViewportGO = new GameObject("Viewport");
        scrollViewportGO.transform.SetParent(bestiaryPanel.transform, false);
        RectTransform viewportRt = scrollViewportGO.AddComponent<RectTransform>();
        viewportRt.sizeDelta = new Vector2(0, 350);
        Image viewportBg = scrollViewportGO.AddComponent<Image>();
        viewportBg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
        Mask viewportMask = scrollViewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject scrollContentGO = new GameObject("Content");
        scrollContentGO.transform.SetParent(scrollViewportGO.transform, false);
        RectTransform contentRt = scrollContentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.offsetMin = new Vector2(5, 5);
        contentRt.offsetMax = new Vector2(-5, -5);

        VerticalLayoutGroup contentVlg = scrollContentGO.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 8;
        contentVlg.padding = new RectOffset(10, 10, 10, 10);
        contentVlg.childAlignment = TextAnchor.UpperCenter;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = false;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;

        ContentSizeFitter csf = scrollContentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = bestiaryPanel.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        bestiaryContainer = scrollContentGO.transform;

        CreateSmallButton(bestiaryPanel.transform, "Common.Close", new Vector2(300, 60), CloseBestiary, true);

        bestiaryPanel.SetActive(false);
    }

    void CreateBestiaryButton()
    {
        if (pausePanel == null) return;

        GameObject btnGO = new GameObject("BestiaryButton");
        btnGO.transform.SetParent(pausePanel.transform, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        
        // Если есть иконка, используем её, иначе используем цвет
        if (bestiaryButtonIcon != null)
        {
            img.sprite = bestiaryButtonIcon;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.8f, 0.4f, 0.2f, 0.8f);
        }

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OpenBestiary);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);  // Левый верхний угол
        rt.anchorMax = new Vector2(0, 1);  // Левый верхний угол
        rt.pivot = new Vector2(0, 1);      // Левый верхний угол
        rt.anchoredPosition = new Vector2(20, -20);  // Отступ 20 пикселей слева и сверху
        rt.sizeDelta = new Vector2(80, 80);

        // Если нет иконки, добавляем текст
        if (bestiaryButtonIcon == null)
        {
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            textGO.AddComponent<CanvasRenderer>();
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "B";
            text.fontSize = 40;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }
    }

    public void OpenBestiary()
    {
        HideInventory();
        HideSkillButtons();
        if (bestiaryPanel != null)
        {
            bestiaryPanel.SetActive(true);
            RefreshBestiary();
        }
    }

    public void CloseBestiary()
    {
        if (bestiaryPanel != null)
            bestiaryPanel.SetActive(false);
        ShowInventory();
    }

    void RefreshBestiary()
    {
        if (bestiaryContainer == null) return;

        foreach (GameObject btn in bestiaryButtons)
            if (btn != null) Destroy(btn);
        bestiaryButtons.Clear();

        var kills = Bestiary.GetAll();
        if (kills.Count == 0)
        {
            GameObject emptyGO = new GameObject("EmptyText");
            emptyGO.transform.SetParent(bestiaryContainer, false);
            emptyGO.AddComponent<CanvasRenderer>();
            TextMeshProUGUI emptyText = emptyGO.AddComponent<TextMeshProUGUI>();
            emptyText.text = Loc("Bestiary.Empty");
            emptyText.fontSize = 32;
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            RectTransform emptyRt = emptyGO.GetComponent<RectTransform>();
            emptyRt.sizeDelta = new Vector2(0, 50);
            bestiaryButtons.Add(emptyGO);
            return;
        }

        foreach (var kv in kills)
        {
            GameObject entryGO = new GameObject("BestiaryEntry");
            entryGO.transform.SetParent(bestiaryContainer, false);
            entryGO.AddComponent<CanvasRenderer>();
            Image entryBg = entryGO.AddComponent<Image>();
            entryBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
            RectTransform entryRt = entryGO.GetComponent<RectTransform>();
            entryRt.sizeDelta = new Vector2(0, 120);

            // Очищаем имя врага от цифр в скобках
            string cleanEnemyName = System.Text.RegularExpressions.Regex.Replace(kv.Key, @"\s*\(\d+\)\s*", "").Trim();
            
            // Получаем данные врага
            BestiaryData enemyData = BestiaryData.GetByName(cleanEnemyName);

            // Иконка врага (левая сторона, 1/5 экрана)
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(entryGO.transform, false);
            iconGO.AddComponent<CanvasRenderer>();
            Image iconImage = iconGO.AddComponent<Image>();
            if (enemyData != null && enemyData.enemyIcon != null)
                iconImage.sprite = enemyData.enemyIcon;
            else
                iconImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            RectTransform iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(100, 100);
            iconRt.anchoredPosition = new Vector2(10, 0);

            // Текст с информацией (рядом с иконкой)
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(entryGO.transform, false);
            textGO.AddComponent<CanvasRenderer>();
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = $"{cleanEnemyName}\n{string.Format(Loc("Bestiary.Killed"), kv.Value)}";
            text.fontSize = 26;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;
            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0.5f);
            textRt.anchorMax = new Vector2(1, 0.5f);
            textRt.pivot = new Vector2(0, 0.5f);
            textRt.offsetMin = new Vector2(120, -30);
            textRt.offsetMax = new Vector2(-110, 30);

            // Кнопка "Описание"
            GameObject descBtnGO = new GameObject("DescriptionButton");
            descBtnGO.transform.SetParent(entryGO.transform, false);
            descBtnGO.AddComponent<CanvasRenderer>();
            Image descBtnImg = descBtnGO.AddComponent<Image>();
            descBtnImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            Button descBtn = descBtnGO.AddComponent<Button>();
            descBtn.targetGraphic = descBtnImg;
            RectTransform descBtnRt = descBtnGO.GetComponent<RectTransform>();
            descBtnRt.anchorMin = new Vector2(1, 0.5f);
            descBtnRt.anchorMax = new Vector2(1, 0.5f);
            descBtnRt.pivot = new Vector2(1, 0.5f);
            descBtnRt.sizeDelta = new Vector2(100, 50);
            descBtnRt.anchoredPosition = new Vector2(-10, 0);

            GameObject descBtnTextGO = new GameObject("Text");
            descBtnTextGO.transform.SetParent(descBtnGO.transform, false);
            descBtnTextGO.AddComponent<CanvasRenderer>();
            TextMeshProUGUI descBtnText = descBtnTextGO.AddComponent<TextMeshProUGUI>();
            descBtnText.text = Loc("Bestiary.DescriptionButton");
            descBtnText.fontSize = 20;
            descBtnText.alignment = TextAlignmentOptions.Center;
            descBtnText.color = Color.black;
            RectTransform descBtnTextRt = descBtnTextGO.GetComponent<RectTransform>();
            descBtnTextRt.anchorMin = Vector2.zero;
            descBtnTextRt.anchorMax = Vector2.one;
            descBtnTextRt.offsetMin = Vector2.zero;
            descBtnTextRt.offsetMax = Vector2.zero;

            // Привязываем кнопку к методу открытия описания
            string capturedEnemyName = cleanEnemyName;
            descBtn.onClick.AddListener(() => OpenBestiaryDescription(capturedEnemyName, enemyData));

            bestiaryButtons.Add(entryGO);
        }
    }

    void HideInventory()
    {
        inventoryVisible = false;
        RefreshInventory();
    }

    void ShowInventory()
    {
        inventoryVisible = true;
        RefreshInventory();
    }

    void FindAndStyleResumeQuitButtons()
    {
        if (resumeButton == null)
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (Button btn in allButtons)
            {
                if (btn.name == "ResumeButton")
                {
                    resumeButton = btn;
                    break;
                }
            }
        }

        if (quitButton == null)
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (Button btn in allButtons)
            {
                if (btn.name == "QuitButton")
                {
                    quitButton = btn;
                    break;
                }
            }
        }

        if (resumeButton != null)
        {
            StyleResumeQuitButton(resumeButton);
            LocalizeExistingButtonText(resumeButton, "Common.Resume");
            RectTransform rt = resumeButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(-120, 50);
            }
        }

        if (quitButton != null)
        {
            StyleResumeQuitButton(quitButton);
            LocalizeExistingButtonText(quitButton, "Common.Quit");
            RectTransform rt = quitButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(120, 50);
            }
        }
    }

    void StyleResumeQuitButton(Button btn)
    {
        if (btn == null) return;

        Image img = btn.targetGraphic as Image;
        if (img != null)
        {
            img.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        }

        TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.color = new Color(1f, 0.84f, 0f, 1f);
            text.fontSize = 36;
            text.fontStyle = FontStyles.Bold;
        }

        Shadow shadow = btn.gameObject.GetComponent<Shadow>();
        if (shadow == null)
            shadow = btn.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(4, -4);
    }

    void LocalizeExistingButtonText(Button btn, string key)
    {
        if (btn == null) return;
        TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) return;
        text.text = Loc(key);
        LocalizedText loc = text.GetComponent<LocalizedText>();
        if (loc == null) loc = text.gameObject.AddComponent<LocalizedText>();
        loc.key = key;
    }

    void CreateStatButtons()
    {
        if (pausePanel == null) return;

        GameObject container = new GameObject("StatButtonsContainer");
        RectTransform rt = container.AddComponent<RectTransform>();
        container.transform.SetParent(pausePanel.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 100);
        rt.sizeDelta = new Vector2(900, 90);

        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        buttonsParent = container.transform;

        for (int i = 0; i < statNames.Length; i++)
            CreateStatButton(statNames[i], i);

        // Добавляем кнопку статистики
        CreateStatisticsButton();

        UpdateButtonsVisibility();
    }

    void UpdateButtonsVisibility()
    {
        if (buttonsParent == null) return;
        bool visible = playerStats != null && playerStats.skillPoints > 0;
        buttonsParent.gameObject.SetActive(visible);
        Debug.Log($"[PauseMenu] Stat buttons visible={visible}, skillPoints={playerStats?.skillPoints ?? 0}");
    }

    void CreateStatButton(string statName, int index)
    {
        GameObject btnGO = new GameObject($"Btn_{statName}");
        btnGO.transform.SetParent(buttonsParent, false);

        Color leftColor = new Color(0f, 1f, 0.5f, 1f);
        Color rightColor = new Color(0f, 0.85f, 1f, 1f);
        Color shadowColor = new Color(0f, 0.3f, 0.4f, 0.75f);

        Image img = btnGO.AddComponent<Image>();
        img.sprite = CreateGradientPillSprite(165, 75, 150, 60, leftColor, rightColor, shadowColor, new Vector2(5, -5));
        img.color = Color.white;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        string statKey = $"Stats.{statName}";
        text.text = Loc(statKey);
        LocalizedText loc = textGO.AddComponent<LocalizedText>();
        loc.key = statKey;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.fontSize = 33;
        text.fontStyle = FontStyles.Bold;

        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        RectTransform btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(150, 60);

        int capturedIndex = index;
        btn.onClick.AddListener(() => IncreaseStat(capturedIndex));
    }

    Sprite CreateGradientPillSprite(int texWidth, int texHeight, int pillWidth, int pillHeight, Color leftColor, Color rightColor, Color shadowColor, Vector2 shadowOffset)
    {
        Texture2D tex = new Texture2D(texWidth, texHeight);
        Color[] pixels = new Color[texWidth * texHeight];
        Vector2 center = new Vector2(texWidth / 2f, texHeight / 2f);
        Vector2 shadowCenter = center + shadowOffset;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int i = y * texWidth + x;
                pixels[i] = Color.clear;

                if (IsInsidePill(x, y, shadowCenter.x, shadowCenter.y, pillWidth, pillHeight))
                {
                    pixels[i] = shadowColor;
                }

                if (IsInsidePill(x, y, center.x, center.y, pillWidth, pillHeight))
                {
                    float t = Mathf.InverseLerp(center.x - pillWidth / 2f, center.x + pillWidth / 2f, x);
                    pixels[i] = Color.Lerp(leftColor, rightColor, t);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), 100);
    }

    bool IsInsidePill(int x, int y, float cx, float cy, int width, int height)
    {
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        float radius = halfHeight;

        float dx = Mathf.Abs(x - cx);
        float dy = Mathf.Abs(y - cy);

        if (dx <= halfWidth - radius)
            return dy <= halfHeight;

        dx -= halfWidth - radius;
        return dx * dx + dy * dy <= radius * radius;
    }

    public void IncreaseStat(int index)
    {
        if (playerStats == null || playerStats.skillPoints <= 0) return;

        switch (index)
        {
            case 0: // HP
                playerStats.maxHp += 5;
                if (playerCombat != null)
                    playerCombat.maxHealth = playerStats.maxHp;
                playerCombat?.Heal(5);
                break;
            case 1: // ATK
                playerStats.atk += 1;
                if (playerCombat != null)
                    playerCombat.damage = playerStats.atk;
                break;
            case 2: // DEF
                playerStats.def += 1;
                break;
            case 3: // SPD
                playerStats.spd += 1;
                break;
            case 4: // LCK
                playerStats.lck += 1;
                break;
        }

        playerStats.skillPoints--;
        UpdateStatsDisplay();
    }

    public void RefreshStats()
    {
        UpdateStatsDisplay();
    }

    public void UpdateStatsDisplay()
    {
        if (playerStats == null) return;

        if (statsText != null)
        {
            int junkCount = 0;
            int junkValue = 0;
            if (playerInventory != null)
            {
                foreach (InventoryItem invItem in playerInventory.items)
                {
                    if (invItem.itemData != null && invItem.itemData.itemType == ItemType.Material)
                    {
                        junkCount += invItem.quantity;
                        junkValue += invItem.itemData.price * invItem.quantity;
                    }
                }
            }

            statsText.text =
                $"{Loc("Stats.Level")}: {playerStats.level}\n" +
                $"{Loc("Stats.XP")}: {playerStats.experience} / {playerStats.experienceToNextLevel}\n" +
                $"{Loc("Stats.Gold")}: {playerStats.gold}\n" +
                $"{Loc("Stats.SkillPoints")}: {playerStats.skillPoints}\n\n" +
                $"{Loc("Stats.HP")}: {playerStats.hp} / {playerStats.maxHp}\n" +
                $"{Loc("Stats.ATK")}: {playerStats.atk}\n" +
                $"{Loc("Stats.DEF")}: {playerStats.def} ({playerStats.GetDamageReductionPercent():F0}%)\n" +
                $"{Loc("Stats.SPD")}: {playerStats.spd}\n" +
                $"{Loc("Stats.LCK")}: {playerStats.lck}\n" +
                $"{Loc("Stats.AtkSpd")}: {playerStats.atkSpd} ({(1f / playerStats.GetAttackCooldownMultiplier() - 1f) * 100f:F0}%)\n" +
                $"{Loc("Stats.Lethality")}: {playerStats.GetEffectiveLethality()} ({playerStats.lethality} + {playerStats.GetEffectiveLethality() - playerStats.lethality} {Loc("Stats.FromLCK")})\n\n" +
                $"{Loc("Stats.Regen")}: {playerStats.GetHpRegenPerSecond():F1} {Loc("Stats.PerSecond")}\n" +
                $"{Loc("Stats.Crit")}: {playerStats.GetCritChance() * 100f:F0}%\n\n" +
                $"{Loc("Stats.Junk")}: {junkCount} {Loc("Stats.Items")}\n" +
                $"{Loc("Stats.SellValue")}: {junkValue}g";
        }

        if (skillPointsText != null)
            skillPointsText.text = $"{Loc("Stats.SkillPoints")}: {playerStats.skillPoints}";

        UpdateButtonsVisibility();
    }

    void CreateInventoryPanel()
    {
        if (pausePanel == null) return;

        RectTransform panelRt = pausePanel.GetComponent<RectTransform>();
        float panelWidth = panelRt != null ? panelRt.rect.width : 1200f;
        float panelHeight = panelRt != null ? panelRt.rect.height : 700f;

        GameObject invPanel = new GameObject("InventoryPanel");
        invPanel.transform.SetParent(pausePanel.transform, false);
        RectTransform invRt = invPanel.AddComponent<RectTransform>();
        invRt.anchorMin = new Vector2(0.5f, 0.5f);
        invRt.anchorMax = new Vector2(0.5f, 0.5f);
        invRt.pivot = new Vector2(0.5f, 0.5f);
        invRt.anchoredPosition = new Vector2(0, 95);
        float invWidth = Mathf.Min(760f, panelWidth * 0.72f);
        float invHeight = Mathf.Min(430f, panelHeight * 0.68f);
        invRt.sizeDelta = new Vector2(invWidth, invHeight);

        Image invBg = invPanel.AddComponent<Image>();
        invBg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        invBg.raycastTarget = true;

        VerticalLayoutGroup vlg = invPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject eqGO = new GameObject("EquippedText");
        eqGO.transform.SetParent(invPanel.transform, false);
        eqGO.AddComponent<CanvasRenderer>();
        equippedText = eqGO.AddComponent<TextMeshProUGUI>();
        equippedText.fontSize = 30;
        equippedText.enableAutoSizing = true;
        equippedText.fontSizeMin = 20;
        equippedText.fontSizeMax = 46;
        equippedText.alignment = TextAlignmentOptions.TopLeft;
        equippedText.color = new Color(1f, 0.85f, 0.3f, 1f);
        RectTransform eqRt = eqGO.GetComponent<RectTransform>();
        eqRt.sizeDelta = new Vector2(0, 120);

        GameObject cardGO = new GameObject("InventoryCard");
        cardGO.transform.SetParent(invPanel.transform, false);
        RectTransform cardRt = cardGO.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(0, 170);
        cardGO.AddComponent<CanvasRenderer>();
        Image cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        inventoryContainer = cardGO.transform;

        GameObject navGO = new GameObject("InventoryNav");
        navGO.transform.SetParent(invPanel.transform, false);
        RectTransform navRt = navGO.AddComponent<RectTransform>();
        navRt.sizeDelta = new Vector2(0, 70);
        HorizontalLayoutGroup nav = navGO.AddComponent<HorizontalLayoutGroup>();
        nav.spacing = 8;
        nav.childAlignment = TextAnchor.MiddleCenter;
        nav.childControlWidth = false;
        nav.childControlHeight = false;
        nav.childForceExpandWidth = false;
        nav.childForceExpandHeight = false;

        CreateSmallButton(navGO.transform, "←", new Vector2(90, 60), () => SelectInventoryOffset(-1));
        CreateSmallButton(navGO.transform, "Inventory.Apply", new Vector2(220, 60), () => OnItemAction(selectedInvIndex), true);
        CreateSmallButton(navGO.transform, "→", new Vector2(90, 60), () => SelectInventoryOffset(1));

        Button discardBtn = CreateSmallButton(navGO.transform, "Inventory.Junk", new Vector2(160, 60), () => DiscardSelectedItem(), true);
        discardBtn.GetComponent<Image>().color = new Color(0.65f, 0.2f, 0.2f, 0.95f);
        if (discardButtonIcon != null)
        {
            discardBtn.GetComponent<Image>().sprite = discardButtonIcon;
            var txt = discardBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = "";
            var loc = discardBtn.GetComponentInChildren<LocalizedText>();
            if (loc != null) loc.enabled = false;
        }

        inventoryVisible = true;
    }

    Button CreateSmallButton(Transform parent, string label, Vector2 size, UnityEngine.Events.UnityAction action, bool localize = false)
    {
        GameObject btnGO = new GameObject(label);
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.65f, 0.95f);
        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(action);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        string displayText = localize ? Loc(label) : label;
        text.text = displayText;
        text.fontSize = 40;
        text.enableAutoSizing = true;
        text.fontSizeMin = 26;
        text.fontSizeMax = 46;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        if (localize)
        {
            LocalizedText loc = textGO.AddComponent<LocalizedText>();
            loc.key = label;
        }
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        return btn;
    }

    void SelectInventoryOffset(int offset)
    {
        if (playerInventory == null || playerInventory.items.Count == 0) return;
        selectedInvIndex = (selectedInvIndex + offset + playerInventory.items.Count) % playerInventory.items.Count;
        RefreshInventory();
    }

    void RefreshInventory()
    {
        if (inventoryContainer == null) return;

        foreach (GameObject btn in inventoryButtons)
            if (btn != null) Destroy(btn);
        inventoryButtons.Clear();

        UpdateEquippedText();

        if (!inventoryVisible) return;
        if (playerInventory == null || equipmentManager == null) return;

        if (playerInventory.items.Count == 0)
        {
            CreateInventoryCard(null, 0);
            return;
        }

        selectedInvIndex = Mathf.Clamp(selectedInvIndex, 0, playerInventory.items.Count - 1);
        CreateInventoryCard(playerInventory.items[selectedInvIndex], selectedInvIndex);
    }

    void CreateInventoryCard(InventoryItem invItem, int index)
    {
        foreach (Transform child in inventoryContainer)
            Destroy(child.gameObject);

        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(inventoryContainer, false);
        RectTransform contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(8, 8);
        contentRt.offsetMax = new Vector2(-8, -8);

        if (invItem == null || invItem.itemData == null)
        {
            TextMeshProUGUI emptyText = contentGO.AddComponent<TextMeshProUGUI>();
            emptyText.text = Loc("Inventory.Empty");
            emptyText.fontSize = 46;
            emptyText.enableAutoSizing = true;
            emptyText.fontSizeMin = 26;
            emptyText.fontSizeMax = 46;
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            return;
        }

        ItemData item = invItem.itemData;
        Image bg = inventoryContainer.GetComponent<Image>();
        if (bg != null)
            bg.color = Color.Lerp(new Color(0.05f, 0.05f, 0.08f, 0.85f), item.GetRarityColor(), 0.25f);

        HorizontalLayoutGroup hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(contentGO.transform, false);
        iconGO.AddComponent<CanvasRenderer>();
        Image icon = iconGO.AddComponent<Image>();
        Sprite itemIcon = item.icon;
#if UNITY_EDITOR
        if (itemIcon == null)
        {
            Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath($"Assets/_Project/Custom/Items/{item.itemName.Replace(" ", "")}.png");
            if (subAssets != null)
            {
                foreach (Object sub in subAssets)
                {
                    if (sub is Sprite s) { itemIcon = s; break; }
                }
            }
        }
#endif
        icon.sprite = itemIcon;
        icon.color = itemIcon == null ? item.GetRarityColor() : Color.white;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(110, 110);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(contentGO.transform, false);
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = $"{index + 1}/{playerInventory.items.Count}\n{item.itemName} x{invItem.quantity}\n{item.description}";
        text.fontSize = 46;
        text.enableAutoSizing = true;
        text.fontSizeMin = 24;
        text.fontSizeMax = 46;
        text.alignment = TextAlignmentOptions.Left;
        text.color = item.GetRarityColor();
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(560, 145);
    }

    void OnItemAction(int index)
    {
        if (playerInventory == null || equipmentManager == null) return;
        if (index < 0 || index >= playerInventory.items.Count) return;

        InventoryItem invItem = playerInventory.items[index];
        if (invItem.itemData == null) return;

        if (equipmentManager.IsEquipped(invItem.itemData))
        {
            equipmentManager.Unequip(invItem.itemData, playerInventory);
        }
        else if (equipmentManager.CanEquip(invItem.itemData))
        {
            var result = equipmentManager.Equip(invItem.itemData, playerInventory);
            if (result.success && result.replacedItem != null)
            {
                playerInventory.RemoveItem(result.replacedItem, 1);
                Debug.Log($"Auto-discarded replaced item: {result.replacedItem.itemName}");
            }
        }
        else if (invItem.itemData.restoreHp > 0)
        {
            playerCombat?.Heal(invItem.itemData.restoreHp);
            playerInventory.RemoveItem(invItem.itemData, 1);
        }
        else if (invItem.itemData.experienceReward > 0)
        {
            playerStats?.AddReward(invItem.itemData.experienceReward, 0);
            playerInventory.RemoveItem(invItem.itemData, 1);
        }

        UpdateStatsDisplay();
        RefreshInventory();
    }

    public void DiscardSelectedItem()
    {
        if (playerInventory == null || playerInventory.items.Count == 0) return;
        if (selectedInvIndex < 0 || selectedInvIndex >= playerInventory.items.Count) return;

        InventoryItem invItem = playerInventory.items[selectedInvIndex];
        if (invItem.itemData == null) return;

        if (equipmentManager != null && equipmentManager.IsEquipped(invItem.itemData))
            equipmentManager.Unequip(invItem.itemData, playerInventory);

        Inventory.NotifyItemTrashed(invItem);
        TrashEmanator.Instance?.AddItem(invItem.itemData, invItem.quantity);

        playerInventory.RemoveItem(invItem.itemData, invItem.quantity);
        Debug.Log($"Discarded: {invItem.itemData.itemName} x{invItem.quantity}");

        UpdateStatsDisplay();
        RefreshInventory();
    }

    void UpdateEquippedText()
    {
        if (equippedText == null || equipmentManager == null) return;

        string w = equipmentManager.weapon != null ? equipmentManager.weapon.itemName : "-";
        string a = equipmentManager.armor != null ? equipmentManager.armor.itemName : "-";
        string b = equipmentManager.boots != null ? equipmentManager.boots.itemName : "-";
        string ac = equipmentManager.accessory != null ? equipmentManager.accessory.itemName : "-";

        equippedText.text = string.Format(Loc("Inventory.Equipped"), w, a, b, ac);
    }

    void CreateStatisticsButton()
    {
        if (buttonsParent == null) return;

        GameObject btnGO = new GameObject("StatisticsButton");
        btnGO.transform.SetParent(buttonsParent, false);

        Image img = btnGO.AddComponent<Image>();
        
        // Если есть иконка, используем её, иначе создаём градиентный спрайт
        if (statisticsButtonIcon != null)
        {
            img.sprite = statisticsButtonIcon;
        }
        else
        {
            Color leftColor = new Color(1f, 0.5f, 0f, 1f);
            Color rightColor = new Color(1f, 0.84f, 0f, 1f);
            Color shadowColor = new Color(0.5f, 0.3f, 0f, 0.75f);
            img.sprite = CreateGradientPillSprite(165, 75, 150, 60, leftColor, rightColor, shadowColor, new Vector2(5, -5));
        }
        img.color = Color.white;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        // Если нет иконки, добавляем текст
        if (statisticsButtonIcon == null)
        {
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = Loc("Statistics.Title");
            textGO.AddComponent<LocalizedText>().key = "Statistics.Title";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;

            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        RectTransform btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(200, 60);

        btn.onClick.AddListener(OpenStatistics);
    }

    public void OpenStatistics()
    {
        HideInventory();
        if (statisticsPanel == null)
            CreateStatisticsPanel();
        
        if (statisticsPanel != null)
        {
            statisticsPanel.SetActive(true);
            RefreshStatistics();
        }
    }

    public void CloseStatistics()
    {
        if (statisticsPanel != null)
            statisticsPanel.SetActive(false);
        ShowInventory();
    }

    void CreateStatisticsPanel()
    {
        if (pausePanel == null) return;

        statisticsPanel = new GameObject("StatisticsPanel");
        statisticsPanel.transform.SetParent(pausePanel.transform, false);
        RectTransform srt = statisticsPanel.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(600, 700);

        Image bg = statisticsPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = statisticsPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(statisticsPanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = Loc("Statistics.Title");
        titleGO.AddComponent<LocalizedText>().key = "Statistics.Title";
        title.fontSize = 48;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.84f, 0f, 1f);
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 60);

        // Текст статистики
        GameObject statsGO = new GameObject("StatsText");
        statsGO.transform.SetParent(statisticsPanel.transform, false);
        statsGO.AddComponent<CanvasRenderer>();
        statisticsText = statsGO.AddComponent<TextMeshProUGUI>();
        statisticsText.text = Loc("Statistics.Loading");
        statisticsText.fontSize = 32;
        statisticsText.alignment = TextAlignmentOptions.TopLeft;
        statisticsText.color = Color.white;
        RectTransform statsRt = statsGO.GetComponent<RectTransform>();
        statsRt.sizeDelta = new Vector2(0, 400);

        // Кнопка закрытия
        CreateSmallButton(statisticsPanel.transform, "Common.Close", new Vector2(300, 60), CloseStatistics, true);

        statisticsPanel.SetActive(false);
    }

    void RefreshStatistics()
    {
        if (statisticsText == null) return;

        GameStatistics stats = GameStatistics.Instance;
        if (stats != null)
        {
            statisticsText.text = stats.GetStatisticsText();
        }
        else
        {
            statisticsText.text = Loc("Statistics.Unavailable");
        }
    }

    public void OpenBestiaryDescription(string enemyName, BestiaryData enemyData)
    {
        HideInventory();
        if (bestiaryDescriptionPanel == null)
            CreateBestiaryDescriptionPanel();

        if (bestiaryDescriptionPanel != null)
        {
            bestiaryDescriptionPanel.SetActive(true);
            bestiaryDescriptionPanel.transform.SetAsLastSibling();
            RefreshBestiaryDescription(enemyName, enemyData);
        }
    }

    public void CloseBestiaryDescription()
    {
        if (bestiaryDescriptionPanel != null)
            bestiaryDescriptionPanel.SetActive(false);
        ShowInventory();
    }

    void CreateBestiaryDescriptionPanel()
    {
        if (pausePanel == null) return;

        bestiaryDescriptionPanel = new GameObject("BestiaryDescriptionPanel");
        bestiaryDescriptionPanel.transform.SetParent(pausePanel.transform, false);
        bestiaryDescriptionPanel.transform.SetAsLastSibling();

        RectTransform prt = bestiaryDescriptionPanel.AddComponent<RectTransform>();
        // Панель на весь экран с небольшим отступом
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(40, 40);
        prt.offsetMax = new Vector2(-40, -40);

        Image bg = bestiaryDescriptionPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);

        // Иконка — большая, по центру сверху
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(bestiaryDescriptionPanel.transform, false);
        iconGO.AddComponent<CanvasRenderer>();
        descriptionIcon = iconGO.AddComponent<Image>();
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 1f);
        iconRt.anchorMax = new Vector2(0.5f, 1f);
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.anchoredPosition = new Vector2(0, -40);
        iconRt.sizeDelta = new Vector2(350, 350);

        // Заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(bestiaryDescriptionPanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        descriptionTitle = titleGO.AddComponent<TextMeshProUGUI>();
        descriptionTitle.text = Loc("Bestiary.Enemy");
        descriptionTitle.fontSize = 52;
        descriptionTitle.alignment = TextAlignmentOptions.Center;
        descriptionTitle.color = new Color(1f, 0.84f, 0f, 1f);
        descriptionTitle.fontStyle = FontStyles.Bold;
        descriptionTitle.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0, -430);
        titleRt.sizeDelta = new Vector2(0, 90);

        // Текст описания — большой, по центру
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(bestiaryDescriptionPanel.transform, false);
        descGO.AddComponent<CanvasRenderer>();
        descriptionText = descGO.AddComponent<TextMeshProUGUI>();
        descriptionText.text = Loc("Bestiary.NoDescription");
        descriptionText.fontSize = 36;
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
        descriptionText.color = Color.white;
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform descRt = descGO.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0, 0);
        descRt.anchorMax = new Vector2(1, 1);
        descRt.offsetMin = new Vector2(50, 120);
        descRt.offsetMax = new Vector2(-50, -530);

        // Кнопка закрытия — снизу по центру
        GameObject closeBtnGO = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(bestiaryDescriptionPanel.transform, false);
        closeBtnGO.AddComponent<CanvasRenderer>();
        Image closeImg = closeBtnGO.AddComponent<Image>();
        closeImg.color = new Color(0.2f, 0.45f, 0.65f, 0.95f);
        Button closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(CloseBestiaryDescription);
        RectTransform closeRt = closeBtnGO.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0);
        closeRt.anchorMax = new Vector2(0.5f, 0);
        closeRt.pivot = new Vector2(0.5f, 0);
        closeRt.anchoredPosition = new Vector2(0, 40);
        closeRt.sizeDelta = new Vector2(400, 80);

        GameObject closeTextGO = new GameObject("Text");
        closeTextGO.transform.SetParent(closeBtnGO.transform, false);
        closeTextGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
        closeText.text = Loc("Common.Close");
        closeTextGO.AddComponent<LocalizedText>().key = "Common.Close";
        closeText.fontSize = 40;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;
        RectTransform closeTextRt = closeTextGO.GetComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        closeTextRt.offsetMin = Vector2.zero;
        closeTextRt.offsetMax = Vector2.zero;

        bestiaryDescriptionPanel.SetActive(false);
    }

    void RefreshBestiaryDescription(string enemyName, BestiaryData enemyData)
    {
        if (descriptionTitle != null)
            descriptionTitle.text = enemyName;
        
        if (descriptionIcon != null)
        {
            if (enemyData != null && enemyData.enemyIcon != null)
            {
                descriptionIcon.sprite = enemyData.enemyIcon;
                descriptionIcon.color = Color.white;
            }
            else
            {
                descriptionIcon.sprite = null;
                descriptionIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
        
        if (descriptionText != null)
        {
            if (enemyData != null && !string.IsNullOrEmpty(enemyData.description))
                descriptionText.text = enemyData.description;
            else
                descriptionText.text = Loc("Bestiary.NoDescription");
        }
    }

    void OnValidate()
    {
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<Inventory>();
        if (equipmentManager == null)
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
    }
}

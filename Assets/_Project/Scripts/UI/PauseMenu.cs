using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
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

    [Header("Save Panel (auto-created)")]
    private GameObject savePanel;
    private readonly GameObject[] slotButtons = new GameObject[SaveManager.SlotCount + 1];
    private int selectedSlot = -1;

    [Header("Skill Tree Panel (auto-created)")]
    private GameObject skillTreePanel;

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

    private static PauseMenu instance;

    private bool isPaused;
    private bool inventoryVisible;
    private readonly string[] statNames = { "HP", "ATK", "DEF", "SPD", "LCK" };
    private readonly List<GameObject> inventoryButtons = new List<GameObject>();
    private int selectedInvIndex;

    void Awake()
    {
        // Синглтон: при повторной загрузке GameScene уничтожаем дубли (вместе с дублем Canvas)
        if (instance != null && instance != this)
        {
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

        // Делаем Canvas с UI постоянным между сценами (меню, HUD, цифры урона)
        if (pausePanel != null)
        {
            Canvas rootCanvas = pausePanel.GetComponentInParent<Canvas>(true);
            if (rootCanvas != null && rootCanvas.GetComponent<PersistentUI>() == null)
                rootCanvas.gameObject.AddComponent<PersistentUI>();
        }

        SceneManager.sceneLoaded += OnSceneLoadedRebind;

        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<Inventory>();
        
        // Если инвентаря нет, создаём его на Player GameObject
        if (playerInventory == null)
        {
            GameObject playerObj = playerStats != null ? playerStats.gameObject : (playerCombat != null ? playerCombat.gameObject : gameObject);
            playerInventory = playerObj.GetComponent<Inventory>();
            if (playerInventory == null)
                playerInventory = playerObj.AddComponent<Inventory>();
        }
        
        if (equipmentManager == null)
            equipmentManager = FindAnyObjectByType<EquipmentManager>();
        if (equipmentManager == null)
        {
            GameObject playerObj = playerStats != null ? playerStats.gameObject : (playerCombat != null ? playerCombat.gameObject : gameObject);
            equipmentManager = playerObj.GetComponent<EquipmentManager>();
            if (equipmentManager == null)
                equipmentManager = playerObj.AddComponent<EquipmentManager>();
        }

        if (buttonsParent == null)
            CreateStatButtons();

        if (inventoryContainer == null)
            CreateInventoryPanel();

        CreateSettingsPanel();
        CreateSettingsButton();

        CreateSavePanel();
        CreateSaveButton();

        CreateSkillTreePanel();
        CreateSkillTreeButton();

        CreateBestiaryPanel();
        CreateBestiaryButton();

        FindAndStyleResumeQuitButtons();

        AddStartingItems();

        Inventory.OnInventoryChanged += OnInventoryChangedHandler;
    }

    void OnDestroy()
    {
        Inventory.OnInventoryChanged -= OnInventoryChangedHandler;
        if (instance == this)
        {
            instance = null;
            SceneManager.sceneLoaded -= OnSceneLoadedRebind;
        }
    }

    void OnSceneLoadedRebind(Scene scene, LoadSceneMode mode)
    {
        if (instance != this) return;

        // Перепривязываем ссылки на объекты новой сцены
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null && playerStats != null)
            playerCombat = playerStats.GetComponent<AutoCombat>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<Inventory>();
        if (equipmentManager == null)
            equipmentManager = FindAnyObjectByType<EquipmentManager>();

        Debug.Log($"[PauseMenu] Scene loaded: {scene.name}, inventory items: {(playerInventory != null ? playerInventory.items.Count : 0)}");

        // Меню закрыто после смены сцены
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        Time.timeScale = 1f;
        CloseAllSubPanels();
        
        // Обновляем инвентарь после смены сцены
        RefreshInventory();
        UpdateStatsDisplay();
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
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
        {
            UpdateStatsDisplay();
            RefreshInventory();
            PlayPauseMusic();
        }
        else
        {
            CloseAllSubPanels();
            StopPauseMusic();
        }
    }

    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
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
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (savePanel != null) savePanel.SetActive(false);
        if (skillTreePanel != null) skillTreePanel.SetActive(false);
        if (bestiaryPanel != null) bestiaryPanel.SetActive(false);
        selectedSlot = -1;
        inventoryVisible = true;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void OpenSettings()
    {
        HideInventory();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (floatingJoystickToggle != null)
                floatingJoystickToggle.isOn = PlayerPrefs.GetInt("FloatingJoystick", 0) == 1;
            if (heartStyleToggle != null)
                heartStyleToggle.isOn = PlayerPrefs.GetInt(HealthBarUI.HealthBarStyleKey, 0) == 1;
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        ShowInventory();
    }

    public void OpenSavePanel()
    {
        HideInventory();
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
        if (skillTreePanel != null)
            skillTreePanel.SetActive(true);
    }

    public void CloseSkillTree()
    {
        if (skillTreePanel != null)
            skillTreePanel.SetActive(false);
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
        title.text = "Настройки";
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
        label.text = "Плавающий джойстик";
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
        heartLabel.text = "Сердечки вместо полосок";
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

        CreateSmallButton(settingsPanel.transform, "Закрыть", new Vector2(300, 60), CloseSettings);

        settingsPanel.SetActive(false);
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
        title.text = "Сохранение / Загрузка";
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
            text.text = isAuto ? "Автосохранение" : $"Слот {i}";
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
        infoText.text = "Выберите слот";
        infoText.fontSize = 24;
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        RectTransform infoRt = infoGO.GetComponent<RectTransform>();
        infoRt.sizeDelta = new Vector2(0, 40);

        CreateSmallButton(savePanel.transform, "Сохранить", new Vector2(220, 55), SaveSelectedSlot);
        CreateSmallButton(savePanel.transform, "Загрузить", new Vector2(220, 55), LoadSelectedSlot);
        CreateSmallButton(savePanel.transform, "Удалить", new Vector2(220, 55), DeleteSelectedSlot);
        CreateSmallButton(savePanel.transform, "Закрыть", new Vector2(220, 55), CloseSavePanel);

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
            string label = isAuto ? "Автосохранение" : $"Слот {i}";
            text.text = $"{label} {(hasSave ? "[есть]" : "[пусто]")}{(isSelected ? " <<" : "")}";
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
                string label = isAuto ? "Автосохранение" : $"Слот {selectedSlot}";
                infoText.text = $"{label}: {(hasSave ? "занят" : "пусто")}";
            }
            else
            {
                infoText.text = "Выберите слот";
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

        skillTreePanel = new GameObject("SkillTreePanel");
        skillTreePanel.transform.SetParent(pausePanel.transform, false);
        RectTransform srt = skillTreePanel.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(700, 500);

        Image bg = skillTreePanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = skillTreePanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(skillTreePanel.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "Дерево скиллов";
        title.fontSize = 48;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 60);

        GameObject hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(skillTreePanel.transform, false);
        hintGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI hint = hintGO.AddComponent<TextMeshProUGUI>();
        hint.text = "Здесь будет дерево скиллов. Заполним позже.";
        hint.fontSize = 28;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        RectTransform hintRt = hintGO.GetComponent<RectTransform>();
        hintRt.sizeDelta = new Vector2(0, 60);

        CreateSmallButton(skillTreePanel.transform, "Закрыть", new Vector2(300, 60), CloseSkillTree);

        skillTreePanel.SetActive(false);
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
        title.text = "Бестиарий";
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

        CreateSmallButton(bestiaryPanel.transform, "Закрыть", new Vector2(300, 60), CloseBestiary);

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
            emptyText.text = "Пока нет убитых врагов";
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
            text.text = $"{cleanEnemyName}\n{kv.Value} убито";
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
            descBtnText.text = "Описание";
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
        buttonsParent.gameObject.SetActive(playerStats != null && playerStats.skillPoints > 0);
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
        text.text = statName;
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

    void UpdateStatsDisplay()
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
                $"Level: {playerStats.level}\n" +
                $"XP: {playerStats.experience} / {playerStats.experienceToNextLevel}\n" +
                $"Gold: {playerStats.gold}\n" +
                $"Skill Points: {playerStats.skillPoints}\n\n" +
                $"HP: {playerStats.hp} / {playerStats.maxHp}\n" +
                $"ATK: {playerStats.atk}\n" +
                $"DEF: {playerStats.def} ({playerStats.GetDamageReductionPercent():F0}%)\n" +
                $"SPD: {playerStats.spd}\n" +
                $"LCK: {playerStats.lck}\n" +
                $"AtkSpd: {playerStats.atkSpd} ({(1f / playerStats.GetAttackCooldownMultiplier() - 1f) * 100f:F0}%)\n" +
                $"Lethality: {playerStats.GetEffectiveLethality()} ({playerStats.lethality} + {playerStats.GetEffectiveLethality() - playerStats.lethality} from LCK)\n\n" +
                $"Regen: {playerStats.GetHpRegenPerSecond():F1} HP/sec\n" +
                $"Crit: {playerStats.GetCritChance() * 100f:F0}%\n\n" +
                $"Junk: {junkCount} items\n" +
                $"Sell Value: {junkValue}g";
        }

        if (skillPointsText != null)
            skillPointsText.text = $"Skill Points: {playerStats.skillPoints}";

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
        CreateSmallButton(navGO.transform, "Применить", new Vector2(220, 60), () => OnItemAction(selectedInvIndex));
        CreateSmallButton(navGO.transform, "→", new Vector2(90, 60), () => SelectInventoryOffset(1));

        Button discardBtn = CreateSmallButton(navGO.transform, "Мусор", new Vector2(160, 60), () => DiscardSelectedItem());
        discardBtn.GetComponent<Image>().color = new Color(0.65f, 0.2f, 0.2f, 0.95f);
        if (discardButtonIcon != null)
        {
            discardBtn.GetComponent<Image>().sprite = discardButtonIcon;
            var txt = discardBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = "";
        }

        inventoryVisible = true;
    }

    Button CreateSmallButton(Transform parent, string label, Vector2 size, UnityEngine.Events.UnityAction action)
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
        text.text = label;
        text.fontSize = 40;
        text.enableAutoSizing = true;
        text.fontSizeMin = 26;
        text.fontSizeMax = 46;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
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
            emptyText.text = "Inventory is empty";
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

        equippedText.text = $"Equipped:\n  Weapon: {w}\n  Armor: {a}\n  Boots: {b}\n  Accessory: {ac}";
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
            text.text = "СТАТИСТИКА";
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
        title.text = "СТАТИСТИКА";
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
        statisticsText.text = "Загрузка...";
        statisticsText.fontSize = 32;
        statisticsText.alignment = TextAlignmentOptions.TopLeft;
        statisticsText.color = Color.white;
        RectTransform statsRt = statsGO.GetComponent<RectTransform>();
        statsRt.sizeDelta = new Vector2(0, 400);

        // Кнопка закрытия
        CreateSmallButton(statisticsPanel.transform, "Закрыть", new Vector2(300, 60), CloseStatistics);

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
            statisticsText.text = "Статистика недоступна";
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
        RectTransform prt = bestiaryDescriptionPanel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(700, 600);

        Image bg = bestiaryDescriptionPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        VerticalLayoutGroup vlg = bestiaryDescriptionPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Заголовок с иконкой
        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(bestiaryDescriptionPanel.transform, false);
        headerGO.AddComponent<CanvasRenderer>();
        HorizontalLayoutGroup hlg = headerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        RectTransform headerRt = headerGO.GetComponent<RectTransform>();
        headerRt.sizeDelta = new Vector2(0, 120);

        // Иконка
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(headerGO.transform, false);
        iconGO.AddComponent<CanvasRenderer>();
        descriptionIcon = iconGO.AddComponent<Image>();
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(100, 100);

        // Заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(headerGO.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        descriptionTitle = titleGO.AddComponent<TextMeshProUGUI>();
        descriptionTitle.text = "Враг";
        descriptionTitle.fontSize = 40;
        descriptionTitle.alignment = TextAlignmentOptions.Left;
        descriptionTitle.color = new Color(1f, 0.84f, 0f, 1f);
        descriptionTitle.enableAutoSizing = false;
        descriptionTitle.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(500, 100);

        // Текст описания
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(bestiaryDescriptionPanel.transform, false);
        descGO.AddComponent<CanvasRenderer>();
        descriptionText = descGO.AddComponent<TextMeshProUGUI>();
        descriptionText.text = "Описание отсутствует";
        descriptionText.fontSize = 28;
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
        descriptionText.color = Color.white;
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform descRt = descGO.GetComponent<RectTransform>();
        descRt.sizeDelta = new Vector2(0, 300);

        // Кнопка закрытия
        CreateSmallButton(bestiaryDescriptionPanel.transform, "Закрыть", new Vector2(300, 60), CloseBestiaryDescription);

        bestiaryDescriptionPanel.SetActive(false);
    }

    void RefreshBestiaryDescription(string enemyName, BestiaryData enemyData)
    {
        if (descriptionTitle != null)
            descriptionTitle.text = enemyName;
        
        if (descriptionIcon != null)
        {
            if (enemyData != null && enemyData.enemyIcon != null)
                descriptionIcon.sprite = enemyData.enemyIcon;
            else
                descriptionIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        
        if (descriptionText != null)
        {
            if (enemyData != null && !string.IsNullOrEmpty(enemyData.description))
                descriptionText.text = enemyData.description;
            else
                descriptionText.text = "Описание отсутствует";
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

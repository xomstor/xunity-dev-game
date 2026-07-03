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

    [Header("Settings Panel (auto-created)")]
    private GameObject settingsPanel;
    private Toggle floatingJoystickToggle;

    private bool isPaused;
    private bool inventoryVisible;
    private readonly string[] statNames = { "HP", "ATK", "DEF", "SPD", "LCK" };
    private readonly List<GameObject> inventoryButtons = new List<GameObject>();
    private int selectedInvIndex;

    void Awake()
    {
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<Inventory>();
        if (playerInventory == null)
            playerInventory = gameObject.AddComponent<Inventory>();
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

        AddStartingItems();
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
        }
    }

    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (floatingJoystickToggle != null)
                floatingJoystickToggle.isOn = PlayerPrefs.GetInt("FloatingJoystick", 0) == 1;
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnFloatingJoystickToggle(bool value)
    {
        Debug.Log($"[PauseMenu] OnFloatingJoystickToggle called with value={value}");
        VirtualJoystick joystick = null;
        VirtualJoystick[] joysticks = FindObjectsOfType<VirtualJoystick>();
        foreach (VirtualJoystick candidate in joysticks)
        {
            if (candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy)
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
        srt.sizeDelta = new Vector2(500, 300);

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

        CreateSmallButton(settingsPanel.transform, "Закрыть", new Vector2(300, 60), CloseSettings);

        settingsPanel.SetActive(false);
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
                $"DEF: {playerStats.def}\n" +
                $"SPD: {playerStats.spd}\n" +
                $"LCK: {playerStats.lck}\n\n" +
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
        CreateSmallButton(navGO.transform, "Применить", new Vector2(260, 60), () => OnItemAction(selectedInvIndex));
        CreateSmallButton(navGO.transform, "→", new Vector2(90, 60), () => SelectInventoryOffset(1));

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
            equipmentManager.Equip(invItem.itemData, playerInventory);
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

    void UpdateEquippedText()
    {
        if (equippedText == null || equipmentManager == null) return;

        string w = equipmentManager.weapon != null ? equipmentManager.weapon.itemName : "-";
        string a = equipmentManager.armor != null ? equipmentManager.armor.itemName : "-";
        string b = equipmentManager.boots != null ? equipmentManager.boots.itemName : "-";
        string ac = equipmentManager.accessory != null ? equipmentManager.accessory.itemName : "-";

        equippedText.text = $"Equipped:\n  Weapon: {w}\n  Armor: {a}\n  Boots: {b}\n  Accessory: {ac}";
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

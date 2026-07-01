using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

    private bool isPaused;
    private bool inventoryVisible;
    private readonly string[] statNames = { "HP", "ATK", "DEF", "SPD", "LCK" };
    private readonly List<GameObject> inventoryButtons = new List<GameObject>();
    private int selectedInvIndex = -1;

    void Awake()
    {
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<Inventory>();
        if (equipmentManager == null)
            equipmentManager = FindAnyObjectByType<EquipmentManager>();

        if (buttonsParent == null)
            CreateStatButtons();

        if (inventoryContainer == null)
            CreateInventoryPanel();
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
                $"Crit: {playerStats.GetCritChance() * 100f:F0}%";
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
        invRt.anchorMin = new Vector2(1f, 0.5f);
        invRt.anchorMax = new Vector2(1f, 0.5f);
        invRt.pivot = new Vector2(1f, 0.5f);
        invRt.anchoredPosition = new Vector2(-20, 0);
        float invWidth = Mathf.Min(420f, panelWidth * 0.38f);
        float invHeight = panelHeight * 0.82f;
        invRt.sizeDelta = new Vector2(invWidth, invHeight);

        Image invBg = invPanel.AddComponent<Image>();
        invBg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        invBg.raycastTarget = true;

        VerticalLayoutGroup vlg = invPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(8, 8, 8, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject eqGO = new GameObject("EquippedText");
        eqGO.transform.SetParent(invPanel.transform, false);
        eqGO.AddComponent<CanvasRenderer>();
        equippedText = eqGO.AddComponent<TextMeshProUGUI>();
        equippedText.fontSize = 18;
        equippedText.alignment = TextAlignmentOptions.TopLeft;
        equippedText.color = new Color(1f, 0.85f, 0.3f, 1f);
        RectTransform eqRt = eqGO.GetComponent<RectTransform>();
        eqRt.sizeDelta = new Vector2(0, 80);

        GameObject scrollGO = new GameObject("InventoryScroll");
        scrollGO.transform.SetParent(invPanel.transform, false);
        RectTransform scrollRt = scrollGO.AddComponent<RectTransform>();
        scrollRt.sizeDelta = new Vector2(0, invHeight - 120);

        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20f;

        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        RectTransform contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup contentVlg = contentGO.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 3;
        contentVlg.padding = new RectOffset(4, 4, 4, 4);
        contentVlg.childAlignment = TextAnchor.UpperCenter;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = false;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        inventoryContainer = contentGO.transform;

        GameObject toggleBtnGO = new GameObject("ToggleInventoryBtn");
        toggleBtnGO.transform.SetParent(invPanel.transform, false);
        RectTransform toggleRt = toggleBtnGO.AddComponent<RectTransform>();
        toggleRt.sizeDelta = new Vector2(0, 36);
        toggleBtnGO.AddComponent<CanvasRenderer>();
        Image toggleImg = toggleBtnGO.AddComponent<Image>();
        toggleImg.color = new Color(0.2f, 0.4f, 0.6f, 0.9f);
        Button toggleBtn = toggleBtnGO.AddComponent<Button>();
        GameObject toggleTextGO = new GameObject("Text");
        toggleTextGO.transform.SetParent(toggleBtnGO.transform, false);
        toggleTextGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI toggleText = toggleTextGO.AddComponent<TextMeshProUGUI>();
        toggleText.text = "Inventory";
        toggleText.alignment = TextAlignmentOptions.Center;
        toggleText.fontSize = 20;
        toggleText.color = Color.white;
        RectTransform ttRt = toggleTextGO.GetComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero;
        ttRt.anchorMax = Vector2.one;
        ttRt.offsetMin = Vector2.zero;
        ttRt.offsetMax = Vector2.zero;
        toggleBtn.onClick.AddListener(() => { inventoryVisible = !inventoryVisible; RefreshInventory(); });

        inventoryVisible = true;
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

        for (int i = 0; i < playerInventory.items.Count; i++)
        {
            InventoryItem invItem = playerInventory.items[i];
            if (invItem.itemData == null) continue;

            bool isEquipped = equipmentManager.IsEquipped(invItem.itemData);
            bool canEquip = equipmentManager.CanEquip(invItem.itemData);

            GameObject row = CreateInventoryRow(invItem, i, isEquipped, canEquip);
            inventoryButtons.Add(row);
        }

        if (inventoryButtons.Count == 0)
        {
            GameObject empty = new GameObject("EmptyMsg");
            empty.transform.SetParent(inventoryContainer, false);
            empty.AddComponent<CanvasRenderer>();
            TextMeshProUGUI emptyText = empty.AddComponent<TextMeshProUGUI>();
            emptyText.text = "Inventory is empty";
            emptyText.fontSize = 18;
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            RectTransform emptyRt = empty.GetComponent<RectTransform>();
            emptyRt.sizeDelta = new Vector2(0, 40);
            inventoryButtons.Add(empty);
        }
    }

    GameObject CreateInventoryRow(InventoryItem invItem, int index, bool isEquipped, bool canEquip)
    {
        ItemData item = invItem.itemData;

        GameObject row = new GameObject($"InvItem_{item.itemName}");
        row.transform.SetParent(inventoryContainer, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, 52);
        row.AddComponent<CanvasRenderer>();
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = Color.Lerp(new Color(0.12f, 0.12f, 0.16f, 0.9f), item.GetRarityColor(), 0.25f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(6, 6, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(row.transform, false);
        iconGO.AddComponent<CanvasRenderer>();
        Image icon = iconGO.AddComponent<Image>();
        icon.sprite = item.icon;
        icon.color = item.icon == null ? item.GetRarityColor() : Color.white;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(36, 36);

        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(row.transform, false);
        nameGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = $"{item.itemName}";
        if (invItem.quantity > 1)
            nameText.text += $" x{invItem.quantity}";
        nameText.fontSize = 18;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = item.GetRarityColor();
        nameText.raycastTarget = false;
        RectTransform nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.sizeDelta = new Vector2(180, 40);

        if (canEquip || isEquipped)
        {
            GameObject btnGO = new GameObject(isEquipped ? "UnequipBtn" : "EquipBtn");
            btnGO.transform.SetParent(row.transform, false);
            btnGO.AddComponent<CanvasRenderer>();
            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.color = isEquipped ? new Color(0.7f, 0.2f, 0.2f, 0.9f) : new Color(0.2f, 0.6f, 0.3f, 0.9f);
            Button btn = btnGO.AddComponent<Button>();
            RectTransform btnRt = btnGO.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(90, 36);

            GameObject btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            btnTextGO.AddComponent<CanvasRenderer>();
            TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnText.text = isEquipped ? "Снять" : "Надеть";
            btnText.fontSize = 16;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            RectTransform btRt = btnTextGO.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;

            int capturedIndex = index;
            btn.onClick.AddListener(() => OnItemAction(capturedIndex));
        }

        return row;
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
        else
        {
            if (playerInventory.GetItemCount(invItem.itemData) > 0)
            {
                playerInventory.RemoveItem(invItem.itemData, 1);
                equipmentManager.Equip(invItem.itemData, playerInventory);
            }
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

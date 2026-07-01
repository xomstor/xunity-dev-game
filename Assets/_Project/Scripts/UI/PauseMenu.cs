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

    private bool isPaused;
    private readonly string[] statNames = { "HP", "ATK", "DEF", "SPD", "LCK" };

    void Awake()
    {
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();

        if (buttonsParent == null)
            CreateStatButtons();
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
            UpdateStatsDisplay();
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
        rt.sizeDelta = new Vector2(400, 70);

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

        Image img = btnGO.AddComponent<Image>();
        img.sprite = CreateCircleSprite(64, Color.white);
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "+";
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 28;
        text.fontStyle = FontStyles.Bold;

        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        RectTransform btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(50, 50);

        int capturedIndex = index;
        btn.onClick.AddListener(() => IncreaseStat(capturedIndex));
    }

    Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = dist <= radius ? color : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
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

    void OnValidate()
    {
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
    }
}

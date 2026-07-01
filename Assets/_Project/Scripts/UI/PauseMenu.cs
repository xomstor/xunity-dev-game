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
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, -120);
        rt.sizeDelta = new Vector2(300, 250);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = $"+ {statName}";
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 24;

        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        RectTransform btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(0, 45);

        int capturedIndex = index;
        btn.onClick.AddListener(() => IncreaseStat(capturedIndex));
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

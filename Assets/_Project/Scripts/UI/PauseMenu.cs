using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public TextMeshProUGUI statsText;

    [Header("References")]
    public PlayerStats playerStats;
    public AutoCombat playerCombat;

    private bool isPaused;

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

    void UpdateStatsDisplay()
    {
        if (statsText == null || playerStats == null) return;

        statsText.text =
            $"Level: {playerStats.level}\n" +
            $"XP: {playerStats.experience}\n\n" +
            $"HP: {playerStats.hp} / {playerStats.maxHp}\n" +
            $"ATK: {playerStats.atk}\n" +
            $"DEF: {playerStats.def}\n" +
            $"SPD: {playerStats.spd}\n" +
            $"LCK: {playerStats.lck}";
    }

    void OnValidate()
    {
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
    }
}

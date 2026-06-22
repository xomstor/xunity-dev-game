using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("HP")]
    public Slider hpBar;
    public TextMeshProUGUI hpText;

    [Header("Exp")]
    public Slider expBar;
    public TextMeshProUGUI levelText;

    [Header("Gold")]
    public TextMeshProUGUI goldText;

    [Header("Attack Button")]
    public Button attackButton;

    private PlayerController player;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (attackButton != null)
        {
            attackButton.onClick.AddListener(OnAttackButtonPressed);
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (hpBar != null)
        {
            hpBar.value = (float)player.currentHP / player.maxHP;
        }
        if (hpText != null)
        {
            hpText.text = $"{player.currentHP}/{player.maxHP}";
        }

        if (GameManager.Instance != null)
        {
            int level = GameManager.Instance.PlayerLevel;
            int exp = GameManager.Instance.PlayerExp;
            int expNeeded = level * 100;

            if (expBar != null) expBar.value = (float)exp / expNeeded;
            if (levelText != null) levelText.text = $"Lv.{level}";
            if (goldText != null) goldText.text = $"{GameManager.Instance.PlayerGold}g";
        }
    }

    private void OnAttackButtonPressed()
    {
        player?.ManualAttack();
    }
}

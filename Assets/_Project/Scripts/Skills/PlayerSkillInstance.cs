using UnityEngine;

public abstract class PlayerSkillInstance : MonoBehaviour
{
    [Header("Skill Data")]
    public SkillData Data;

    [Header("Selection")]
    [Tooltip("If false, the skill button is hidden.")]
    [SerializeField] private bool isSelected = true;

    [SerializeField] protected int level = 1;
    [SerializeField] protected int talentPoints = 1;
    [SerializeField] protected int totalKills = 0;

    protected float cooldownTimer;
    protected PlayerStats playerStats;

    public event System.Action OnChanged;
    public static event System.Action<PlayerSkillInstance> OnSelectionChanged;

    public int Level => level;
    public int TalentPoints => talentPoints;
    public int TotalKills => totalKills;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value) return;
            isSelected = value;
            OnChanged?.Invoke();
            OnSelectionChanged?.Invoke(this);
        }
    }
    public int KillsToNextPoint => Data != null ? Data.killsPerTalentPoint - (totalKills % Data.killsPerTalentPoint) : 0;

    protected string SaveKeyPrefix => Data != null ? Data.skillName : GetType().Name;

    protected virtual void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        EnsureManager().RegisterSkill(this);
        Load();
    }

    protected virtual void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public abstract bool TryUse();

    public int GetUpgradeCost()
    {
        if (Data == null) return 1;
        return Data.GetUpgradeCost(level);
    }

    public bool CanUpgrade()
    {
        if (Data == null) return false;
        return level < Data.maxLevel && talentPoints >= GetUpgradeCost();
    }

    public bool TryUpgrade()
    {
        if (Data == null) return false;
        if (level >= Data.maxLevel) return false;
        int cost = GetUpgradeCost();
        if (talentPoints < cost) return false;

        talentPoints -= cost;
        level++;
        Save();
        OnChanged?.Invoke();
        Debug.Log($"[{GetType().Name}] {SaveKeyPrefix} upgraded to level {level}. Damage: {GetCurrentDamage()}, Cooldown: {GetCurrentCooldown():F2}s, TP left: {talentPoints}");
        return true;
    }

    public void RegisterKill()
    {
        if (Data == null) return;
        totalKills++;
        int newPoints = totalKills / Data.killsPerTalentPoint - (totalKills - 1) / Data.killsPerTalentPoint;
        if (newPoints > 0)
        {
            talentPoints += newPoints;
            Debug.Log($"[{GetType().Name}] Talent point earned for {SaveKeyPrefix}! Total TP: {talentPoints}, kills: {totalKills}");
        }
        Save();
        OnChanged?.Invoke();
    }

    public int GetCurrentDamage()
    {
        if (Data == null) return 0;
        int playerDamage = 0;
        if (Data.usePlayerStats && playerStats != null)
        {
            bool isCrit;
            playerDamage = playerStats.GetDamage(out isCrit, 0);
        }
        return Data.GetDamage(level, playerDamage);
    }

    public float GetCurrentCooldown()
    {
        if (Data == null) return float.MaxValue;
        return Data.GetCooldown(level);
    }

    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);
    public float CooldownProgress => GetCurrentCooldown() <= 0f ? 0f : Mathf.Clamp01(cooldownTimer / GetCurrentCooldown());

    protected PlayerSkillsManager EnsureManager()
    {
        var manager = GetComponent<PlayerSkillsManager>();
        if (manager == null)
            manager = gameObject.AddComponent<PlayerSkillsManager>();
        return manager;
    }

    protected void Save()
    {
        PlayerPrefs.SetInt($"{SaveKeyPrefix}_Level", level);
        PlayerPrefs.SetInt($"{SaveKeyPrefix}_TalentPoints", talentPoints);
        PlayerPrefs.SetInt($"{SaveKeyPrefix}_TotalKills", totalKills);
        PlayerPrefs.Save();
    }

    protected void Load()
    {
        level = PlayerPrefs.GetInt($"{SaveKeyPrefix}_Level", 1);
        talentPoints = PlayerPrefs.GetInt($"{SaveKeyPrefix}_TalentPoints", 1);
        totalKills = PlayerPrefs.GetInt($"{SaveKeyPrefix}_TotalKills", 0);
        if (Data != null)
            level = Mathf.Clamp(level, 1, Data.maxLevel);
    }
}

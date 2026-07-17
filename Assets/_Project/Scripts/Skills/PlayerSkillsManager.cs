using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillsManager : MonoBehaviour
{
    [SerializeField] private List<PlayerSkillInstance> skills = new List<PlayerSkillInstance>();
    [SerializeField] private PlayerSkillInstance[] loadoutSlots = new PlayerSkillInstance[4];
    [SerializeField] private bool[] slotEnabled = new bool[4] { true, true, true, true };

    public IReadOnlyList<PlayerSkillInstance> Skills => skills;
    public const int SlotCount = 4;
    public IReadOnlyList<PlayerSkillInstance> LoadoutSlots => loadoutSlots;
    public event System.Action<int> OnSlotChanged;
    public event System.Action OnChanged;

    public PlayerSkillInstance SelectedSkill
    {
        get
        {
            foreach (var skill in skills)
                if (skill.IsSelected) return skill;
            return null;
        }
    }

    public void SelectSkill(PlayerSkillInstance skill)
    {
        if (skill == null) return;
        foreach (var s in skills)
            s.IsSelected = (s == skill);
        OnChanged?.Invoke();
    }

    public PlayerSkillInstance GetSlot(int index)
    {
        EnsureSlotCapacity();
        return index >= 0 && index < loadoutSlots.Length ? loadoutSlots[index] : null;
    }

    public bool IsSlotEnabled(int index)
    {
        EnsureSlotCapacity();
        return index >= 0 && index < slotEnabled.Length && slotEnabled[index];
    }

    public void SetSlot(int index, PlayerSkillInstance skill)
    {
        EnsureSlotCapacity();
        if (index < 0 || index >= SlotCount) return;
        if (skill != null && !skills.Contains(skill)) return;
        loadoutSlots[index] = skill;
        SaveLoadout();
        OnSlotChanged?.Invoke(index);
        OnChanged?.Invoke();
    }

    public void SetSlotEnabled(int index, bool enabled)
    {
        EnsureSlotCapacity();
        if (index < 0 || index >= SlotCount) return;
        slotEnabled[index] = enabled;
        SaveLoadout();
        OnSlotChanged?.Invoke(index);
        OnChanged?.Invoke();
    }

    public bool TryUseSlot(int index)
    {
        PlayerSkillInstance skill = GetSlot(index);
        return IsSlotEnabled(index) && skill != null && skill.TryUse();
    }

    public void RegisterSkill(PlayerSkillInstance skill)
    {
        if (skill == null) return;
        if (!skills.Contains(skill))
            skills.Add(skill);
        skill.OnChanged -= OnSkillChanged;
        skill.OnChanged += OnSkillChanged;
        OnChanged?.Invoke();
    }

    public void UnregisterSkill(PlayerSkillInstance skill)
    {
        if (skill == null) return;
        skills.Remove(skill);
        skill.OnChanged -= OnSkillChanged;
        OnChanged?.Invoke();
    }

    public void RegisterKill()
    {
        foreach (var skill in skills)
            skill.RegisterKill();
    }

    void OnSkillChanged() => OnChanged?.Invoke();

    void Awake()
    {
        EnsureSlotCapacity();
    }

    void Start()
    {
        EnsureSlotCapacity();
        LoadLoadout();
    }

    void EnsureSlotCapacity()
    {
        if (loadoutSlots == null)
            loadoutSlots = new PlayerSkillInstance[SlotCount];
        else if (loadoutSlots.Length != SlotCount)
            System.Array.Resize(ref loadoutSlots, SlotCount);

        int previousEnabledLength = slotEnabled != null ? slotEnabled.Length : 0;
        if (slotEnabled == null)
            slotEnabled = new bool[SlotCount];
        else if (slotEnabled.Length != SlotCount)
            System.Array.Resize(ref slotEnabled, SlotCount);
        for (int i = previousEnabledLength; i < SlotCount; i++)
            slotEnabled[i] = true;
    }

    void SaveLoadout()
    {
        EnsureSlotCapacity();
        for (int i = 0; i < SlotCount; i++)
        {
            string skillName = loadoutSlots[i] != null && loadoutSlots[i].Data != null ? loadoutSlots[i].Data.skillName : string.Empty;
            PlayerPrefs.SetString($"EirHold_SkillSlot_{i}", skillName);
            PlayerPrefs.SetInt($"EirHold_SkillSlotEnabled_{i}", slotEnabled[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    void LoadLoadout()
    {
        EnsureSlotCapacity();
        bool hasNewLayout = PlayerPrefs.HasKey("EirHold_SkillSlot_3") || PlayerPrefs.HasKey("EirHold_SkillLayoutVersion");
        for (int i = 0; i < SlotCount; i++)
        {
            slotEnabled[i] = PlayerPrefs.GetInt($"EirHold_SkillSlotEnabled_{i}", 1) == 1;
            string skillName = PlayerPrefs.GetString($"EirHold_SkillSlot_{i}", string.Empty);
            loadoutSlots[i] = FindSkill(skillName);
        }

        if (!hasNewLayout || CountAssignedSlots() == 0)
        {
            MigrateLegacyLoadout();
            SaveLoadout();
        }
        else
        {
            PlayerPrefs.SetInt("EirHold_SkillLayoutVersion", 2);
            PlayerPrefs.Save();
        }
        OnChanged?.Invoke();
    }

    PlayerSkillInstance FindSkill(string skillName)
    {
        if (string.IsNullOrEmpty(skillName)) return null;
        foreach (PlayerSkillInstance skill in skills)
            if (skill != null && skill.Data != null && skill.Data.skillName == skillName)
                return skill;
        return null;
    }

    int CountAssignedSlots()
    {
        int count = 0;
        foreach (PlayerSkillInstance skill in loadoutSlots)
            if (skill != null) count++;
        return count;
    }

    void MigrateLegacyLoadout()
    {
        PlayerSkillInstance[] previous = (PlayerSkillInstance[])loadoutSlots.Clone();
        for (int i = 0; i < SlotCount; i++)
            loadoutSlots[i] = i < previous.Length ? previous[i] : null;

        PlayerSkillInstance fireball = FindSkill("Fireball");
        if (fireball != null)
            loadoutSlots[0] = fireball;

        int nextSlot = 1;
        foreach (PlayerSkillInstance skill in skills)
        {
            if (skill == null || skill == fireball) continue;
            bool alreadyAssigned = false;
            foreach (PlayerSkillInstance assigned in loadoutSlots)
                if (assigned == skill) alreadyAssigned = true;
            if (alreadyAssigned || nextSlot >= SlotCount) continue;
            loadoutSlots[nextSlot++] = skill;
        }
    }
}

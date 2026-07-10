using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillsManager : MonoBehaviour
{
    [SerializeField] private List<PlayerSkillInstance> skills = new List<PlayerSkillInstance>();

    public IReadOnlyList<PlayerSkillInstance> Skills => skills;
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
}

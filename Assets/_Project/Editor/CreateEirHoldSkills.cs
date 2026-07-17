#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateEirHoldSkills
{
    [MenuItem("Tools/EirHold/Create Four Skills")]
    public static void CreateSkills()
    {
        EnsureFolder("Assets/_Project/Resources", "Skills");
        CreateSkill("IceSpike", "Ice Spike", "Launches a freezing spike that damages enemies.", SkillBehavior.Projectile, ElementalType.Ice, 24, 8f);
        CreateSkill("StoneBrick", "Stone Brick", "Creates a temporary stone barrier.", SkillBehavior.SpawnObject, ElementalType.Earth, 12, 14f);
        CreateSkill("TarPuddle", "Tar Puddle", "Creates a damaging area that slows enemies.", SkillBehavior.AreaEffect, ElementalType.Cursed, 10, 16f);
        CreateSkill("SnakeBox", "Snake Box", "Creates a decoy and hides the player briefly.", SkillBehavior.Decoy, ElementalType.Poison, 0, 20f);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("EirHold Skills", "Four SkillData assets created in Assets/_Project/Resources/Skills.", "OK");
    }

    static void CreateSkill(string fileName, string skillName, string description, SkillBehavior behavior, ElementalType element, int damage, float cooldown)
    {
        string path = $"Assets/_Project/Resources/Skills/{fileName}.asset";
        SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<SkillData>();
            AssetDatabase.CreateAsset(skill, path);
        }
        skill.skillName = skillName;
        skill.description = description;
        skill.behavior = behavior;
        skill.projectileElement = element;
        skill.baseDamage = damage;
        skill.usePlayerStats = damage <= 0;
        skill.baseCooldown = cooldown;
        skill.minCooldown = Mathf.Max(1f, cooldown * 0.25f);
        skill.effectRadius = behavior == SkillBehavior.AreaEffect ? 2.2f : 1.5f;
        skill.effectDuration = behavior == SkillBehavior.AreaEffect ? 5f : 3f;
        skill.effectTickRate = 0.5f;
        skill.slowMultiplier = behavior == SkillBehavior.AreaEffect ? 0.45f : 1f;
        EditorUtility.SetDirty(skill);
    }

    static void EnsureFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{folder}"))
            AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif

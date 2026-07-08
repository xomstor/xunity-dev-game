using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Переносит полный статус игрока (статы, инвентарь, экипировка) между сценами в памяти.
/// Создаётся автоматически при старте игры. Не зависит от сейв-файлов.
/// </summary>
public class PlayerStateTransfer : MonoBehaviour
{
    public static PlayerStateTransfer Instance { get; private set; }

    private class Snapshot
    {
        public int hp, maxHp, atk, def, spd, lck, atkSpd, lethality;
        public int level, experience, gold, experienceToNextLevel, skillPoints;
        public List<(ItemData item, int qty)> items = new List<(ItemData, int)>();
        public ItemData weapon, armor, boots, accessory;
    }

    private Snapshot snapshot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("PlayerStateTransfer");
        go.AddComponent<PlayerStateTransfer>();
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneUnloaded(Scene scene)
    {
        Capture();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (snapshot == null) return;
        StartCoroutine(ApplyNextFrame());
    }

    IEnumerator ApplyNextFrame()
    {
        // Ждём несколько кадров, чтобы Awake/Start новой сцены отработали полностью
        yield return null;
        yield return null;
        yield return null;
        Apply();
    }

    public void Capture()
    {
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();
        if (stats == null) return;

        Snapshot s = new Snapshot
        {
            hp = stats.hp, maxHp = stats.maxHp,
            atk = stats.atk, def = stats.def, spd = stats.spd, lck = stats.lck,
            atkSpd = stats.atkSpd, lethality = stats.lethality,
            level = stats.level, experience = stats.experience, gold = stats.gold,
            experienceToNextLevel = stats.experienceToNextLevel,
            skillPoints = stats.skillPoints
        };

        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv != null)
        {
            foreach (InventoryItem it in inv.items)
            {
                if (it.itemData != null)
                    s.items.Add((it.itemData, it.quantity));
            }
        }

        EquipmentManager eq = FindAnyObjectByType<EquipmentManager>();
        if (eq != null)
        {
            s.weapon = eq.weapon;
            s.armor = eq.armor;
            s.boots = eq.boots;
            s.accessory = eq.accessory;
        }

        snapshot = s;
        Debug.Log($"[PlayerStateTransfer] Captured: lvl={s.level}, hp={s.hp}/{s.maxHp}, gold={s.gold}, items={s.items.Count}");
    }

    public void Apply()
    {
        if (snapshot == null) return;

        PlayerStats stats = FindAnyObjectByType<PlayerStats>();
        if (stats == null)
        {
            Debug.LogWarning("[PlayerStateTransfer] PlayerStats not found!");
            return;
        }

        stats.hp = snapshot.hp;
        stats.maxHp = snapshot.maxHp;
        stats.atk = snapshot.atk;
        stats.def = snapshot.def;
        stats.spd = snapshot.spd;
        stats.lck = snapshot.lck;
        stats.atkSpd = snapshot.atkSpd;
        stats.lethality = snapshot.lethality;
        stats.level = snapshot.level;
        stats.experience = snapshot.experience;
        stats.gold = snapshot.gold;
        stats.experienceToNextLevel = snapshot.experienceToNextLevel;
        stats.skillPoints = snapshot.skillPoints;

        // Синхронизируем боевую систему
        AutoCombat combat = stats.GetComponent<AutoCombat>();
        if (combat == null)
            combat = stats.GetComponentInChildren<AutoCombat>();
        if (combat != null && combat.team == CombatTeam.Player)
        {
            combat.maxHealth = snapshot.maxHp;
            combat.damage = snapshot.atk;
            int missing = snapshot.hp - combat.CurrentHealth;
            if (missing > 0) combat.Heal(missing);
            else if (missing < 0) combat.TakeDamage(-missing, int.MaxValue);
        }

        // Инвентарь
        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv != null)
        {
            inv.items.Clear();
            foreach (var (item, qty) in snapshot.items)
                inv.items.Add(new InventoryItem { itemData = item, quantity = qty });
            Inventory.NotifyInventoryChanged();
        }

        // Экипировка
        EquipmentManager eq = FindAnyObjectByType<EquipmentManager>();
        if (eq != null)
        {
            eq.weapon = snapshot.weapon;
            eq.armor = snapshot.armor;
            eq.boots = snapshot.boots;
            eq.accessory = snapshot.accessory;
        }

        Debug.Log($"[PlayerStateTransfer] Applied: lvl={snapshot.level}, hp={snapshot.hp}/{snapshot.maxHp}, gold={snapshot.gold}, items={snapshot.items.Count}");
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class PlayerStatsData
{
    public int hp;
    public int maxHp;
    public int atk;
    public int def;
    public int spd;
    public int lck;
    public int atkSpd;
    public int lethality;
    public int level;
    public int experience;
    public int gold;
    public int experienceToNextLevel;
    public int skillPoints;
    public int baseLck;
    public float defCurvePower;
    public float defCapPercent;
    public int defCapValue;
    public float baseCritChance;
    public float critDamageMultiplier;
}

[Serializable]
public class InventoryItemData
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class EquipmentData
{
    public string weaponId;
    public string armorId;
    public string bootsId;
    public string accessoryId;
}

[Serializable]
public class ShopData
{
    public int weaponTierIndex;
    public int armorTierIndex;
    public int bootTierIndex;
    public int accessoryTierIndex;
}

[Serializable]
public class WorldLevelData
{
    public int currentWorldLevel;
    public int currentLevelIndex;
}

[Serializable]
public class QuestData
{
    public string npcName;
    public bool questStarted;
    public bool questCompleted;
    public string requiredItemId;
}

[Serializable]
public class TrashedItemData
{
    public string itemId;
    public string itemName;
    public int quantity;
    public string trashedTime;
}

[Serializable]
public class GameData
{
    public string version = "1";
    public string savedAt;
    public string sceneName;
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public PlayerStatsData playerStats;
    public List<InventoryItemData> inventory;
    public EquipmentData equipment;
    public ShopData shop;
    public WorldLevelData worldLevel;
    public List<QuestData> quests;
    public List<TrashedItemData> trashedItems;
    public bool emanatorSpawned;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const int SlotCount = 3;
    public const int AutoSaveSlot = 0;

    public int lastUsedSlot = 1;
    public bool autoLoadOnStart = false;

    string SaveFolder => Application.persistentDataPath;

    public string GetSlotLabel(int slot) => slot == AutoSaveSlot ? "Автосохранение" : $"Слот {slot}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("SaveManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SaveManager>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    void OnApplicationQuit()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
            AutoSave();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && SceneManager.GetActiveScene().name != "MainMenu")
            AutoSave();
    }

    public string GetSlotPath(int slot)
    {
        if (slot == AutoSaveSlot)
            return Path.Combine(SaveFolder, "autosave.json");
        return Path.Combine(SaveFolder, $"slot{slot}.json");
    }

    public bool SaveGame(int slot)
    {
        try
        {
            GameData data = CaptureGameData();
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSlotPath(slot), json);
            lastUsedSlot = slot;
            Debug.Log($"[SaveManager] Saved to slot {slot}: {data.sceneName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
            return false;
        }
    }

    public bool LoadGame(int slot)
    {
        string path = GetSlotPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveManager] Slot {slot} not found: {path}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);
            if (data == null)
            {
                Debug.LogError($"[SaveManager] Failed to parse slot {slot}");
                return false;
            }

            if (!string.IsNullOrEmpty(data.sceneName) && data.sceneName != SceneManager.GetActiveScene().name)
            {
                pendingLoadData = data;
                pendingLoadFromOtherScene = true;
                SceneManager.LoadScene(data.sceneName);
            }
            else
            {
                ApplyGameData(data, false);
            }
            lastUsedSlot = slot;
            Debug.Log($"[SaveManager] Loaded slot {slot}: {data.sceneName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
            return false;
        }
    }

    public bool HasSave(int slot)
    {
        return File.Exists(GetSlotPath(slot));
    }

    public void AutoSave()
    {
        SaveGame(AutoSaveSlot);
    }

    public bool LoadAuto()
    {
        return LoadGame(AutoSaveSlot);
    }

    public void DeleteSlot(int slot)
    {
        string path = GetSlotPath(slot);
        if (File.Exists(path))
            File.Delete(path);
    }

    GameData CaptureGameData()
    {
        GameData data = new GameData();
        data.sceneName = SceneManager.GetActiveScene().name;

        PlayerStats stats = FindAnyObjectByType<PlayerStats>();
        if (stats != null)
        {
            data.playerStats = new PlayerStatsData
            {
                hp = stats.hp,
                maxHp = stats.maxHp,
                atk = stats.atk,
                def = stats.def,
                spd = stats.spd,
                lck = stats.lck,
                atkSpd = stats.atkSpd,
                lethality = stats.lethality,
                level = stats.level,
                experience = stats.experience,
                gold = stats.gold,
                experienceToNextLevel = stats.experienceToNextLevel,
                skillPoints = stats.skillPoints,
                baseLck = stats.baseLck,
                defCurvePower = stats.defCurvePower,
                defCapPercent = stats.defCapPercent,
                defCapValue = stats.defCapValue,
                baseCritChance = stats.baseCritChance,
                critDamageMultiplier = stats.critDamageMultiplier
            };

            data.playerPosX = stats.transform.position.x;
            data.playerPosY = stats.transform.position.y;
            data.playerPosZ = stats.transform.position.z;
        }

        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv != null)
        {
            data.inventory = new List<InventoryItemData>();
            foreach (var item in inv.items)
            {
                if (item.itemData == null) continue;
                data.inventory.Add(new InventoryItemData { itemId = item.itemData.itemId, quantity = item.quantity });
            }
        }

        EquipmentManager eq = EquipmentManager.Instance ?? FindAnyObjectByType<EquipmentManager>();
        if (eq != null)
        {
            data.equipment = new EquipmentData
            {
                weaponId = eq.weapon != null ? eq.weapon.itemId : null,
                armorId = eq.armor != null ? eq.armor.itemId : null,
                bootsId = eq.boots != null ? eq.boots.itemId : null,
                accessoryId = eq.accessory != null ? eq.accessory.itemId : null
            };
        }

        ShopManager shop = ShopManager.Instance ?? FindAnyObjectByType<ShopManager>();
        if (shop != null)
        {
            data.shop = new ShopData
            {
                weaponTierIndex = shop.weaponTierIndex,
                armorTierIndex = shop.armorTierIndex,
                bootTierIndex = shop.bootTierIndex,
                accessoryTierIndex = shop.accessoryTierIndex
            };
        }

        WorldLevelManager world = WorldLevelManager.Instance ?? FindAnyObjectByType<WorldLevelManager>();
        if (world != null)
        {
            data.worldLevel = new WorldLevelData
            {
                currentWorldLevel = world.currentWorldLevel,
                currentLevelIndex = world.currentLevelIndex
            };
        }

        data.quests = CaptureQuestData();
        data.trashedItems = CaptureTrashedItems();
        data.emanatorSpawned = TrashEmanatorSpawnManager.Instance != null && TrashEmanatorSpawnManager.Instance.IsSpawned;

        return data;
    }

    List<TrashedItemData> CaptureTrashedItems()
    {
        List<TrashedItemData> list = new List<TrashedItemData>();
        if (TrashEmanator.Instance != null)
        {
            foreach (var item in TrashEmanator.Instance.trashedItems)
            {
                list.Add(new TrashedItemData
                {
                    itemId = item.itemId,
                    itemName = item.itemName,
                    quantity = item.quantity,
                    trashedTime = item.trashedTime
                });
            }
        }
        if (TrashEmanatorSpawnManager.Instance != null)
        {
            foreach (var item in TrashEmanatorSpawnManager.Instance.GetPendingItems())
            {
                list.Add(new TrashedItemData
                {
                    itemId = item.itemId,
                    itemName = item.itemName,
                    quantity = item.quantity,
                    trashedTime = item.trashedTime
                });
            }
        }
        return list;
    }

    List<QuestData> CaptureQuestData()
    {
        List<QuestData> list = new List<QuestData>();
        DialogueTrigger[] triggers = FindObjectsByType<DialogueTrigger>(FindObjectsInactive.Include);
        foreach (DialogueTrigger trigger in triggers)
        {
            try
            {
                list.Add(new QuestData
                {
                    npcName = trigger.name,
                    questStarted = trigger.GetQuestStarted(),
                    questCompleted = trigger.GetQuestCompleted(),
                    requiredItemId = trigger.requiredItem != null ? trigger.requiredItem.itemId : null
                });
            }
            catch { }
        }
        return list;
    }

    GameData pendingLoadData;
    bool pendingLoadFromOtherScene;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerSpawner.ResetReady();

        if (pendingLoadData != null)
        {
            ApplyGameData(pendingLoadData, pendingLoadFromOtherScene);
            pendingLoadData = null;
            pendingLoadFromOtherScene = false;
            return;
        }

        if (scene.name != "MainMenu")
        {
            GameObject player = PlayerSpawner.GetOrCreateInstance()?.EnsurePlayerExists();
            if (player != null)
            {
                PlayerSpawner spawner = PlayerSpawner.GetOrCreateInstance();
                if (spawner != null) spawner.SnapToSpawnPoint(player);
                PlayerSpawner.NotifyPlayerReady();
            }
            AutoSave();
        }
    }

    void ApplyGameData(GameData data, bool fromOtherScene = false)
    {
        GameObject player = PlayerSpawner.GetOrCreateInstance()?.EnsurePlayerExists();

        PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
        if (stats == null) stats = player != null ? player.GetComponentInChildren<PlayerStats>() : null;
        if (stats == null) stats = FindAnyObjectByType<PlayerStats>();

        if (stats != null && data.playerStats != null)
        {
            stats.hp = data.playerStats.hp;
            stats.maxHp = data.playerStats.maxHp;
            stats.atk = data.playerStats.atk;
            stats.def = data.playerStats.def;
            stats.spd = data.playerStats.spd;
            stats.lck = data.playerStats.lck;
            stats.atkSpd = data.playerStats.atkSpd;
            stats.lethality = data.playerStats.lethality;
            stats.level = data.playerStats.level;
            stats.experience = data.playerStats.experience;
            stats.gold = data.playerStats.gold;
            stats.experienceToNextLevel = data.playerStats.experienceToNextLevel;
            stats.skillPoints = data.playerStats.skillPoints;
            stats.baseLck = data.playerStats.baseLck;
            stats.defCurvePower = data.playerStats.defCurvePower;
            stats.defCapPercent = data.playerStats.defCapPercent;
            stats.defCapValue = data.playerStats.defCapValue;
            stats.baseCritChance = data.playerStats.baseCritChance;
            stats.critDamageMultiplier = data.playerStats.critDamageMultiplier;

            Vector3 savedPos = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
            if (savedPos == Vector3.zero) fromOtherScene = true;
            Vector3? targetPos = fromOtherScene ? (Vector3?)null : savedPos;
            PlayerSpawner.GetOrCreateInstance()?.SnapToSpawnPoint(stats.gameObject, targetPos);
        }

        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv != null && data.inventory != null)
        {
            inv.items.Clear();
            foreach (var itemData in data.inventory)
            {
                ItemData item = FindItemById(itemData.itemId);
                if (item != null)
                    inv.items.Add(new InventoryItem { itemData = item, quantity = itemData.quantity });
            }
            Inventory.NotifyInventoryChanged();
        }

        EquipmentManager eq = EquipmentManager.Instance ?? FindAnyObjectByType<EquipmentManager>();
        if (eq != null && data.equipment != null)
        {
            eq.weapon = FindItemById(data.equipment.weaponId);
            eq.armor = FindItemById(data.equipment.armorId);
            eq.boots = FindItemById(data.equipment.bootsId);
            eq.accessory = FindItemById(data.equipment.accessoryId);
        }

        ShopManager shop = ShopManager.Instance ?? FindAnyObjectByType<ShopManager>();
        if (shop != null && data.shop != null)
        {
            shop.weaponTierIndex = data.shop.weaponTierIndex;
            shop.armorTierIndex = data.shop.armorTierIndex;
            shop.bootTierIndex = data.shop.bootTierIndex;
            shop.accessoryTierIndex = data.shop.accessoryTierIndex;
            shop.RebuildDynamicShop();
        }

        WorldLevelManager world = WorldLevelManager.Instance ?? FindAnyObjectByType<WorldLevelManager>();
        if (world != null && data.worldLevel != null)
        {
            world.currentWorldLevel = data.worldLevel.currentWorldLevel;
            world.currentLevelIndex = data.worldLevel.currentLevelIndex;
        }

        ApplyQuestData(data.quests);
        ApplyTrashedItems(data.trashedItems);
        if (data.emanatorSpawned && TrashEmanatorSpawnManager.Instance != null && !TrashEmanatorSpawnManager.Instance.IsSpawned)
            TrashEmanatorSpawnManager.Instance.TrySpawn();

        AutoCombat playerCombat = FindPlayerAutoCombat();
        if (playerCombat != null)
        {
            playerCombat.maxHealth = stats != null ? stats.maxHp : playerCombat.maxHealth;
            playerCombat.damage = stats != null ? stats.atk : playerCombat.damage;
            playerCombat.ResetHealth();
        }

        PlayerSpawner.NotifyPlayerReady();

        PlayerStateTransfer.Instance?.ClearSnapshot();
    }

    void ApplyQuestData(List<QuestData> quests)
    {
        if (quests == null) return;
        DialogueTrigger[] triggers = FindObjectsByType<DialogueTrigger>(FindObjectsInactive.Include);
        foreach (QuestData quest in quests)
        {
            foreach (DialogueTrigger trigger in triggers)
            {
                if (trigger.name != quest.npcName) continue;
                try
                {
                    trigger.SetQuestStarted(quest.questStarted);
                    trigger.SetQuestCompleted(quest.questCompleted);
                }
                catch { }
            }
        }
    }

    void ApplyTrashedItems(List<TrashedItemData> items)
    {
        if (items == null) return;
        if (TrashEmanator.Instance != null)
        {
            TrashEmanator.Instance.trashedItems.Clear();
            foreach (var data in items)
            {
                ItemData item = FindItemById(data.itemId);
                TrashEmanator.Instance.trashedItems.Add(new TrashedItem
                {
                    itemId = data.itemId,
                    itemName = data.itemName,
                    quantity = data.quantity,
                    trashedTime = data.trashedTime,
                    itemData = item
                });
            }
            TrashEmanator.Instance.NotifyListChanged();
        }
        else if (TrashEmanatorSpawnManager.Instance != null)
        {
            TrashEmanatorSpawnManager.Instance.GetPendingItems().Clear();
            foreach (var data in items)
            {
                TrashEmanatorSpawnManager.Instance.GetPendingItems().Add(new TrashedItem
                {
                    itemId = data.itemId,
                    itemName = data.itemName,
                    quantity = data.quantity,
                    trashedTime = data.trashedTime
                });
            }
        }
    }

    AutoCombat FindPlayerAutoCombat()
    {
        AutoCombat[] combats = FindObjectsByType<AutoCombat>();
        foreach (AutoCombat c in combats)
            if (c.team == CombatTeam.Player)
                return c;
        return null;
    }

    ItemData FindItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (ItemData item in allItems)
        {
            if (item.itemId == itemId)
                return item;
        }
        return null;
    }

}

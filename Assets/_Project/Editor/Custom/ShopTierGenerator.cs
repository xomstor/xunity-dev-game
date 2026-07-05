#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ShopTierGenerator : EditorWindow
{
    private static readonly string[] tierNames = new string[]
    {
        "Мусор", "Тряпка", "Барахло", "Бесполезность", "Хлам", "Норм",
        "Бронза", "Медь", "Железо", "Золото", "Алмаз", "Изумруд", "Платина", "Challenger"
    };

    [MenuItem("Tools/Generate Shop Tiers")]
    public static void Generate()
    {
        string folderPath = "Assets/_Project/ScriptableObjects/Items/ShopTiers";
        CreateFolder(folderPath);

        ShopManager shopManager = FindAnyObjectByType<ShopManager>();
        if (shopManager == null)
        {
            Debug.LogWarning("ShopManager не найден в сцене. Предметы созданы, но не назначены. Назначь их вручную.");
        }

        List<ItemData> weapons = new List<ItemData>();
        List<ItemData> armors = new List<ItemData>();
        List<ItemData> boots = new List<ItemData>();
        List<ItemData> accessories = new List<ItemData>();

        for (int i = 0; i < tierNames.Length; i++)
        {
            weapons.Add(CreateItem(folderPath, "weapon", i, ItemType.Weapon,
                $"{tierNames[i]} меч", $"Оружие уровня '{tierNames[i]}'. Наносит урон врагам.",
                atk: 3 + i * 2, def: 0, hp: 0, spd: 0, lck: 0));

            armors.Add(CreateItem(folderPath, "armor", i, ItemType.Armor,
                $"{tierNames[i]} броня", $"Броня уровня '{tierNames[i]}'. Защищает от ударов.",
                atk: 0, def: 2 + i, hp: 5 + i * 2, spd: 0, lck: 0));

            boots.Add(CreateItem(folderPath, "boots", i, ItemType.Armor,
                $"{tierNames[i]} ботинки", $"Ботинки уровня '{tierNames[i]}'. Увеличивают скорость.",
                atk: 0, def: 0, hp: 0, spd: 2 + i, lck: 0));

            accessories.Add(CreateItem(folderPath, "accessory", i, ItemType.Armor,
                $"{tierNames[i]} кольцо", $"Аксессуар уровня '{tierNames[i]}'. Даёт удачу.",
                atk: 0, def: 0, hp: 2 + i, spd: 0, lck: 1 + i / 2));
        }

        if (shopManager != null)
        {
            Undo.RecordObject(shopManager, "Fill Shop Tiers");
            shopManager.weaponTiers = weapons.ToArray();
            shopManager.armorTiers = armors.ToArray();
            shopManager.bootTiers = boots.ToArray();
            shopManager.accessoryTiers = accessories.ToArray();
            EditorUtility.SetDirty(shopManager);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Сгенерировано {tierNames.Length} тиров шмоток. Назначено в ShopManager: {(shopManager != null ? "да" : "нет")}");
    }

    static ItemData CreateItem(string folderPath, string category, int tier, ItemType itemType, string itemName, string description,
        int atk, int def, int hp, int spd, int lck)
    {
        string itemId = $"shop_{category}_tier_{tier}";
        string path = $"{folderPath}/{itemId}.asset";

        ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (existing != null)
        {
            return existing;
        }

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = itemId;
        item.itemName = itemName;
        item.description = description;
        item.itemType = itemType;
        item.rarity = GetRarityForTier(tier);
        item.price = 50 + 30 * tier * tier;
        item.maxStack = 1;
        item.atkBonus = atk;
        item.defBonus = def;
        item.hpBonus = hp;
        item.spdBonus = spd;
        item.lckBonus = lck;
        item.restoreHp = 0;
        item.experienceReward = 0;

        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    static ItemRarity GetRarityForTier(int tier)
    {
        if (tier <= 1) return ItemRarity.Common;
        if (tier <= 4) return ItemRarity.Uncommon;
        if (tier <= 8) return ItemRarity.Rare;
        if (tier <= 12) return ItemRarity.Epic;
        return ItemRarity.Legendary;
    }

    static void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
#endif

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class EnemyBatchGenerator : EditorWindow
{
    private static readonly List<EnemyDefinition> enemies = new List<EnemyDefinition>
    {
        new EnemyDefinition
        {
            id = "enemy_spyder",
            name = "Паук-шпион",
            description = "Ползает по потолкам подземелий и шпионит за героями. Его глаза слишком много видят.",
            level = 2, health = 45, damage = 8, defense = 1, exp = 12, gold = 4,
            dropId = "drop_spider_tail", dropName = "Паучий хвост", dropType = ItemType.Material,
            dropDescription = "Отрезанный хвост паука. Зачем? Никто не знает, но торговцы платят за него монетки.",
            dropRarity = ItemRarity.Common, dropPrice = 10, atk = 0, def = 0, hp = 0, spd = 1
        },
        new EnemyDefinition
        {
            id = "enemy_boar_smoker",
            name = "Кабан-коптильщик",
            description = "Дикий кабан, который случайно пробежал через коптильню. Пахнет вкусно и опасно.",
            level = 3, health = 80, damage = 14, defense = 3, exp = 20, gold = 8,
            dropId = "drop_sausage_sword", dropName = "Копчёная сосиска-меч", dropType = ItemType.Weapon,
            dropDescription = "Двуручная сосиска, затвердевшая от копчения. Наносит урон одновременно физический и моральный.",
            dropRarity = ItemRarity.Uncommon, dropPrice = 45, atk = 12, def = 0, hp = 2, spd = -1
        },
        new EnemyDefinition
        {
            id = "enemy_creeper_gardener",
            name = "Крипер-огородник",
            description = "Зелёный мститель, который защищает свою грядку луком. Лучше не подходить после полива.",
            level = 4, health = 60, damage = 25, defense = 0, exp = 28, gold = 6,
            dropId = "drop_onion_ghost", dropName = "Лук-призрак", dropType = ItemType.Consumable,
            dropDescription = "Полупрозрачная луковица. Если съесть, начинаешь видеть призраков и плакать одновременно.",
            dropRarity = ItemRarity.Common, dropPrice = 15, atk = 0, def = 0, hp = 8, spd = 0, restoreHp = 8
        },
        new EnemyDefinition
        {
            id = "enemy_skeleton_accountant",
            name = "Скелет-бухгалтер",
            description = "Считает каждую косточку в инвентаре. Очень зол, если у тебя нет чека.",
            level = 5, health = 55, damage = 12, defense = 5, exp = 25, gold = 15,
            dropId = "drop_skull_piggybank", dropName = "Череп-копилка", dropType = ItemType.Misc,
            dropDescription = "Пустой череп, в который удобно складывать мелочь. Иногда захватывает души сдачи.",
            dropRarity = ItemRarity.Uncommon, dropPrice = 60, atk = 0, def = 2, hp = 0, spd = 0
        },
        new EnemyDefinition
        {
            id = "enemy_slime_confectioner",
            name = "Слайм-кондитер",
            description = "Желейная сущность, которая мечтает о собственной кондитерской. Сладкий, но ядовитый.",
            level = 2, health = 40, damage = 7, defense = 0, exp = 10, gold = 3,
            dropId = "drop_jelly_bomb", dropName = "Желе-бомба", dropType = ItemType.Consumable,
            dropDescription = "Баночка взрывного желе. Бросаешь — все прилипают. Включая тебя.",
            dropRarity = ItemRarity.Common, dropPrice = 20, atk = 0, def = 0, hp = 5, spd = 0, restoreHp = 5
        },
        new EnemyDefinition
        {
            id = "enemy_wolf_rocker",
            name = "Волк-рокер",
            description = "Серый волк с ирокезом из гривы. Завывает не на луну, а на бит.",
            level = 6, health = 95, damage = 18, defense = 2, exp = 35, gold = 10,
            dropId = "drop_bone_guitar", dropName = "Гитара из костей", dropType = ItemType.Weapon,
            dropDescription = "Акустическая гитара, собранная из бёдер бывших врагов. Звучит тяжело.",
            dropRarity = ItemRarity.Rare, dropPrice = 120, atk = 16, def = 0, hp = 0, spd = 2
        },
        new EnemyDefinition
        {
            id = "enemy_goblin_cook",
            name = "Гоблин-повар",
            description = "Маленький, зелёный и очень капризный. Бросает в противников недоваренными котлетами.",
            level = 4, health = 65, damage = 11, defense = 2, exp = 22, gold = 7,
            dropId = "drop_pot_shield", dropName = "Кастрюля-щит", dropType = ItemType.Armor,
            dropDescription = "Чугунная кастрюля с ручками. Отлично защищает и можно сварить суп после боя.",
            dropRarity = ItemRarity.Uncommon, dropPrice = 55, atk = 0, def = 8, hp = 5, spd = -2
        },
        new EnemyDefinition
        {
            id = "enemy_zombie_farmer",
            name = "Зомби-фермер",
            description = "До сих пор пытается вспахать поле, хотя давно мёртв. Очень ревнует к своей моркови.",
            level = 3, health = 75, damage = 13, defense = 1, exp = 18, gold = 5,
            dropId = "drop_rotten_carrot_dagger", dropName = "Гнилая морковка-кинжал", dropType = ItemType.Weapon,
            dropDescription = "Зелёная, мягкая и вонючая. Наносит урон ядом и дурманит врагов запахом.",
            dropRarity = ItemRarity.Common, dropPrice = 18, atk = 7, def = 0, hp = 0, spd = 1
        },
        new EnemyDefinition
        {
            id = "enemy_ghost_barista",
            name = "Призрак-бариста",
            description = "Прозрачный бармен, который вечно наливает кофе. Не спит уже триста лет.",
            level = 7, health = 50, damage = 20, defense = 0, exp = 40, gold = 12,
            dropId = "drop_insomnia_cup", dropName = "Чашка бессонницы", dropType = ItemType.Consumable,
            dropDescription = "Кружка, из которой невозможно выпить всё. Даёт бодрость, но забирает сны.",
            dropRarity = ItemRarity.Rare, dropPrice = 90, atk = 0, def = 0, hp = 15, spd = 3, restoreHp = 0
        },
        new EnemyDefinition
        {
            id = "enemy_bear_programmer",
            name = "Медведь-программист",
            description = "Большой бурый медведь, который сидит за ноутбуком из коры и ругается на баги.",
            level = 8, health = 130, damage = 22, defense = 6, exp = 55, gold = 20,
            dropId = "drop_keyboard_whip", dropName = "Клавиатура-кнут", dropType = ItemType.Weapon,
            dropDescription = "Проводная клавиатура с острыми краями клавиш. Щёлкает больно и громко.",
            dropRarity = ItemRarity.Rare, dropPrice = 140, atk = 19, def = 0, hp = 0, spd = 1
        },
        new EnemyDefinition
        {
            id = "enemy_vampire_dentist",
            name = "Вампир-стоматолог",
            description = "Лечит зубы, но иногда заодно пьёт кровь. Очень дотошный к гигиене полости рта.",
            level = 9, health = 85, damage = 17, defense = 3, exp = 48, gold = 18,
            dropId = "drop_toothbrush_spear", dropName = "Зубная щётка-копьё", dropType = ItemType.Weapon,
            dropDescription = "Длинная щётка с щетиной из серебра. Колет и чистит одновременно.",
            dropRarity = ItemRarity.Uncommon, dropPrice = 85, atk = 14, def = 0, hp = 0, spd = 2
        },
        new EnemyDefinition
        {
            id = "enemy_gargoyle_florist",
            name = "Гаргулья-флорист",
            description = "Каменная гаргулья, которая разводит кактусы на крыше. Очень колючий собеседник.",
            level = 6, health = 110, damage = 15, defense = 8, exp = 38, gold = 11,
            dropId = "drop_cactus_flower", dropName = "Кактус-цветок", dropType = ItemType.Material,
            dropDescription = "Редкий цветок, который цветёт один раз в жизни. И сразу колется.",
            dropRarity = ItemRarity.Uncommon, dropPrice = 40, atk = 0, def = 1, hp = 0, spd = 0
        },
        new EnemyDefinition
        {
            id = "enemy_mimic_shopper",
            name = "Мимик-шопоголик",
            description = "Сундук, который притворяется распродажей. Открываешь — он тебя съедает.",
            level = 10, health = 100, damage = 28, defense = 4, exp = 60, gold = 30,
            dropId = "drop_evening_star_bag", dropName = "Сумка вечерней звезды", dropType = ItemType.Armor,
            dropDescription = "Маленькая сумочка, внутри которой мерцает звезда. Немного тяжела.",
            dropRarity = ItemRarity.Rare, dropPrice = 150, atk = 0, def = 5, hp = 10, spd = 1
        },
        new EnemyDefinition
        {
            id = "enemy_orc_poet",
            name = "Орк-поэт",
            description = "Зелёный громила, который читает стихи перед боем. Рифмы ужасные, удары тяжёлые.",
            level = 5, health = 90, damage = 16, defense = 3, exp = 30, gold = 9,
            dropId = "drop_poetic_whip", dropName = "Стихотворная плётка", dropType = ItemType.Weapon,
            dropDescription = "Плётка из свёрнутых свитков с плохими рифмами. Больно бьёт по самолюбию.",
            dropRarity = ItemRarity.Common, dropPrice = 35, atk = 9, def = 0, hp = 0, spd = 0
        },
        new EnemyDefinition
        {
            id = "enemy_dragon_smoker",
            name = "Дракон-курильщик",
            description = "Большой дракон, который не дышит огнём, а выпускает клубы дыма. Всё равно опасно.",
            level = 12, health = 200, damage = 35, defense = 10, exp = 120, gold = 80,
            dropId = "drop_fire_sneeze_pipe", dropName = "Трубка огненного чиха", dropType = ItemType.Weapon,
            dropDescription = "Трубка, из которой вылетает огонь при каждом чихании. Чихай с осторожностью.",
            dropRarity = ItemRarity.Epic, dropPrice = 300, atk = 28, def = 0, hp = 0, spd = 2
        },
        new EnemyDefinition
        {
            id = "enemy_undead_dancer",
            name = "Нежить-танцор",
            description = "Скелет, который не может остановиться танцевать. Даже в бою отбивает чечётку.",
            level = 7, health = 70, damage = 14, defense = 2, exp = 42, gold = 13,
            dropId = "drop_party_shoes", dropName = "Туфли вечеринки", dropType = ItemType.Armor,
            dropDescription = "Блестящие туфли, которые сами начинают танцевать. Нельзя стоять на месте.",
            dropRarity = ItemRarity.Uncommon, dropPrice = 75, atk = 0, def = 3, hp = 0, spd = 5
        },
        new EnemyDefinition
        {
            id = "enemy_elemental_cleaner",
            name = "Элементаль-уборщик",
            description = "Живая стихия воды и моющего средства. Убирает всё, включая героев.",
            level = 8, health = 85, damage = 19, defense = 1, exp = 50, gold = 14,
            dropId = "drop_broom_storm", dropName = "Веник-шторм", dropType = ItemType.Weapon,
            dropDescription = "Веник, который взмывает в воздух и заметает врагов ураганом. Очень гигиенично.",
            dropRarity = ItemRarity.Rare, dropPrice = 130, atk = 17, def = 0, hp = 0, spd = 3
        },
        new EnemyDefinition
        {
            id = "enemy_mushroom_philosopher",
            name = "Гриб-философ",
            description = "Огромный мухомор, который задаёт вопросы о смысле жизни. Ядовитый и занудный.",
            level = 11, health = 120, damage = 24, defense = 4, exp = 80, gold = 25,
            dropId = "drop_deep_thoughts_hat", dropName = "Шляпа глубоких мыслей", dropType = ItemType.Armor,
            dropDescription = "Шляпа с грибной шляпкой. Надеваешь — начинаешь понимать вселенную. Слишком хорошо.",
            dropRarity = ItemRarity.Epic, dropPrice = 220, atk = 0, def = 6, hp = 15, spd = 0
        },
        new EnemyDefinition
        {
            id = "enemy_harpy_fan",
            name = "Гарпия-фанатка",
            description = "Птице-женщина, которая кричит именами героев и бросается перьями. Опасная группи.",
            level = 6, health = 60, damage = 18, defense = 1, exp = 36, gold = 10,
            dropId = "drop_autograph_feather", dropName = "Перо автографа", dropType = ItemType.Material,
            dropDescription = "Перо, которым можно писать автографы. Если подписать врага — он обидится.",
            dropRarity = ItemRarity.Common, dropPrice = 25, atk = 0, def = 0, hp = 0, spd = 1
        },
        new EnemyDefinition
        {
            id = "enemy_boss_crab",
            name = "Краб-судьба",
            description = "Гигантский краб, который решает, кому жить, а кому быть съеденным. Очень серьёзный.",
            level = 15, health = 300, damage = 40, defense = 15, exp = 250, gold = 150,
            dropId = "drop_scissors_of_destiny", dropName = "Ножницы судьбы", dropType = ItemType.Weapon,
            dropDescription = "Большие ножницы, которыми можно перерезать нить судьбы. Или просто пакет.",
            dropRarity = ItemRarity.Legendary, dropPrice = 500, atk = 40, def = 0, hp = 0, spd = 4
        }
    };

    [MenuItem("Tools/Generate Enemy Batch")]
    public static void ShowWindow()
    {
        GetWindow<EnemyBatchGenerator>("Enemy Batch Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Сгенерировать 20 врагов с абсурдными дропами", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Сгенерировать врагов и предметы", GUILayout.Height(40)))
        {
            GenerateAll();
        }
    }

    private static void GenerateAll()
    {
        string basePath = "Assets/_Project/ScriptableObjects";
        string enemiesPath = $"{basePath}/Enemies";
        string dropsPath = $"{basePath}/Items/EnemyDrops";

        CreateFolder(enemiesPath);
        CreateFolder(dropsPath);

        int generatedEnemies = 0;
        int generatedDrops = 0;

        foreach (var def in enemies)
        {
            ItemData drop = CreateDropItem(def, dropsPath);
            generatedDrops++;

            CreateEnemy(def, drop, enemiesPath);
            generatedEnemies++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Готово", 
            $"Сгенерировано: {generatedEnemies} врагов и {generatedDrops} предметов.", 
            "OK");
    }

    private static void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static ItemData CreateDropItem(EnemyDefinition def, string folderPath)
    {
        string assetPath = $"{folderPath}/{def.dropId}.asset";

        ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        ItemData drop = ScriptableObject.CreateInstance<ItemData>();
        drop.itemId = def.dropId;
        drop.itemName = def.dropName;
        drop.description = def.dropDescription;
        drop.itemType = def.dropType;
        drop.rarity = def.dropRarity;
        drop.price = def.dropPrice;
        drop.maxStack = 1;
        drop.atkBonus = def.atk;
        drop.defBonus = def.def;
        drop.hpBonus = def.hp;
        drop.spdBonus = def.spd;
        drop.lckBonus = 0;
        drop.restoreHp = def.restoreHp;
        drop.experienceReward = 0;

        AssetDatabase.CreateAsset(drop, assetPath);
        return drop;
    }

    private static void CreateEnemy(EnemyDefinition def, ItemData drop, string folderPath)
    {
        string assetPath = $"{folderPath}/{def.id}.asset";

        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
        if (existing != null)
        {
            Debug.Log($"Враг {def.name} уже существует, пропускаю.");
            return;
        }

        EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
        enemy.enemyId = def.id;
        enemy.enemyName = def.name;
        enemy.description = def.description;
        enemy.level = def.level;
        enemy.maxHealth = def.health;
        enemy.damage = def.damage;
        enemy.defense = def.defense;
        enemy.experienceReward = def.exp;
        enemy.goldReward = def.gold;
        enemy.uniqueDrop = drop;
        enemy.dropChance = 0.25f;

        AssetDatabase.CreateAsset(enemy, assetPath);
    }

    private class EnemyDefinition
    {
        public string id;
        public string name;
        public string description;
        public int level;
        public int health;
        public int damage;
        public int defense;
        public int exp;
        public int gold;

        public string dropId;
        public string dropName;
        public string dropDescription;
        public ItemType dropType;
        public ItemRarity dropRarity;
        public int dropPrice;
        public int atk;
        public int def;
        public int hp;
        public int spd;
        public int restoreHp;
    }
}
#endif

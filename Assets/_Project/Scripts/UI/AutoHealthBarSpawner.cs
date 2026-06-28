using UnityEngine;

public class AutoHealthBarSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject healthBarPrefab; // Перетащи сюда HealthBarPrefab

    [Header("Settings")]
    public Canvas canvas; // Перетащи MainCanvas
    public bool spawnForPlayers = true;
    public bool spawnForEnemies = true;

    [Header("Player Settings")]
    public bool playerBarFixedPosition = true; // Фиксированная в углу?
    public Vector2 playerBarPosition = new Vector2(20, -20); // Позиция в углу
    public Vector2 playerBarSize = new Vector2(200, 25); // Размер полоски игрока

    void Start()
    {
        // Найти все объекты с AutoCombat
        AutoCombat[] allCombatants = FindObjectsOfType<AutoCombat>();

        foreach (var combatant in allCombatants)
        {
            // Проверяем команду
            bool shouldSpawn = false;

            if (combatant.team == CombatTeam.Player && spawnForPlayers)
                shouldSpawn = true;
            else if (combatant.team == CombatTeam.Enemy && spawnForEnemies)
                shouldSpawn = true;

            if (shouldSpawn)
            {
                CreateHealthBar(combatant);
            }
        }
    }

    void CreateHealthBar(AutoCombat combatant)
    {
        if (healthBarPrefab == null || canvas == null)
        {
            Debug.LogError("Не задан HealthBarPrefab или Canvas!");
            return;
        }

        // Создаем полоску
        GameObject barInstance = Instantiate(healthBarPrefab, canvas.transform);
        barInstance.name = $"HealthBar_{combatant.gameObject.name}";

        // Получаем компонент
        HealthBarUI healthBar = barInstance.GetComponent<HealthBarUI>();
        if (healthBar == null)
        {
            Debug.LogError("У префаба нет компонента HealthBarUI!");
            Destroy(barInstance);
            return;
        }

        // Настраиваем
        healthBar.SetTarget(combatant);

        // Особая настройка для игрока
        if (combatant.team == CombatTeam.Player && playerBarFixedPosition)
        {
            healthBar.followTarget = false; // Фиксированная позиция

            // Настраиваем размер и позицию
            RectTransform rect = barInstance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); // Top-Left
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = playerBarPosition;
            rect.sizeDelta = playerBarSize;

            Debug.Log($"✅ Создана полоска игрока (фиксированная)");
        }
        else
        {
            // Для врагов - над головой
            healthBar.followTarget = true;
            Debug.Log($"✅ Создана полоска для {combatant.name} (над головой)");
        }
    }
}
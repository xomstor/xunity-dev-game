using UnityEngine;

public class AutoHealthBarSpawner : MonoBehaviour
{
    public static AutoHealthBarSpawner Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject healthBarPrefab;

    [Header("Settings")]
    public Canvas canvas;
    public bool spawnForPlayers = true;
    public bool spawnForEnemies = true;

    [Header("Player Settings")]
    public bool playerBarFixedPosition = true;
    public Vector2 playerBarPosition = new Vector2(20, -20);
    public Vector2 playerBarSize = new Vector2(200, 25);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SpawnAllBars();
    }

    void SpawnAllBars()
    {
        if (healthBarPrefab == null || canvas == null)
        {
            Debug.LogError("HealthBarPrefab or Canvas not assigned!");
            return;
        }

        AutoCombat[] allCombatants = FindObjectsByType<AutoCombat>(FindObjectsInactive.Exclude);

        foreach (var combatant in allCombatants)
        {
            if (!ShouldSpawnFor(combatant)) continue;
            if (HasHealthBar(combatant)) continue; // Не создаём если уже есть

            CreateHealthBar(combatant);
        }
    }

    // ✅ ПУБЛИЧНЫЙ МЕТОД - создать бар для конкретного врага!
    public void SpawnBarForEnemy(AutoCombat enemy)
    {
        if (enemy == null) return;
        if (healthBarPrefab == null || canvas == null) return;
        if (!ShouldSpawnFor(enemy)) return;
        if (HasHealthBar(enemy)) return; // Уже есть - не создаём

        CreateHealthBar(enemy);
        Debug.Log($"🔨 Spawner: HP Bar создан для {enemy.name}");
    }

    bool ShouldSpawnFor(AutoCombat combatant)
    {
        if (combatant.team == CombatTeam.Player && spawnForPlayers)
            return true;
        if (combatant.team == CombatTeam.Enemy && spawnForEnemies)
            return true;
        return false;
    }

    bool HasHealthBar(AutoCombat combatant)
    {
        HealthBarUI[] existingBars = FindObjectsByType<HealthBarUI>();
        foreach (var bar in existingBars)
        {
            if (bar.target == combatant)
                return true;
        }
        return false;
    }

    void CreateHealthBar(AutoCombat combatant)
    {
        GameObject barInstance = Instantiate(healthBarPrefab, canvas.transform);
        barInstance.name = $"HealthBar_{combatant.gameObject.name}";

        HealthBarUI health = barInstance.GetComponent<HealthBarUI>();
        if (health == null)
        {
            Debug.LogError("HealthBar prefab missing HealthBarUI component!");
            Destroy(barInstance);
            return;
        }

        health.SetTarget(combatant);

        if (combatant.team == CombatTeam.Player && playerBarFixedPosition)
        {
            health.followTarget = false;

            RectTransform rect = barInstance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = playerBarPosition;
            rect.sizeDelta = playerBarSize;
        }
        else
        {
            health.followTarget = true;
        }
    }
}
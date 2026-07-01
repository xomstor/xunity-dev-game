using UnityEngine;

public class AutoHealthBarSpawner : MonoBehaviour
{
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

    void Start()
    {
        AutoCombat[] allCombatants = FindObjectsByType<AutoCombat>(FindObjectsInactive.Exclude);

        foreach (var combatant in allCombatants)
        {
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
            Debug.LogError("HealthBarPrefab or Canvas not assigned!");
            return;
        }

        GameObject barInstance = Instantiate(healthBarPrefab, canvas.transform);
        barInstance.name = $"HealthBar_{combatant.gameObject.name}";

        HealthBarUI healthBar = barInstance.GetComponent<HealthBarUI>();
        if (healthBar == null)
        {
            Debug.LogError("HealthBar prefab missing HealthBarUI component!");
            Destroy(barInstance);
            return;
        }

        healthBar.SetTarget(combatant);

        if (combatant.team == CombatTeam.Player && playerBarFixedPosition)
        {
            healthBar.followTarget = false;

            RectTransform rect = barInstance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = playerBarPosition;
            rect.sizeDelta = playerBarSize;
        }
        else
        {
            healthBar.followTarget = true;
        }
    }
}
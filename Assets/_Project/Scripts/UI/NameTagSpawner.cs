using UnityEngine;

public class NameTagSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject nameTagPrefab;

    [Header("Settings")]
    public Canvas canvas;
    public bool spawnForPlayers = false;
    public bool spawnForEnemies = true;
    public float showDistance = 3f;
    public Vector3 worldOffset = new Vector3(0, 2.5f, 0);

    void Start()
    {
        if (nameTagPrefab == null || canvas == null)
        {
            Debug.LogError("NameTagPrefab or Canvas not assigned!");
            return;
        }

        AutoCombat[] allCombatants = FindObjectsByType<AutoCombat>(FindObjectsSortMode.None);

        foreach (var combatant in allCombatants)
        {
            bool shouldSpawn = false;

            if (combatant.team == CombatTeam.Player && spawnForPlayers)
                shouldSpawn = true;
            else if (combatant.team == CombatTeam.Enemy && spawnForEnemies)
                shouldSpawn = true;

            if (shouldSpawn)
            {
                CreateNameTag(combatant);
            }
        }
    }

    void CreateNameTag(AutoCombat combatant)
    {
        GameObject tagInstance = Instantiate(nameTagPrefab, canvas.transform);
        tagInstance.name = $"NameTag_{combatant.gameObject.name}";

        NameTagUI nameTag = tagInstance.GetComponent<NameTagUI>();
        if (nameTag == null)
        {
            Debug.LogError("NameTag prefab missing NameTagUI component!");
            Destroy(tagInstance);
            return;
        }

        nameTag.target = combatant.transform;
        nameTag.targetCombat = combatant;
        nameTag.showDistance = showDistance;
        nameTag.worldOffset = worldOffset;
        nameTag.Hide();

        AddTriggerDetector(combatant.gameObject, nameTag);
    }

    void AddTriggerDetector(GameObject enemy, NameTagUI nameTag)
    {
        GameObject detectorObject = new GameObject("NameTagDetector");
        detectorObject.transform.SetParent(enemy.transform);
        detectorObject.transform.localPosition = Vector3.zero;

        CircleCollider2D trigger = detectorObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = showDistance;

        Rigidbody2D rb = detectorObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        NameTagDetector detector = detectorObject.AddComponent<NameTagDetector>();
        detector.nameTag = nameTag;
    }
}

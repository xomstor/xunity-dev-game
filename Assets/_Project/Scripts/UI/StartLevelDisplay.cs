using UnityEngine;

public class StartLevelDisplay : MonoBehaviour
{
    [Header("Search")]
    public string playerTag = "Player";
    public float searchRadius = 10f;

    void Start()
    {
        ShowNearestSpawnPoint();
    }

    void ShowNearestSpawnPoint()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        SpawnPoint closest = null;
        float closestDistance = searchRadius;

        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            float distance = Vector2.Distance(player.transform.position, spawnPoint.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = spawnPoint;
            }
        }

        if (closest != null && closest.showLevelName)
        {
            LevelNameDisplay.Instance?.Show(closest.displayName);
        }
    }
}

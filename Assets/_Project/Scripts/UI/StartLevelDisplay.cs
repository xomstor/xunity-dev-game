using UnityEngine;
using System.Collections;

public class StartLevelDisplay : MonoBehaviour
{
    [Header("Search")]
    public string playerTag = "Player";
    public float searchRadius = 10f;
    public float checkDelay = 0.5f;

    void Start()
    {
        StartCoroutine(ShowNearestSpawnPointDelayed());
    }

    IEnumerator ShowNearestSpawnPointDelayed()
    {
        yield return new WaitForSeconds(checkDelay);

        if (LevelNameDisplay.Instance == null)
        {
            Debug.LogError("StartLevelDisplay: LevelNameDisplay.Instance is null!");
            yield break;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogError("StartLevelDisplay: Player not found!");
            yield break;
        }

        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude);

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
            Debug.Log($"StartLevelDisplay: showing {closest.displayName}");
            LevelNameDisplay.Instance.Show(closest.displayName);
        }
    }
}

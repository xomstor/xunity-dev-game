using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string spawnPointName;
    public bool isHub;
    public bool respawnEnemiesWhenVisited = true;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(spawnPointName))
            spawnPointName = gameObject.name;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isHub ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    }
}

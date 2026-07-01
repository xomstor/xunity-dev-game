using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string spawnPointName;
    [Tooltip("Название уровня, которое показывается при спавне")]
    public string displayName;
    public bool isHub;
    public bool respawnEnemiesWhenVisited = true;
    public bool showLevelName = true;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(spawnPointName))
            spawnPointName = gameObject.name;
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isHub ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    }
}

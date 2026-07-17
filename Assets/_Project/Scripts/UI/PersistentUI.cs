using UnityEngine;

public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Находим root объект (может быть не сам gameObject, если вложен)
        GameObject rootGO = gameObject;
        while (rootGO.transform.parent != null)
            rootGO = rootGO.transform.parent.gameObject;
        
        DontDestroyOnLoad(rootGO);
        Debug.Log("[PersistentUI] Made persistent: " + rootGO.name);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

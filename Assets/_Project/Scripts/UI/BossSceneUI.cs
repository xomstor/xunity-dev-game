using UnityEngine;
using UnityEngine.SceneManagement;

public class BossSceneUI : MonoBehaviour
{
    [Tooltip("Scene that contains the persistent UI Canvas")]
    public string uiSourceScene = "GameScene";

    void Awake()
    {
        // Если UI уже есть и постоянный — ничего не делаем
        if (PersistentUI.Instance != null)
        {
            Debug.Log("[BossSceneUI] PersistentUI already exists, skipping load.");
            return;
        }

        // Если UI нет — загружаем GameScene аддитивно
        Debug.Log("[BossSceneUI] Loading UI from " + uiSourceScene);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(uiSourceScene, LoadSceneMode.Additive);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scene.name != uiSourceScene)
        {
            Debug.Log("[BossSceneUI] Loaded scene is " + scene.name + ", not " + uiSourceScene);
            return;
        }

        Debug.Log("[BossSceneUI] " + uiSourceScene + " loaded, finding Canvas with PersistentUI...");

        Canvas canvas = FindCanvasWithPersistentUI(scene);
        if (canvas == null)
        {
            Debug.LogError("[BossSceneUI] No Canvas with PersistentUI found in " + uiSourceScene + "!");
            return;
        }

        Debug.Log("[BossSceneUI] Found Canvas, hiding other objects in loaded scene...");

        // Скрываем все остальные объекты из загруженной сцены (кроме Canvas)
        GameObject canvasGO = canvas.gameObject;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root != canvasGO && root.transform != canvasGO.transform.parent)
            {
                root.SetActive(false);
                Debug.Log("[BossSceneUI] Hidden: " + root.name);
            }
        }

        Debug.Log("[BossSceneUI] UI setup complete!");
    }

    Canvas FindCanvasWithPersistentUI(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                PersistentUI persistent = root.GetComponentInChildren<PersistentUI>(true);
                if (persistent != null)
                {
                    Debug.Log("[BossSceneUI] Found Canvas with PersistentUI in " + root.name);
                    return canvas;
                }
            }
        }
        return null;
    }
}

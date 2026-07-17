using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseButton : MonoBehaviour
{
    public PauseMenu pauseMenu;
    public Button button;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);

    }

    void OnEnable()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        if (PauseMenu.Instance != null)
            pauseMenu = PauseMenu.Instance;
        else if (pauseMenu == null)
            pauseMenu = FindAnyObjectByType<PauseMenu>();
    }

    void OnClick()
    {
        if (PauseMenu.Instance != null)
            pauseMenu = PauseMenu.Instance;
        else if (pauseMenu == null)
            pauseMenu = FindAnyObjectByType<PauseMenu>();
        if (pauseMenu != null)
            pauseMenu.TogglePause();
        else
            Debug.LogWarning("[PauseButton] No PauseMenu found!");
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}

using UnityEngine;
using UnityEngine.UI;

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

    void OnClick()
    {
        if (pauseMenu != null)
            pauseMenu.TogglePause();
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}

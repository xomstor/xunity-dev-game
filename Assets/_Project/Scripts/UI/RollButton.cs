using UnityEngine;
using UnityEngine.UI;

public class RollButton : MonoBehaviour
{
    public PlayerController player;
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
        if (player != null)
            player.Roll();
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}

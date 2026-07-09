using UnityEngine;
using UnityEngine.UI;

public class ThrowSkillButton : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public PlayerThrowAbility throwAbility;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (throwAbility == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                throwAbility = pc.GetComponent<PlayerThrowAbility>();
        }

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (throwAbility != null)
            throwAbility.TryUse();
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}

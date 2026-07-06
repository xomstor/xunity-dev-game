using UnityEngine;
using UnityEngine.UI;

public class MoneySlapButton : MonoBehaviour
{
    [Header("Special Attack Button")]
    [Tooltip("Ссылка на компонент MoneySlapAttack на игроке")]
    public MoneySlapAttack moneySlap;
    [Tooltip("Если не назначен, ищется автоматически")]
    public AutoCombat playerCombat;
    public Button button;

    void Awake()
    {
        if (moneySlap == null)
            moneySlap = FindAnyObjectByType<MoneySlapAttack>();
        if (playerCombat == null)
            playerCombat = FindAnyObjectByType<AutoCombat>();
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    void Update()
    {
        if (button != null && moneySlap != null)
            button.interactable = moneySlap.CanCast();
    }

    void OnClick()
    {
        if (moneySlap == null) return;

        AutoCombat target = playerCombat != null && playerCombat.Target != null
            ? playerCombat.Target.GetComponent<AutoCombat>()
            : null;

        if (target == null)
            moneySlap.CastOnNearestEnemy();
        else
            moneySlap.Cast(target);
    }
}

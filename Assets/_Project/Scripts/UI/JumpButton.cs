using UnityEngine;
using UnityEngine.EventSystems;

public class JumpButton : MonoBehaviour, IPointerDownHandler
{
    public PlayerController player;

    void Start()
    {
        // Если PlayerController не привязан, ищем его в сцене
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[JumpButton] PlayerController not found!");
                return;
            }
        }
        player.Jump();
    }
}

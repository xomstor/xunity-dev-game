using UnityEngine;
using UnityEngine.EventSystems;

public class BlockButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerController player;

    void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!ResolvePlayer()) return;
        player.StartBlock();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!ResolvePlayer()) return;
        player.StopBlock();
    }

    bool ResolvePlayer()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[BlockButton] PlayerController not found!");
                return false;
            }
        }
        return true;
    }
}

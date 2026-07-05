using UnityEngine;

public class SwipeRollInput : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;

    [Header("Swipe Settings")]
    public float minSwipeDistance = 50f;
    public float maxSwipeTime = 0.5f;
    public bool detectMouseSwipe = true;

    private Vector2 swipeStartPosition;
    private float swipeStartTime;
    private bool isTrackingSwipe;

    void Update()
    {
        HandleTouchInput();

        if (detectMouseSwipe)
            HandleMouseInput();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            swipeStartPosition = touch.position;
            swipeStartTime = Time.time;
            isTrackingSwipe = true;
        }
        else if (touch.phase == TouchPhase.Ended && isTrackingSwipe)
        {
            isTrackingSwipe = false;
            TryTriggerRoll(touch.position);
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPosition = Input.mousePosition;
            swipeStartTime = Time.time;
            isTrackingSwipe = true;
        }
        else if (Input.GetMouseButtonUp(0) && isTrackingSwipe)
        {
            isTrackingSwipe = false;
            TryTriggerRoll(Input.mousePosition);
        }
    }

    void TryTriggerRoll(Vector2 endPosition)
    {
        float elapsed = Time.time - swipeStartTime;
        if (elapsed > maxSwipeTime) return;

        Vector2 delta = endPosition - swipeStartPosition;
        if (delta.magnitude < minSwipeDistance) return;

        if (player == null) return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            int direction = delta.x > 0 ? 1 : -1;
            player.Roll(direction);
        }
        else if (delta.y > 0)
        {
            player.Jump();
        }
        else if (delta.y < 0)
        {
            if (!player.TryDropThrough())
            {
                player.StartCrouch();
            }
        }
    }
}

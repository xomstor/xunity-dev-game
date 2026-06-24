using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("References")]
    public VirtualJoystick joystick;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
    }

    private void Update()
    {
        // Keyboard (editor/тест)
        moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1;
        }

        // Джойстик (приоритет если активен)
        if (joystick != null && joystick.IsActive)
        {
            moveInput = joystick.Direction;
        }

        // Тап по экрану (только если не трогаем джойстик)
        HandleTapMovement();
    }

    private Vector2 tapTarget;
    private bool movingToTap;

    private void HandleTapMovement()
    {
        if (Touchscreen.current == null) return;
        if (joystick != null && joystick.IsActive) return;

        var touches = Touchscreen.current.touches;
        if (touches.Count > 0 && touches[0].phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            Vector2 screenPos = touches[0].position.ReadValue();
            tapTarget = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            movingToTap = true;
        }

        if (movingToTap)
        {
            Vector2 dir = tapTarget - (Vector2)transform.position;
            if (dir.magnitude < 0.15f)
            {
                movingToTap = false;
                moveInput = Vector2.zero;
            }
            else
            {
                moveInput = dir.normalized;
            }
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }
}

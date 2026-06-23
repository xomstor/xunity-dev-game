using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    public float minZoom = 3f;
    public float maxZoom = 10f;
    public float zoomSpeed = 2f;

    [Header("Pan")]
    public float panSpeed = 0.5f;
    public Vector2 mapMin;
    public Vector2 mapMax;

    private Camera cam;
    private Vector3 lastPanPosition;
    private bool isPanning;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        Debug.Log($"CameraController cam: {cam}, size: {cam?.orthographicSize}");
    }

    private void Update()
    {
        HandleMouseInput();
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        // Zoom with scroll wheel
        float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        if (scroll != 0f)
        {
            Debug.Log($"Scroll: {scroll}, OrthoSize: {cam.orthographicSize}");
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Mathf.Sign(scroll) * zoomSpeed * 0.5f, minZoom, maxZoom);
        }

        // Pan with right mouse button
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame)
        {
            lastPanPosition = Mouse.current.position.ReadValue();
            isPanning = true;
        }
        if (Mouse.current.rightButton.wasReleasedThisFrame || Mouse.current.middleButton.wasReleasedThisFrame)
        {
            isPanning = false;
        }
        if (isPanning)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 delta = cam.ScreenToWorldPoint(lastPanPosition) - cam.ScreenToWorldPoint((Vector3)new Vector3(mousePos.x, mousePos.y, 0));
            transform.position = ClampPosition(transform.position + delta);
            lastPanPosition = mousePos;
        }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touches = Touchscreen.current.touches;

        if (touches.Count == 1)
        {
            var touch = touches[0];
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 delta = touch.delta.ReadValue();
                Vector3 worldDelta = cam.ScreenToWorldPoint(new Vector3(delta.x, delta.y, 0))
                                   - cam.ScreenToWorldPoint(Vector3.zero);
                transform.position = ClampPosition(transform.position - worldDelta);
            }
        }
        else if (touches.Count >= 2)
        {
            Vector2 pos0 = touches[0].position.ReadValue();
            Vector2 pos1 = touches[1].position.ReadValue();
            Vector2 delta0 = touches[0].delta.ReadValue();
            Vector2 delta1 = touches[1].delta.ReadValue();

            Vector2 prevPos0 = pos0 - delta0;
            Vector2 prevPos1 = pos1 - delta1;

            float prevDist = (prevPos0 - prevPos1).magnitude;
            float currDist = (pos0 - pos1).magnitude;
            float diff = prevDist - currDist;

            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + diff * zoomSpeed * 0.01f, minZoom, maxZoom);
        }
    }

    private Vector3 ClampPosition(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, mapMin.x, mapMax.x);
        pos.y = Mathf.Clamp(pos.y, mapMin.y, mapMax.y);
        pos.z = transform.position.z;
        return pos;
    }
}

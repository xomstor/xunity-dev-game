using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomMin = 2f;
    public float zoomMax = 8f;
    public float zoomSpeed = 0.5f;
    public float zoomSmoothing = 5f;

    private Camera cam;
    private float targetSize;

    void Awake()
    {
        cam = GetComponent<Camera>();
        targetSize = cam.orthographicSize;
    }

    void Update()
    {
        HandlePinchZoom();
        HandleScrollZoom();
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSmoothing);
    }

    void HandleScrollZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            targetSize = Mathf.Clamp(targetSize - scroll * zoomSpeed * 0.01f, zoomMin, zoomMax);
    }

    void HandlePinchZoom()
    {
        var touch = Touchscreen.current;
        if (touch == null) return;

        var t0 = touch.touches[0];
        var t1 = touch.touches[1];

        if (!t0.press.isPressed || !t1.press.isPressed) return;

        Vector2 pos0 = t0.position.ReadValue();
        Vector2 pos1 = t1.position.ReadValue();
        Vector2 prevPos0 = pos0 - t0.delta.ReadValue();
        Vector2 prevPos1 = pos1 - t1.delta.ReadValue();

        float prevDist = Vector2.Distance(prevPos0, prevPos1);
        float currDist = Vector2.Distance(pos0, pos1);

        float diff = prevDist - currDist;
        targetSize = Mathf.Clamp(targetSize + diff * zoomSpeed * 0.01f, zoomMin, zoomMax);
    }

    public void ZoomBy(float amount)
    {
        targetSize = Mathf.Clamp(targetSize - amount, zoomMin, zoomMax);
    }
}

using UnityEngine;

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
    }

    private void Update()
    {
        HandleMouseInput();
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed * 10f, minZoom, maxZoom);
        }

        // Pan with middle mouse or right mouse
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            lastPanPosition = Input.mousePosition;
            isPanning = true;
        }
        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }
        if (isPanning)
        {
            Vector3 delta = cam.ScreenToWorldPoint(lastPanPosition) - cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = ClampPosition(transform.position + delta);
            lastPanPosition = Input.mousePosition;
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                Vector3 delta = cam.ScreenToWorldPoint(new Vector3(touch.deltaPosition.x, touch.deltaPosition.y, 0));
                Vector3 origin = cam.ScreenToWorldPoint(Vector3.zero);
                transform.position = ClampPosition(transform.position - (delta - origin));
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prevT0 = t0.position - t0.deltaPosition;
            Vector2 prevT1 = t1.position - t1.deltaPosition;

            float prevDist = (prevT0 - prevT1).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
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

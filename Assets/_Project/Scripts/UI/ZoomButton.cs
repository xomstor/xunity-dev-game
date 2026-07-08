using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomButton : MonoBehaviour, IPointerDownHandler
{
    public CameraController cameraController;
    public float zoomAmount = 0.5f;

    void Start()
    {
        // Если CameraController не привязан, ищем его в сцене
        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cameraController == null)
        {
            cameraController = FindAnyObjectByType<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("[ZoomButton] CameraController not found!");
                return;
            }
        }
        cameraController.ZoomBy(zoomAmount);
    }
}

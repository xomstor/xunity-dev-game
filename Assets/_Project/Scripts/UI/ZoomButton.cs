using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomButton : MonoBehaviour, IPointerDownHandler
{
    public CameraController cameraController;
    public float zoomAmount = 0.5f;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cameraController == null) return;
        cameraController.ZoomBy(zoomAmount);
    }
}

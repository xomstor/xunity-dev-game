using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("Material")]
    public Material skyboxMaterial;

    [Header("Rotation")]
    public bool rotate = true;
    public float rotationSpeed = 1f;

    [Header("Exposure")]
    public bool pulseExposure;
    public float exposureMin = 0.3f;
    public float exposureMax = 0.7f;
    public float exposurePulseSpeed = 0.5f;

    float currentRotation;

    void OnEnable()
    {
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;
    }

    void Update()
    {
        Material mat = skyboxMaterial != null ? skyboxMaterial : RenderSettings.skybox;
        if (mat == null) return;

        if (rotate)
        {
            currentRotation += rotationSpeed * Time.deltaTime;
            if (currentRotation >= 360f) currentRotation -= 360f;
            mat.SetFloat("_Rotation", currentRotation);
        }

        if (pulseExposure)
        {
            float t = Mathf.PingPong(Time.time * exposurePulseSpeed, 1f);
            float exposure = Mathf.Lerp(exposureMin, exposureMax, t);
            mat.SetFloat("_Exposure", exposure);
        }
    }

    void OnDisable()
    {
        if (skyboxMaterial != null)
            skyboxMaterial.SetFloat("_Rotation", 0f);
    }
}

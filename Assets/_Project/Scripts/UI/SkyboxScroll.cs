using UnityEngine;

public class SkyboxScroll : MonoBehaviour
{
    [Tooltip("Material using the SkyboxGradient shader. If null, tries RenderSettings.skybox.")]
    public Material skyboxMaterial;

    [Tooltip("Speed of cloud layer 1")]
    public float cloudSpeed1 = 0.2f;
    [Tooltip("Speed of cloud layer 2")]
    public float cloudSpeed2 = 0.35f;

    private float offset1;
    private float offset2;

    void Start()
    {
        if (skyboxMaterial == null)
            skyboxMaterial = RenderSettings.skybox;
    }

    void Update()
    {
        if (skyboxMaterial == null) return;

        offset1 += cloudSpeed1 * Time.unscaledDeltaTime;
        offset2 += cloudSpeed2 * Time.unscaledDeltaTime;

        skyboxMaterial.SetFloat("_CloudOffset1", offset1);
        skyboxMaterial.SetFloat("_CloudOffset2", offset2);
    }
}

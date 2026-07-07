using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LavaController : MonoBehaviour
{
    [Header("Flow")]
    public float flowSpeed = 0.5f;
    public float flowScale = 4f;

    [Header("Surface")]
    public float waveSpeed = 1.5f;
    public float waveAmp = 0.04f;

    [Header("Bubbles")]
    public float bubbleSpeed = 1.2f;
    public float bubbleDensity = 15f;

    private Material lavaMaterial;
    private static readonly int FlowSpeedID = Shader.PropertyToID("_FlowSpeed");
    private static readonly int FlowScaleID = Shader.PropertyToID("_FlowScale");
    private static readonly int WaveSpeedID = Shader.PropertyToID("_WaveSpeed");
    private static readonly int WaveAmpID = Shader.PropertyToID("_WaveAmp");
    private static readonly int BubbleSpeedID = Shader.PropertyToID("_BubbleSpeed");
    private static readonly int BubbleDensityID = Shader.PropertyToID("_BubbleDensity");

    void Awake()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.material == null) return;
        lavaMaterial = sr.material;
        ApplyProperties();
    }

    void OnValidate()
    {
        ApplyProperties();
    }

    void ApplyProperties()
    {
        if (lavaMaterial == null) return;
        lavaMaterial.SetFloat(FlowSpeedID, flowSpeed);
        lavaMaterial.SetFloat(FlowScaleID, flowScale);
        lavaMaterial.SetFloat(WaveSpeedID, waveSpeed);
        lavaMaterial.SetFloat(WaveAmpID, waveAmp);
        lavaMaterial.SetFloat(BubbleSpeedID, bubbleSpeed);
        lavaMaterial.SetFloat(BubbleDensityID, bubbleDensity);
    }
}

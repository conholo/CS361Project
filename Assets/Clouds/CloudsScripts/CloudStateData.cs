using UnityEngine;

[CreateAssetMenu(menuName = "Create Cloud State Data", fileName = "CloudStateData", order = 0)]
public class CloudStateData : ScriptableObject
{
    [SerializeField] private CloudState _cloudState;
    [SerializeField] private float _cloudScale;
    [SerializeField] private float _densityOffset;
    [SerializeField] private float _densityMultiplier;
    [SerializeField] private float _darknessThreshold;
    [SerializeField] private float _absorptionThroughClouds;
    [SerializeField] private float _baseBrightness;
    [SerializeField] private float _exposure;
    [SerializeField] private float _phaseFactor;

    public CloudState CloudState => _cloudState;
    public float CloudScale => _cloudScale;
    public float DensityOffset => _densityOffset;
    public float DensityMultiplier => _densityMultiplier;
    public float DarknessThreshold => _darknessThreshold;
    public float AbsorptionThroughClouds => _absorptionThroughClouds;
    public float BaseBrightness => _baseBrightness;
    public float Exposure => _exposure;
    public float PhaseFactor => _phaseFactor;
}
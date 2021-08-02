using System;
using System.Collections;
using UnityEngine;

public class CloudMaster : MonoBehaviour
{
    [Header("Dynamic")]
    [SerializeField] private float _changeTime;
    [SerializeField] private CloudState _currentState;
    [SerializeField] private Material _skyMaterial;
    
    [Header("Main")]
    [SerializeField] private Shader _cloudShader;
    [SerializeField] private Texture2D _blueNoise;

    [Header("Cloud Shape")]
    [SerializeField] private Transform _container;
    [SerializeField] private float _cloudScale;
    [SerializeField] private float _densityOffset;
    [SerializeField] private float _densityMultiplier;
    [SerializeField] private Vector3 _shapeOffset;
    [SerializeField] private Vector4 _shapeNoiseWeights;
    [SerializeField] private Vector2 _heightOffset;
    [SerializeField] private float _rayOffsetStrength;

    [Header("Light")]
    [Range(0, 1)]
    [SerializeField] private float _lightAbsorptionTowardsSun;
    [SerializeField] private float _lightAbsorptionThroughCloud;
    [SerializeField] private int _lightStepsCount;
    [SerializeField] private float _darknessThreshold;
    [Range(0, 1)]
    [SerializeField] private float _forwardScattering;
    [Range(0, 1)]
    [SerializeField] private float _backScattering;
    [Range(0, 1)]
    [SerializeField] private float _baseBrightness;
    [Range(0, 1)]
    [SerializeField] private float _phaseFactor;
    [Range(0, 0.01f)]
    [SerializeField] private float _fogDistanceThreshold = 0.0001f;

    [SerializeField] private Color _skyColorA;
    [SerializeField] private Color _skyColorB;
    [SerializeField] private float _speed;
    [SerializeField] private float _fallOffDistance = 50;

    public Vector2 HeightOffset => _heightOffset;
    
    public Material Material { get; private set; }
    private CloudVisualDataContainer _cloudDataContainer;


    private static readonly int Map = Shader.PropertyToID("_WeatherMap");
    private static readonly int WorleyNoiseMap = Shader.PropertyToID("_WorleyNoiseMap");
    private static readonly int BlueNoise = Shader.PropertyToID("_BlueNoise");

    private static readonly int ShapeNoiseWeights = Shader.PropertyToID("_ShapeNoiseWeights");
    private static readonly int ShapeOffset = Shader.PropertyToID("_ShapeOffset");
    private static readonly int BoundsMin = Shader.PropertyToID("_BoundsMin");
    private static readonly int BoundsMax = Shader.PropertyToID("_BoundsMax");
    private static readonly int CloudScale = Shader.PropertyToID("_CloudScale");
    private static readonly int DensityOffset = Shader.PropertyToID("_DensityOffset");
    private static readonly int DensityMultiplier = Shader.PropertyToID("_DensityMultiplier");
    private static readonly int LightStepsCount = Shader.PropertyToID("_LightStepsCount");
    private static readonly int LightAbsorptionTowardsSun = Shader.PropertyToID("_LightAbsorptionTowardsSun");
    private static readonly int LightAbsorptionThroughCloud = Shader.PropertyToID("_LightAbsorptionThroughCloud");
    private static readonly int DarknessThreshold = Shader.PropertyToID("_DarknessThreshold");
    private static readonly int SkyColorA = Shader.PropertyToID("_SkyColorA");
    private static readonly int SkyColorB = Shader.PropertyToID("_SkyColorB");
    private static readonly int Speed = Shader.PropertyToID("_Speed");
    private static readonly int FallOffDistance = Shader.PropertyToID("_FallOffDistance");
    private static readonly int RayOffsetStrength = Shader.PropertyToID("_RayOffsetStrength");
    private static readonly int PhaseParams = Shader.PropertyToID("_PhaseParams");
    private static readonly int FogDistanceThreshold = Shader.PropertyToID("_FogDistanceThreshold");

    private WorleyNoiseGenerator _worleyNoiseGenerator;
    private WeatherMap _weatherMap;
    
    
    private void Awake()
    {
        _worleyNoiseGenerator = FindObjectOfType<WorleyNoiseGenerator>();
        _weatherMap = FindObjectOfType<WeatherMap>();
        _cloudDataContainer = FindObjectOfType<CloudVisualDataContainer>();
    }


    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (Material == null || Material.shader != _cloudShader)
            Material = new Material(_cloudShader);

        _worleyNoiseGenerator.GenerateWorleyNoise();
        Material.SetTexture(WorleyNoiseMap, _worleyNoiseGenerator.ShapeTexture);
        
        _weatherMap.GenerateWeatherMap();
        Material.SetTexture(Map, _weatherMap.Map);
        Material.SetTexture(BlueNoise, _blueNoise);
        
        Material.SetVector(BoundsMin, _container.position - _container.localScale / 2);
        Material.SetVector(BoundsMax, _container.position + _container.localScale / 2);
        Material.SetVector(ShapeNoiseWeights, _shapeNoiseWeights);
        Material.SetVector(ShapeOffset, _shapeOffset);
        Material.SetVector(PhaseParams, new Vector4(_forwardScattering, _backScattering, _baseBrightness, _phaseFactor));
        
        Material.SetFloat(CloudScale, _cloudScale);
        Material.SetFloat(RayOffsetStrength, _rayOffsetStrength);
        Material.SetFloat(DensityOffset, _densityOffset);
        Material.SetFloat(DensityMultiplier, _densityMultiplier);
        Material.SetFloat(LightStepsCount, _lightStepsCount);
        Material.SetFloat(DarknessThreshold, _darknessThreshold);
        Material.SetFloat(Speed, _speed);
        Material.SetFloat(FallOffDistance, _fallOffDistance);
        Material.SetFloat(LightAbsorptionTowardsSun, _lightAbsorptionTowardsSun);
        Material.SetFloat(LightAbsorptionThroughCloud, _lightAbsorptionThroughCloud);
        Material.SetFloat(FogDistanceThreshold, _fogDistanceThreshold);
        
        Material.SetVector(SkyColorA, _skyColorA);
        Material.SetVector(SkyColorB, _skyColorB);

        Graphics.Blit(src, dest, Material);
    }

    
    private Coroutine _cloudUpdateRoutine;

    public void ChangeCloudStates(CloudState cloudState)
    {
        if (_cloudUpdateRoutine != null)
        {
            StopCoroutine(_cloudUpdateRoutine);
        }
        
        var targetData = _cloudDataContainer.GetData(cloudState);
        
        _cloudUpdateRoutine = StartCoroutine(InterpolateCloudState(targetData));
    }
    
    private IEnumerator InterpolateCloudState(CloudStateData targetData)
    {
        var elapsedTime = 0.0f;

        var startingScale = _cloudScale;
        var startingDensityMultiplier = _densityMultiplier;
        var startingDensityOffset = _densityOffset;
        var startingDarknessThreshold = _darknessThreshold;
        var staringAbsorptionThroughClouds = _lightAbsorptionThroughCloud;
        var startingBaseBrightness = _baseBrightness;
        var startingSkyExposure = _skyMaterial.GetFloat("_Exposure");
        var startingPhaseFactor = _phaseFactor;

        while (elapsedTime < _changeTime)
        {
            var percent = elapsedTime / _changeTime;
            
            _cloudScale = Mathf.Lerp(startingScale, targetData.CloudScale, percent);
            _densityMultiplier = Mathf.Lerp(startingDensityMultiplier, targetData.DensityMultiplier, percent);
            _densityOffset = Mathf.Lerp(startingDensityOffset, targetData.DensityOffset, percent);
            _darknessThreshold = Mathf.Lerp(startingDarknessThreshold, targetData.DarknessThreshold, percent);
            _lightAbsorptionThroughCloud = Mathf.Lerp(staringAbsorptionThroughClouds, targetData.AbsorptionThroughClouds, percent);
            _baseBrightness = Mathf.Lerp(startingBaseBrightness, targetData.BaseBrightness, percent);
            _skyMaterial.SetFloat("_Exposure", Mathf.Lerp(startingSkyExposure, targetData.Exposure, percent));
            _phaseFactor = Mathf.Lerp(startingPhaseFactor, targetData.PhaseFactor, percent);
            
            elapsedTime += Time.deltaTime;
            
            yield return null;
        }

        _cloudUpdateRoutine = null;
    }
}

public enum CloudState
{
    Clear,
    Scattered,
    Broken,
    Overcast,
    LightRain,
    Shower,
    Thunderstorm
}
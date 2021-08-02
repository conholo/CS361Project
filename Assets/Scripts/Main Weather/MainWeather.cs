using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainWeather : MonoBehaviour
{
    [SerializeField] private float _updateSimulationSpeed;
    [Header("Min Max Parameters")] 
    [SerializeField] private float _maxWind = 50;
    [SerializeField] private float _windMultiplier = 2;
    [SerializeField] private float _treeSwayAttenuation = 3;

    [SerializeField] private float _degrees;
    [SerializeField] private float _speed;
    [SerializeField] private float _temperature;
    [SerializeField] private float _defaultWaterSpeed = 0.075f;

    [SerializeField] private MeshRenderer _grassMesh;
    [SerializeField] private MeshRenderer _treeMesh;
    [SerializeField] private MeshRenderer _waterMesh;
    [SerializeField] private CloudMaster _cloudMaster;

    [Range(0, 1)]
    [SerializeField] private float _cloudPercent;


    private float _grassWindStrength;
    private float _grassFrequencyX;
    private float _grassFrequencyY;

    private float _swayAmplitude;
    private float _swayAngle;

    private float _waterScrollSpeed;
    private float _windDegrees;

    private Coroutine _updateSimCoroutine;

    private WeatherSimulationData _lastReceivedData;


    private void Awake()
    {
        WeatherRequester.OnSimulationChanged += UpdateSimulation;
    }

    private void OnDestroy()
    {
        WeatherRequester.OnSimulationChanged -= UpdateSimulation;
    }

    private void Start()
    {
        UpdateLastValues();
    }

    private void UpdateLastValues()
    {
        _grassWindStrength = _grassMesh.sharedMaterial.GetFloat("_WindStrength");
        _grassFrequencyX = _grassMesh.sharedMaterial.GetFloat("_WindFrequencyX");
        _grassFrequencyY = _grassMesh.sharedMaterial.GetFloat("_WindFrequencyY");

        _swayAmplitude = _treeMesh.sharedMaterial.GetFloat("_Amplitude");
        _swayAngle = _treeMesh.sharedMaterial.GetFloat("_WindAngle");

        _waterScrollSpeed = _waterMesh.sharedMaterial.GetFloat("_ScrollSpeed");
        _windDegrees = 0;
    }

    public void RevertToDefault()
    {
        if(_updateSimCoroutine != null)
            StopCoroutine(_updateSimCoroutine);

        if (!_lastReceivedData.Initialized)
            _lastReceivedData = new WeatherSimulationData(new List<string> {"clear"}, 50.0, 5.0, 0, 0, TimeSpan.Zero);

        var cloudState = GetCloudStateFromDescription(_lastReceivedData.Descriptions[0], _lastReceivedData.CloudPercent);
        
        _cloudMaster.ChangeCloudStates(cloudState);

        _updateSimCoroutine = StartCoroutine(UpdateSimulationRoutine(_lastReceivedData));
    }

    private void UpdateSimulation(WeatherSimulationData simulationData)
    {
        if(_updateSimCoroutine != null)
            StopCoroutine(_updateSimCoroutine);

        var cloudState = GetCloudStateFromDescription(simulationData.Descriptions[0], simulationData.CloudPercent);
        
        _cloudMaster.ChangeCloudStates(cloudState);

        _updateSimCoroutine = StartCoroutine(UpdateSimulationRoutine(simulationData));
    }

    public void UpdateTemperature(float temperature)
    {
        var waterTemperaturePercent = Mathf.InverseLerp(32, 40, temperature);
        var waterSpeedEffect = Mathf.Lerp(0, 1, waterTemperaturePercent * waterTemperaturePercent * (3f - 2f * waterTemperaturePercent));
        var currentWaterSpeed = _defaultWaterSpeed * waterSpeedEffect;

        _waterMesh.sharedMaterial.SetFloat("_ScrollSpeed", currentWaterSpeed);
    }
    
    public void UpdateWindDegree(float windDegree)
    {
        var grassUnitPosition = new Vector2(Mathf.Cos((windDegree + 90f) * Mathf.Deg2Rad), -Mathf.Sin((windDegree + 90f) * Mathf.Deg2Rad));
        var grassX = Remap(grassUnitPosition.x, -1.0f, 1.0f, -0.05f, 0.05f);
        var grassY = Remap(grassUnitPosition.y, -1.0f, 1.0f, -0.05f, 0.05f);

        _treeMesh.sharedMaterial.SetFloat("_WindAngle", windDegree);
        
        _grassMesh.sharedMaterial.SetFloat("_WindFrequencyX", grassX);
        _grassMesh.sharedMaterial.SetFloat("_WindFrequencyY", grassY);
    }

    public void UpdateWindStrength(float strength)
    {
        var windPercent = strength / _maxWind;
        
        var grassWindSpeed = windPercent * _windMultiplier;
        var treeSwayAmplitude = windPercent / _treeSwayAttenuation;
        _treeMesh.sharedMaterial.SetFloat("_Amplitude", treeSwayAmplitude);
        _grassMesh.sharedMaterial.SetFloat("_WindStrength", grassWindSpeed);
    }

    private CloudState GetCloudStateFromDescription(string description, int percent)
    {
        if (description.Contains("clear"))
            return CloudState.Clear;
        if (description.Contains("thunderstorm"))
            return CloudState.Thunderstorm;

        if (description.Contains("rain"))
        {
            if(description.Contains("light") || description.Contains("drizzle"))
                return CloudState.LightRain;
            return CloudState.Shower;
        }

        if (percent >= 11 && percent < 50)
            return CloudState.Scattered;
        if (percent >= 50 && percent < 84)
            return CloudState.Broken;
        return CloudState.Overcast;
    }

    private IEnumerator UpdateSimulationRoutine(WeatherSimulationData weatherSimulationData)
    {
        var elapsed = 0f;

        var windMPH = weatherSimulationData.WindSpeed * 2.23694;
        var windPercent = (float)windMPH / _maxWind;
        
        var grassWindSpeed = windPercent * _windMultiplier;
        var grassUnitPosition = new Vector2(Mathf.Cos((weatherSimulationData.WindDegrees + 90f) * Mathf.Deg2Rad), -Mathf.Sin((weatherSimulationData.WindDegrees + 90f) * Mathf.Deg2Rad));
        var grassX = Remap(grassUnitPosition.x, -1.0f, 1.0f, -0.05f, 0.05f);
        var grassY = Remap(grassUnitPosition.y, -1.0f, 1.0f, -0.05f, 0.05f);

        var treeSwayAmplitude = windPercent / _treeSwayAttenuation;

        var fTemp = (float)((weatherSimulationData.Temperature -273.15) * (9f / 5) + 32);
        var waterTemperaturePercent = Mathf.InverseLerp(32, 40, fTemp);
        var waterSpeedEffect = Mathf.Lerp(0, 1, waterTemperaturePercent * waterTemperaturePercent * (3f - 2f * waterTemperaturePercent));
        var currentWaterSpeed = _defaultWaterSpeed * waterSpeedEffect;

        
        while (elapsed < _updateSimulationSpeed)
        {
            elapsed += Time.deltaTime;

            var percent = elapsed / _updateSimulationSpeed;

            var grassWindStrength = Mathf.Lerp(_grassWindStrength, grassWindSpeed, percent);
             _grassMesh.sharedMaterial.SetFloat("_WindStrength", grassWindStrength);

             var windFrequencyX = Mathf.Lerp(_grassFrequencyX, grassX, percent);
             var windFrequencyY = Mathf.Lerp(_grassFrequencyY, grassY, percent);
             
             _grassMesh.sharedMaterial.SetFloat("_WindFrequencyX", windFrequencyX);
             _grassMesh.sharedMaterial.SetFloat("_WindFrequencyY", windFrequencyY);

             var swayAmplitude = Mathf.Lerp(_swayAmplitude, treeSwayAmplitude, percent);
             var swayAngle = Mathf.Lerp(_swayAngle, weatherSimulationData.WindDegrees, percent);
             
             _treeMesh.sharedMaterial.SetFloat("_Amplitude", swayAmplitude);
             _treeMesh.sharedMaterial.SetFloat("_WindAngle", swayAngle);

             var waterSpeed = Mathf.Lerp(_waterScrollSpeed, currentWaterSpeed, percent);

             _waterMesh.sharedMaterial.SetFloat("_ScrollSpeed", waterSpeed);
             
            yield return null;
        }
     
        _grassMesh.sharedMaterial.SetFloat("_WindStrength", grassWindSpeed);
        _grassMesh.sharedMaterial.SetFloat("_WindFrequencyX", grassX);
        _grassMesh.sharedMaterial.SetFloat("_WindFrequencyY", grassY);
        _treeMesh.sharedMaterial.SetFloat("_Amplitude", treeSwayAmplitude);
        _treeMesh.sharedMaterial.SetFloat("_WindAngle", weatherSimulationData.WindDegrees);

        if (currentWaterSpeed < 0.01)
            currentWaterSpeed = 0;
        _waterMesh.sharedMaterial.SetFloat("_ScrollSpeed", currentWaterSpeed);


        UpdateLastValues();

        _updateSimCoroutine = null;
    }

    private string CompassWind(float windDegrees)
    {
        var points = new[]
        {
            "North", "North North East", "North East", "East North East",
            "East", "East South East", "South East", "South South East",
            "South", "South South West", "South West", "West South West",
            "West", "West North West", "North West", "North North West"
        };
        
        var rawPosition = Mathf.Floor((windDegrees + 360f / 16 / 2) % 360) / (360.0f / 16);
        var arrayPosition = (int)(rawPosition % 16);
        return points[arrayPosition];
    }

    private Vector2 WindDirectionFromCompass(float degrees)
    {
        return new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
    }
    
    private float GetWindMPH(float mps)
    {
        var mph = (float)Math.Round(2.23694 * mps, 2);
        return mph;
    }

    private static float Remap (float from, float fromMin, float fromMax, float toMin,  float toMax)
    {
        var fromAbs  =  from - fromMin;
        var fromMaxAbs = fromMax - fromMin;      
       
        var normal = fromAbs / fromMaxAbs;
 
        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;
 
        var to = toAbs + toMin;
       
        return to;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SandboxUI : MonoBehaviour
{
    [SerializeField] private Slider _temperatureSlider;
    [SerializeField] private Slider _windDegreeSlider;
    [SerializeField] private Slider _windStrengthSlider;
    [SerializeField] private TMP_Dropdown _cloudType;

    private CloudMaster _cloudMaster;
    private MainWeather _mainWeather;
    
    
    private readonly Dictionary<string, CloudState> _cloudNameToState = new Dictionary<string, CloudState>
    {
        {"Clear", CloudState.Clear},
        {"Scattered", CloudState.Scattered},
        {"Broken", CloudState.Broken},
        {"Overcast", CloudState.Overcast},
        {"Light Rain", CloudState.LightRain},
        {"Shower", CloudState.Shower},
        {"Thunderstorm", CloudState.Thunderstorm},
    };
    
    private void Awake()
    {
        _cloudMaster = FindObjectOfType<CloudMaster>();
        _mainWeather = FindObjectOfType<MainWeather>();
        
        _cloudType.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Clear"),
            new TMP_Dropdown.OptionData("Scattered"),
            new TMP_Dropdown.OptionData("Broken"),
            new TMP_Dropdown.OptionData("Overcast"),
            new TMP_Dropdown.OptionData("Light Rain"),
            new TMP_Dropdown.OptionData("Shower"),
            new TMP_Dropdown.OptionData("Thunderstorm"),
        };
        
        _cloudType.onValueChanged.AddListener(NotifyOnCloudChanged);
        _temperatureSlider.onValueChanged.AddListener(NotifyOnTempChanged);
        _windDegreeSlider.onValueChanged.AddListener(NotifyOnWindDegreeChanged);
        _windStrengthSlider.onValueChanged.AddListener(NotifyOnWindStrengthChanged);
    }

    private void NotifyOnWindStrengthChanged(float windStrength)
    {
        _mainWeather.UpdateWindStrength(windStrength);
    }

    private void NotifyOnWindDegreeChanged(float windDegrees)
    {
        _mainWeather.UpdateWindDegree(windDegrees);
    }

    private void NotifyOnTempChanged(float temperature)
    {
        _mainWeather.UpdateTemperature(temperature);
    }
    

    private void NotifyOnCloudChanged(int cloudIndex)
    {
        var cloudName = _cloudType.options[cloudIndex].text;

        var cloudState = _cloudNameToState[cloudName];
        
        _cloudMaster.ChangeCloudStates(cloudState);
    }
}

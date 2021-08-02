using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeatherRequestSubmitFormUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _cityInputField;
    [SerializeField] private TMP_Text _invalidCityText;
    [SerializeField] private Button _submitRequestButton;
    [SerializeField] private Transform _dataTransformParent;
    
    private WeatherRequester _weatherRequester;

    private List<Toggle> _dataToggles = new List<Toggle>();
    
    private void Awake()
    {
        _submitRequestButton.interactable = false;
        _cityInputField.onValueChanged.AddListener(ValidateCityInput);

        _weatherRequester = FindObjectOfType<WeatherRequester>();

        _dataToggles = _dataTransformParent.GetComponentsInChildren<Toggle>().ToList();
    }

    private void ValidateCityInput(string cityInputText)
    {
        var isValid = Regex.IsMatch(cityInputText, @"^[a-zA-Z]+$");

        _submitRequestButton.interactable = isValid;

        _invalidCityText.SetText(isValid ? "" : "Please enter a valid city name.");
        _invalidCityText.enabled = !isValid;
    }

    public void SubmitRequest()
    {
        var selectedToggles = _dataToggles.Where(t => t.isOn).Select(t => t.name).ToList();

        var submitData = new SubmitData {SelectedDataToggles = selectedToggles, CityInput = _cityInputField.text};
        _weatherRequester.SubmitRequest(submitData);
    }
}


public class SubmitData
{
    public List<string> SelectedDataToggles;
    public string CityInput;
}
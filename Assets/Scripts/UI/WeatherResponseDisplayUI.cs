using TMPro;
using UnityEngine;

public class WeatherResponseDisplayUI : MonoBehaviour
{
    [SerializeField] private Transform _sandboxParent;
    [SerializeField] private Transform _requestFormParent;
    [SerializeField] private TMP_Text _weatherDescriptionText;
    
    private void Awake()
    {
        WeatherRequester.OnRequestReceived += UpdateUIOnRequestReceived;
    }

    private void Start()
    {
        _requestFormParent.gameObject.SetActive(false);
        _sandboxParent.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        WeatherRequester.OnRequestReceived -= UpdateUIOnRequestReceived;
    }

    private void UpdateUIOnRequestReceived(string weatherDescription)
    {
        _weatherDescriptionText.SetText(weatherDescription);
        _requestFormParent.gameObject.SetActive(false);
    }

    public void ToggleRequestForm()
    {
        _requestFormParent.gameObject.SetActive(!_requestFormParent.gameObject.activeSelf);
    }

    public void ToggleSandboxForm()
    {
        _sandboxParent.gameObject.SetActive(!_sandboxParent.gameObject.activeSelf);
    }
}
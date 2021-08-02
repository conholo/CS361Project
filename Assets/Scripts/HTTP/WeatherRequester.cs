using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class WeatherRequester : MonoBehaviour
{
    private const string URL = "http://rampantredpanda.me:4474/city/";
    private bool _requestInProgress;
    public static event Action<string> OnRequestReceived;
    public static event Action<WeatherSimulationData> OnSimulationChanged;

    private Coroutine _requestRoutine;
    
    public void SubmitRequest(SubmitData submitData)
    {
        if (_requestRoutine != null)
        {
            StopCoroutine(_requestRoutine);
        }

        _requestRoutine = StartCoroutine(Request(submitData));
    }
    
    private IEnumerator Request(SubmitData submitData)
    {
        var webRequest = UnityWebRequest.Get(URL + submitData.CityInput);

        yield return webRequest.SendWebRequest();
        
        if(webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.DataProcessingError)
            Debug.LogError($"Network Error: {webRequest.error}");
        else
        {
            var weather = JsonConvert.DeserializeObject<WeatherMainData>(webRequest.downloadHandler.text);

            var description = $"{submitData.CityInput}\n\n";
            description += weather.GetDescriptionsFromSelections(submitData.SelectedDataToggles);
            
            OnRequestReceived?.Invoke(description);

            var descriptions = weather.Weather.Select(t => t.Description).ToList();

            var time = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var timeFormat = time.AddSeconds(weather.GeographicDescription.DT).ToLocalTime().TimeOfDay;
            
            var simData = new WeatherSimulationData(descriptions, weather.Weather[0].Temp, weather.Wind.Speed,
                weather.Wind.Deg, weather.Clouds.All, timeFormat);
            
            OnSimulationChanged?.Invoke(simData);

            _requestRoutine = null;
        }
    }
}


public struct WeatherSimulationData
{
    public double Temperature { get; }
    public double WindSpeed { get; }
    public int WindDegrees { get; }
    public int CloudPercent { get; }
    
    public bool Initialized { get; }
    
    public TimeSpan Time { get; }
    
    public List<string> Descriptions { get; }

    public WeatherSimulationData(List<string> descriptions, double temperatures, double windSpeed, int windDegrees, int cloudPercent, TimeSpan time)
    {
        Descriptions = descriptions;
        Temperature = temperatures;
        WindSpeed = windSpeed;
        WindDegrees = windDegrees;
        CloudPercent = cloudPercent;
        Time = time;

        Initialized = true;
    }
}

public class GeographicDescription
{
    public double Lon { get; set; }
    public double Lat { get; set; }
    public string Country { get; set; }
    public int DT { get; set; }
    public int Sunrise { get; set; }
    public int Sunset { get; set; }
    public int Timezone { get; set; }
    
    public override string ToString()
    {
        var result = "Main Weather Description:\n";
        result += $"Lat/Long: {Lat}/{Lon}\n";
        result += $"Country: {Country}\n";

        var sunrise = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var sunriseTime = sunrise.AddSeconds(Sunrise).ToLocalTime().TimeOfDay;
        result += $"Sunrise: {sunriseTime}\n";
        
        var sunset = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var sunsetTime = sunset.AddSeconds(Sunset).ToLocalTime().TimeOfDay;
        result += $"Sunset: {sunsetTime}\n";

        var time = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var timeFormat = time.AddSeconds(DT).ToLocalTime().TimeOfDay;
        result += $"Time: {timeFormat}\n";

        var timeZone = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        var timeZoneTime = timeZone.AddSeconds(Timezone).ToLocalTime().TimeOfDay;

        result += $"Timezone: {timeZoneTime}\n";

        return result;
    }

    public string GetStandardGeoDescription()
    {
        var result = string.Empty;
        result += $"Country: {Country}\n";
        var time = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var timeFormat = time.AddSeconds(DT).ToLocalTime().TimeOfDay;
        result += $"Time: {timeFormat}\n";

        return result;
    }

    public string GetTimezoneDescription()
    {
        var result = string.Empty;
        var timeZone = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        var timeZoneTime = timeZone.AddSeconds(Timezone).ToLocalTime().TimeOfDay;

        result += $"Timezone: {timeZoneTime}\n";
        return result;
    }

    public string GetLatitudeLongitudeDescription()
    {
        var result = string.Empty;
        result += $"Lat/Long: {Lat}/{Lon}\n";
        return result;
    }
    
    public string GetSunriseSunsetDescription()
    {
        var result = string.Empty;
        var sunrise = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var sunriseTime = sunrise.AddSeconds(Sunrise).ToLocalTime().TimeOfDay;
        result += $"Sunrise: {sunriseTime}\n";
        
        var sunset = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var sunsetTime = sunset.AddSeconds(Sunset).ToLocalTime().TimeOfDay;
        result += $"Sunset: {sunsetTime}\n";
        return result;
    }

}

public class Weather
{
    public string Description { get; set; }
    public double Temp { get; set; }
    public double feels_like { get; set; }
    public double temp_min { get; set; }
    public double temp_max { get; set; }
    public int Pressure { get; set; }
    public int Humidity { get; set; }

    public override string ToString()
    {
        var result = "Main Weather Description:\n";
        result += $"Description: {Description}\n";
        result += $"Current Temperature: {Temp}\n";
        result += $"Feels Like: {feels_like}\n";
        result += $"Low: {temp_min}\n";
        result += $"High: {temp_max}\n";
        result += $"Pressure: {Pressure}\n";
        result += $"Humidity: {Humidity}\n";
        result += "\n";

        return result;
    }

    public string GetMainDescription()
    {
        var result = string.Empty;
        result += $"Description: {Description}\n";
        return result;
    }

    public string GetTempDescription()
    {
        var result = string.Empty;
        result += $"Current Temperature: {Temp}\n";
        result += $"Feels Like: {feels_like}\n";
        result += $"Low: {temp_min}\n";
        result += $"High: {temp_max}\n";

        return result;
    }

    public string GetPressureDescription()
    {
        return $"Pressure: {Pressure}\n";
    }
    
    public string GetHumidityDescription()
    {
        return $"Humidity: {Humidity}\n";
    }

}

public class Wind
{
    public double Speed { get; set; }
    public int Deg { get; set; }

    public override string ToString()
    {
        return $"Wind Degree: {Deg}\n Wind Speed: {Speed}\n";
    }
}

public class Clouds
{
    public int All { get; set; }

    public override string ToString()
    {
        return $"Cloud Coverage (%): {All}.\n";
    }
}

public class Rain
{
    [JsonProperty("rain.1h")]
    public double Rain1H { get; set; }

    public override string ToString()
    {
        return $"Rain Accumulation (last hour): {Rain1H}.\n";
    }
}

public class Snow
{
    [JsonProperty("snow.1h")]
    public double Snow1H { get; set; }
    
    public override string ToString()
    {
        return $"Snow Accumulation (last hour): {Snow1H}.\n";
    }
}

public class WeatherMainData
{
    [JsonProperty("geographic description")]
    public GeographicDescription GeographicDescription { get; set; }
    public List<Weather> Weather { get; set; }
    public Wind Wind { get; set; }
    public Clouds Clouds { get; set; }
    public Rain Rain { get; set; }
    public Snow Snow { get; set; }

    public override string ToString()
    {
        var result = string.Empty;

        result += "Weather Descriptions:\n\n";

        result += GeographicDescription;

        foreach (var weather in Weather)
            result += weather;

        result += Wind;
        result += Clouds;
        result += Rain;
        result += Snow;

        return result;
    }

    public string GetDescriptionsFromSelections(List<string> selections)
    {
        var result = GeographicDescription.GetStandardGeoDescription();
        result += Weather[0].GetMainDescription();
        
        if (selections.Contains("Lat/Lon"))
            result += GeographicDescription.GetLatitudeLongitudeDescription();
        if (selections.Contains("Sunrise/Sunset"))
            result += GeographicDescription.GetSunriseSunsetDescription();
        if (selections.Contains("Time Zone"))
            result += GeographicDescription.GetTimezoneDescription();

        if (selections.Contains("Temperature"))
            result += Weather[0].GetTempDescription();
        if (selections.Contains("Humidity"))
            result += Weather[0].GetHumidityDescription();
        if (selections.Contains("Pressure"))
            result += Weather[0].GetPressureDescription();
        if (selections.Contains("Wind"))
            result += Wind.ToString();
        if (selections.Contains("Clouds"))
            result += Clouds.ToString();
        if (selections.Contains("Rain 1h"))
            result += Rain.ToString();
        if (selections.Contains("Snow 1h"))
            result += Snow.ToString();

        return result;
    }
}
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorleyNoiseGenerator))]
public class WorleyMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var noiseGenerator = target as WorleyNoiseGenerator;

        if (noiseGenerator == null) return;
        
        if (noiseGenerator == null) return;
        if(GUILayout.Button("Generate"))
            noiseGenerator.GenerateWorleyNoise();
        if(GUILayout.Button("Save"))
            noiseGenerator.SaveTexture();
        if(GUILayout.Button("Load"))
            noiseGenerator.LoadTexture();
    }
}

[CustomEditor(typeof(WeatherMap))]
public class WeatherMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var weatherMap = target as WeatherMap;

        if (weatherMap == null) return;
        
        if (GUILayout.Button("Update Texture"))
        {
            weatherMap.UpdateMap();
        }
    }
}
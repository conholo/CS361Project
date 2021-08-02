using UnityEngine;

public class TestRenderTexture : MonoBehaviour
{
    private WorleyNoiseGenerator _worleyNoiseGenerator;
    private WeatherMap _weatherMap;
    
    
    private void Awake()
    {
        _worleyNoiseGenerator = FindObjectOfType<WorleyNoiseGenerator>();
        _weatherMap = FindObjectOfType<WeatherMap>();
    }

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        _worleyNoiseGenerator.GenerateWorleyNoise();
        
        
        Graphics.Blit(src, dest);
    }
}
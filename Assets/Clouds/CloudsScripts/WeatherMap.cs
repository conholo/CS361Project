using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WeatherMap : MonoBehaviour
{
    private const int Resolution = 512;

    [SerializeField] private SimplexNoiseSettings _noiseSettings;
    [SerializeField] private ComputeShader _weatherMapCompute;
    [SerializeField] private Transform _container;
    
    [SerializeField] private RenderTexture _weatherMap;
    public Texture Map => _weatherMap;

    private List<ComputeBuffer> _buffersToRelease;

    private bool _initialized;

    public void GenerateWeatherMap()
    {
        if (_initialized) return;
        
        // Create the 16161616 texture
        CreateMapTexture(ref _weatherMap, Resolution);

        _buffersToRelease = new List<ComputeBuffer>();

        // Create offset buffer which is used to offset simplex noise.
        var prng = new System.Random(_noiseSettings.Seed);
        var offsets = new Vector4[_noiseSettings.LayerCount];

        for (var i = 0; i < offsets.Length; i++)
        {
            var offset = new Vector4((float) prng.NextDouble(), (float) prng.NextDouble(), (float) prng.NextDouble(),
                (float) prng.NextDouble());
            offsets[i] = (offset * 2 - Vector4.one) * 1000 + (Vector4) _container.position;
        }
        CreateBuffer(offsets, sizeof(float) * 4, "_Offsets");

        // Create noise data to be used by the GPU.
        var noiseSettingsData = _noiseSettings.GetData();
        noiseSettingsData.Offset += FindObjectOfType<CloudMaster>().HeightOffset;
        
        CreateBuffer(new[] {noiseSettingsData}, _noiseSettings.Stride, "_WeatherMapNoiseSettings");
        
        // Set compute parameters.
        _weatherMapCompute.SetTexture(0, "_Result", _weatherMap);
        _weatherMapCompute.SetInt("_Resolution", Resolution);

        
        // Set thread groups and dispatch.
        var threadGroupSize = 16;
        var threadGroupCount = Mathf.CeilToInt(Resolution / (float) threadGroupSize);
        
        _weatherMapCompute.Dispatch(0, threadGroupCount, threadGroupCount, 1);
        
        _buffersToRelease.ForEach(t => t.Release());

        _initialized = true;
    }

    private void CreateBuffer(System.Array data, int stride, string bufferName, int kernel = 0)
    {
        var buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Raw);
        buffer.SetData(data);
        _buffersToRelease.Add(buffer);
        _weatherMapCompute.SetBuffer(kernel, bufferName, buffer);
    }
    
    private void CreateMapTexture(ref RenderTexture texture, int resolution)
    {
        var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;

        if (texture == null || texture.width != resolution || texture.height != resolution || !texture.IsCreated() ||
            texture.graphicsFormat != format)
        {
            if(texture != null)
                texture.Release();

            texture = new RenderTexture(resolution, resolution, 0)
            {
                graphicsFormat = format,
                volumeDepth = resolution,
                enableRandomWrite = true,
                dimension = TextureDimension.Tex2D,
                name = name
            };

            texture.Create();
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
    }

    public void UpdateMap()
    {
        _weatherMap.Release();
        GenerateWeatherMap();
    }
}
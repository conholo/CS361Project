using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class WorleyNoiseGenerator : MonoBehaviour
{
    private const int ComputeThreadGroupSize = 8;
    private const int ShapeResolution = 132;
    
    [SerializeField] private ComputeShader _worleyCompute;
    [SerializeField] private WorleyNoiseSettings[] _shapeSettings;

    private List<ComputeBuffer> _releaseBuffers;
    
    [SerializeField]
    private RenderTexture _shapeTexture;
    public RenderTexture ShapeTexture => _shapeTexture;
    
    private TextureSaveLoader _saveLoader;
    private bool _initialized;
    private const string SaveName = "Shape Texture";

    public void GenerateWorleyNoise()
    {
        if (_saveLoader == null)
            _saveLoader = FindObjectOfType<TextureSaveLoader>();

        if (_initialized) return;
        
        Create3DNoiseTexture(ref _shapeTexture);
        
        foreach (var shapeChannel in _shapeSettings)
        {
            _releaseBuffers = new List<ComputeBuffer>();

            var minMaxBuffer = new ComputeBuffer(2, sizeof(int), ComputeBufferType.Structured);
            minMaxBuffer.SetData(new[] {int.MaxValue, 0});
            _releaseBuffers.Add(minMaxBuffer);

            _worleyCompute.SetFloat("_Persistence", shapeChannel.Persistence);
            _worleyCompute.SetInt("_Resolution", ShapeResolution);
            _worleyCompute.SetVector("_ChannelMask", shapeChannel.ChannelMask);
        
            _worleyCompute.SetTexture(0, "_Result", _shapeTexture);
            _worleyCompute.SetBuffer(0, "_MinMax", minMaxBuffer);
            _worleyCompute.SetTexture(1, "_Result", _shapeTexture);
            _worleyCompute.SetBuffer(1, "_MinMax", minMaxBuffer);
            GenerateWorley(shapeChannel);

            var threadGroupCount = Mathf.CeilToInt (ShapeResolution / (float) ComputeThreadGroupSize);
            _worleyCompute.Dispatch(0, threadGroupCount, threadGroupCount, threadGroupCount);
            _worleyCompute.Dispatch(1, threadGroupCount, threadGroupCount, threadGroupCount);
        
            _releaseBuffers.ForEach(t => t.Release());
        }

        _initialized = true;
    }

    private void OnDestroy()
    {
        if (_shapeTexture != null)
        {
            _shapeTexture.Release();
            _shapeTexture = null;
        }
    }

    private void GenerateWorley(WorleyNoiseSettings settings)
    {
        var prng = new System.Random(settings.Seed);
        
        UploadPoints(prng, settings.DivisionCountA, "_DivisionA");
        UploadPoints(prng, settings.DivisionCountB, "_DivisionB");
        UploadPoints(prng, settings.DivisionCountC, "_DivisionC");
        
        _worleyCompute.SetInt("_CellCountA", settings.DivisionCountA);
        _worleyCompute.SetInt("_CellCountB", settings.DivisionCountB);
        _worleyCompute.SetInt("_CellCountC", settings.DivisionCountC);
        _worleyCompute.SetBool("_Invert", settings.Invert);
        _worleyCompute.SetInt("_Tile", settings.Tile);        
    }

    private void UploadPoints(System.Random prng, int cellsPerAxis, string bufferName)
    {
        var points = new Vector3[cellsPerAxis * cellsPerAxis * cellsPerAxis];

        var cellSize = 1f / cellsPerAxis;

        for (var x = 0; x < cellsPerAxis; x++)
        {
            for (var y = 0; y < cellsPerAxis; y++)
            {
                for (var z = 0; z < cellsPerAxis; z++)
                {
                    var randomX = (float)prng.NextDouble();
                    var randomY = (float)prng.NextDouble();
                    var randomZ = (float)prng.NextDouble();

                    var randomOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
                    var cellCorner = new Vector3(x, y, z) * cellSize;

                    var index = x + cellsPerAxis * (y + z * cellsPerAxis);
                    points[index] = cellCorner + randomOffset;
                }
            }
        }
        
        UploadBuffer(points, sizeof(float) * 3, bufferName);
    }
    
    private void UploadBuffer(System.Array data, int stride, string bufferName, int kernel = 0)
    {
        var buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Structured);
        _releaseBuffers.Add(buffer);
        buffer.SetData(data);
        _worleyCompute.SetBuffer(kernel, bufferName, buffer);
    }
    
    private void Create3DNoiseTexture(ref RenderTexture renderTexture)
    {
        var targetGraphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
        if (renderTexture == null || !renderTexture.IsCreated() || renderTexture.width != ShapeResolution ||
            renderTexture.height != ShapeResolution || renderTexture.volumeDepth != ShapeResolution ||
            renderTexture.graphicsFormat != targetGraphicsFormat)
        {
            if(renderTexture != null)
                renderTexture.Release();
            
            renderTexture = new RenderTexture(ShapeResolution, ShapeResolution, 0)
            {
                volumeDepth = ShapeResolution,
                graphicsFormat = targetGraphicsFormat,
                enableRandomWrite = true,
                dimension = TextureDimension.Tex3D,
                name = "Shape Texture3D" 
            };

            renderTexture.Create();
            LoadTexture();
        }

        renderTexture.wrapMode = TextureWrapMode.Repeat;
        renderTexture.filterMode = FilterMode.Bilinear;
    }

    #if UNITY_EDITOR
    public void SaveTexture()
    {
        _saveLoader.Save(_shapeTexture, SaveName);
    }
    
    #endif

    public void LoadTexture()
    {
        _saveLoader.LoadToTarget3D(SaveName, ComputeThreadGroupSize, _shapeTexture);
    }
}
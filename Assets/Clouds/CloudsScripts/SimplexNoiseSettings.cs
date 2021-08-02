using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Create SimplexNoiseSettings", fileName = "SimplexNoiseSettings", order = 0)]
public class SimplexNoiseSettings : ScriptableObject
{
    [SerializeField] private int _seed;
    [Range(1, 6)]
    [SerializeField] private int _layerCount;
    [SerializeField] private float _scale = 1;
    [SerializeField] private float _lacunarity = 2;
    [SerializeField] private float _persistence = 0.5f;
    [SerializeField] private Vector2 _offset;

    public int Seed => _seed;
    public int LayerCount => _layerCount;

    public struct Data
    {
        public int Seed;
        public int LayerCount;
        public float Scale;
        public float Lacunarity;
        public float Persistence;
        public Vector2 Offset;
    }

    public Data GetData()
    {
        var data = new Data()
        {
            Seed = _seed,
            LayerCount = Mathf.Max(1, _layerCount),
            Scale = _scale,
            Lacunarity = _lacunarity,
            Persistence = _persistence,
            Offset = _offset
        };
        
        return data;
    }

    public int Stride => sizeof(float) * 7;
}
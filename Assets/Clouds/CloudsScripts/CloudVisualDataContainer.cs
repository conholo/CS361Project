using System.Collections.Generic;
using UnityEngine;

public class CloudVisualDataContainer : MonoBehaviour
{
    [SerializeField] private CloudStateData[] _data;

    public CloudState LastVisualized { get; private set; }
    private Dictionary<CloudState, CloudStateData> _states;
    
    private void Awake()
    {
        FillStateMapping();
    }

    public CloudStateData GetData(CloudState state)
    {
        LastVisualized = state;
        return _states[state];
    }

    public CloudStateData DebugState(CloudState state)
    {
        if (_states == null)
            FillStateMapping();

        return GetData(state);
    }

    private void FillStateMapping()
    {
        _states = new Dictionary<CloudState, CloudStateData>();
        foreach (var cloudVisual in _data)
            _states.Add(cloudVisual.CloudState, cloudVisual);
    }
}
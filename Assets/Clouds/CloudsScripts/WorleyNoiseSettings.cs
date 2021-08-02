using UnityEngine;

[CreateAssetMenu(menuName = "Create WorleyNoiseSettings", fileName = "WorleyNoiseSettings", order = 0)]
public class WorleyNoiseSettings : ScriptableObject
{
    [SerializeField] private Channel _targetChannel;
    [SerializeField] private int _seed;
    [Range (1, 50)]
    [SerializeField] private int _divisionCountA = 5;
    [Range (1, 50)]
    [SerializeField] private int _divisionCountB = 10;
    [Range (1, 50)]
    [SerializeField] private int _divisionCountC = 15;

    [SerializeField] private float _persistence = .5f;
    [SerializeField] private int _tile = 1;
    [SerializeField] private bool _invert = true;


    public Vector4 ChannelMask => new Vector4(
        _targetChannel == Channel.R ? 1 : 0, 
        _targetChannel == Channel.G ? 1 : 0,
        _targetChannel == Channel.B ? 1 : 0, 
        _targetChannel == Channel.A ? 1 : 0);

    public int Seed => _seed;
    public int DivisionCountA => _divisionCountA;
    public int DivisionCountB => _divisionCountB;
    public int DivisionCountC => _divisionCountC;

    public float Persistence => _persistence;
    public int Tile => _tile;
    public bool Invert => _invert;
}

public enum Channel { R, G, B, A }
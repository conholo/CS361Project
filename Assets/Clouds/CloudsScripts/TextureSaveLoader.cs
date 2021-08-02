
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class TextureSaveLoader : MonoBehaviour
{
    private const int SaveThreadGroupSize = 32;

    private const string ResourcesRelativePath = "Textures/";
    
    [SerializeField] private ComputeShader _saveSlicer;
    [SerializeField] private ComputeShader _copy3D;

    public void LoadToTarget3D(string saveName, int threadGroupSize, RenderTexture target)
    {
        var savedTexture = Resources.Load<Texture3D>(ResourcesRelativePath + saveName);

        if (savedTexture == null)
        {
            Debug.LogWarning($"No Texture3D has been found under the save name: {ResourcesRelativePath + saveName}.");
            return;
        }
        
        _copy3D.SetTexture(0, "_Read", savedTexture);
        _copy3D.SetTexture(0, "_Write", target);
        var numThreadGroups = Mathf.CeilToInt(savedTexture.width / (float) threadGroupSize);
        _copy3D.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);
    }

#if UNITY_EDITOR

    public void Save(RenderTexture texture3D, string saveName)
    {
        var resolution = texture3D.width;
        var slices = new Texture2D[resolution];
        
        _saveSlicer.SetTexture(0, "_Read", texture3D);

        for (var layer = 0; layer < resolution; layer++)
        {
            var slice = new RenderTexture(resolution, resolution, 0)
            {
                dimension = TextureDimension.Tex2D, enableRandomWrite = true
            };

            slice.Create();

            _saveSlicer.SetTexture(0, "_Write", slice);
            _saveSlicer.SetInt("_Layer", layer);
            var numThreadGroups = Mathf.CeilToInt(resolution / (float) SaveThreadGroupSize);
            _saveSlicer.Dispatch(0, numThreadGroups, numThreadGroups, 1);

            slices[layer] = RenderTextureTo2D(slice);
        }

        var result = Create3DTextureFrom2DArray(slices, resolution);
        AssetDatabase.CreateAsset(result, $"Assets/Resources/Textures/{saveName}.asset");
    }

    private Texture2D RenderTextureTo2D(RenderTexture slice)
    {
        var result = new Texture2D(slice.width, slice.height);
        RenderTexture.active = slice;
        result.ReadPixels(new Rect(0, 0, slice.width, slice.height), 0, 0);
        result.Apply();
        return result;
    }

    private Texture3D Create3DTextureFrom2DArray(Texture2D[] slices, int volumeResolution)
    {
        var result = new Texture3D(volumeResolution, volumeResolution, volumeResolution, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Trilinear
        };

        var outputPixels = result.GetPixels();

        for (var z = 0; z < volumeResolution; z++)
        {
            var pixelsForLayer = slices[z].GetPixels();

            for (var x = 0; x < volumeResolution; x++)
            {
                for (var y = 0; y < volumeResolution; y++)
                {
                    // Write to 3D texture index using slice index.
                    outputPixels[x + volumeResolution * (y + z * volumeResolution)] = pixelsForLayer[x + y * volumeResolution];
                }
            }
        }
        
        result.SetPixels(outputPixels);
        result.Apply();

        return result;
    }
#endif
}


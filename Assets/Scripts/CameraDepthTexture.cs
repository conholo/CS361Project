using UnityEngine;

public class CameraDepthTexture : MonoBehaviour
{
    [SerializeField] private DepthTextureMode _depthTextureMode;

    private void Awake()
    {
        SetDepthTextureMode();
    }

    private void OnValidate()
    {
        GetComponent<Camera>().depthTextureMode = _depthTextureMode;
    }

    private void SetDepthTextureMode()
    {
        GetComponent<Camera>().depthTextureMode = _depthTextureMode;
    }
}

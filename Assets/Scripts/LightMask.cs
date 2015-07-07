using UnityEngine;
using System.Collections;

public class LightMask : MonoBehaviour
{

    public Material mat;
    private Camera cam;
    private RenderTexture tempRT;

    void Start()
    {
        cam = GetComponent<Camera>();
        tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        
    }

    void OnDisable()
    {
        CleanUp();
    }

    void CleanUp()
    {
        RenderTexture.ReleaseTemporary(tempRT);
    }
}

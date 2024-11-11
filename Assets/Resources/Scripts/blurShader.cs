using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blurShader : MonoBehaviour
{
    public Material blurMat;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, blurMat);
    }
}

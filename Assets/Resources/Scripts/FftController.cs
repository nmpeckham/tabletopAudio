using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class FftController : MonoBehaviour
{
    [Range(0.0f, 2.0f)]
    public float multVal = 0.7f;

    [Range(0.0f, 2.0f)]
    public float audioExp = 0.7f;    //higher = more contrast, lower = less (quiet sounds show as louder)

    public GameObject barsParent;
    public FftBar[] pieces;
    private static Material[] fftBarMaterials;

    internal static float[] fadeTargets = new float[40];
    private static float[] fftOneFrameAgo = new float[10]; // values one frame ago
    private static float[] fftTwoFrameAgo = new float[10];    //values two frames ago

    public Image spectrumImage;
    private Texture2D spectrumTex;

    private float[,] imageData;
    enum FftTypes
    {
        bouncingBars,
        waterfall
    }

    private FftTypes fftType = FftTypes.bouncingBars;

    internal static DiscoMode discoModeController;
    internal static float discoModeMinSum = 0.45f;
    internal static float discoModeNumFreq = 3;


    internal void Init()
    {
        imageData = new float[50, 10];
        spectrumTex = spectrumImage.sprite.texture;

        pieces = barsParent.GetComponentsInChildren<FftBar>();
        fftBarMaterials = new Material[pieces.Length];

        int i = 0;
        foreach (FftBar bar in pieces)
        {
            fftBarMaterials[i] = bar.gameObject.GetComponent<Image>().material;
            i++;
        }
        discoModeController = GetComponent<DiscoMode>();

        StartCoroutine(AdjustScale());
        if (PlayerPrefs.GetInt("fftType") == 1) ChangeFftType();
    }

    IEnumerator AdjustScale()
    {

        while (true)
        {
            float totalSum = 0;
            if (fftType == FftTypes.bouncingBars)
            {
                for (int i = 0; i < pieces.Length; i++)

                {
                    Transform obj = pieces[i].transform;
                    float temp = (fadeTargets[4 * i] + fadeTargets[4 * i + 1] + fadeTargets[4 * i + 2] + fadeTargets[4 * i + 3]) / 4;
                    float newScale;

                    //if (temp > (fftOneFrameAgo[i] + fftTwoFrameAgo[i]) / 2)
                    //{
                    //    newScale = (temp * 0.8f) + ((fftOneFrameAgo[i]) * 0.2f);
                    //}
                    //else
                    //{
                    newScale = (temp * 0.55f) + (fftOneFrameAgo[i] * 0.25f) + (fftTwoFrameAgo[i] * 0.2f); //average slightly over 3 frames
                    //}


                    //newScale *= Mathf.Cos(((i) / 22f) * Mathf.PI);
                    //newScale = -(Mathf.Pow(newScale, audioExp) * Mathf.Log(newScale, 2));
                    //newScale *=  Mathf.Cos((float)newScale * 2.5f) + 1f; //math
                    newScale *= -(i / 4f) + 13f / 4f;
                    newScale = Mathf.Pow((float)newScale, audioExp) * multVal;
                    newScale = Mathf.Min(newScale, 1);
                    obj.localScale = new Vector3(1, newScale, 1);
                    if (i < discoModeNumFreq) totalSum += newScale;

                    //set var in shader to adjust texture height
                    fftBarMaterials[i].SetFloat("Height", newScale);
                    fftOneFrameAgo[i] = temp;
                    fftTwoFrameAgo[i] = fftOneFrameAgo[i];
                }

            }
            else
            {
                for (int i = 0; i < fadeTargets.Length; i++)
                {
                    float b = (fadeTargets[i] > 0.05f && fadeTargets[i] < 0.3f) ? 0.5f / fadeTargets[i] : 0f;//Mathf.Clamp((1 / fadeTargets[i] - 0.05f) - 3f, 0f, 1f) : 0f;
                    float g = fadeTargets[i] > 0.2f ? (fadeTargets[i] - 0.2f) * 1.5f : 0f;
                    float r = fadeTargets[i] * 2f;
                    float a = (r + g + b);

                    spectrumTex.SetPixel(spectrumTex.width - 1, i, new Color(r, g, b, a));
                }
                for (int j = 0; j < spectrumTex.width; j++)
                {
                    for (int k = 0; k < spectrumTex.height; k++)
                    {
                        spectrumTex.SetPixel(j, k, spectrumTex.GetPixel(j + 1, k));
                    }
                }
                spectrumTex.Apply();
            }
            float sum = 0;// fadeTargets[0] + fadeTargets[1] + fadeTargets[2] + fadeTargets[3];
            for(int i = 0; i < discoModeNumFreq; i++)
            {
                sum += fadeTargets[i];
            }
            if (sum * 20 >= discoModeMinSum)
            {
                discoModeController.ChangeColors();
            }
            yield return null;
        }
    }

    internal void ChangeFftType()
    {
        int idx = (int)fftType + 1;
        fftType = (FftTypes)(idx % Enum.GetValues(typeof(FftTypes)).Length);

        if (fftType == FftTypes.bouncingBars)
        {
            barsParent.SetActive(true);
            spectrumImage.gameObject.SetActive(false);
        }
        else if (fftType == FftTypes.waterfall)
        {
            for (int i = 0; i < spectrumTex.width; i++)
            {
                for (int j = 0; j < spectrumTex.height; j++)
                {
                    spectrumTex.SetPixel(i, j, Color.clear);
                }
            }
            spectrumTex.Apply();
            barsParent.SetActive(false);
            spectrumImage.gameObject.SetActive(true);
        }
        PlayerPrefs.SetInt("fftType", (int)fftType);
    }
}

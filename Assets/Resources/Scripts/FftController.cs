using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Extensions;

public class FftController : MonoBehaviour
{

    public TMP_Text freqText;
    public TMP_Text ampText;

    public Slider freqSlider;
    public Slider ampSlider;
    [Range(0.0f, 6f)]
    public float freqVal = 3f;

    [Range(0.0f, 6f)]
    public float ampVal = 4f;    //higher = more contrast, lower = less (quiet sounds show as louder)

    public GameObject barsParent;
    public FftBar[] pieces;
    private static Material[] fftBarMaterials;

    internal static float[] fadeTargets = new float[40];
    private static float[] fftOneFrameAgo = new float[10]; // values one frame ago
    private static float[] fftTwoFrameAgo = new float[10];    //values two frames ago

    public Image spectrumImage;
    private Texture2D spectrumTex;

    public Transform discoModeSum;
    public TMP_Text discoModeSumSliderText;

    private MainAppController mac;
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
        mac = GetComponent<MainAppController>();
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

        freqSlider.onValueChanged.AddListener(FrequencyValChanged);
        ampSlider.onValueChanged.AddListener(AmpValChanged);
    }



    void FrequencyValChanged(float val)
    {
        freqText.text = val.ToString("N1");
        freqVal = val;
    }

    void AmpValChanged(float val)
    {
        ampText.text = val.ToString("N1");
        ampVal = val;
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

                    //sum bands into bins of 4
                    float temp = (fadeTargets[4 * i] + fadeTargets[4 * i + 1] + fadeTargets[4 * i + 2] + fadeTargets[4 * i + 3]) / 4;
                    temp *= (-(i - 11f) / freqVal) + 1f; //frequency correction
                    temp = (Mathf.Sqrt(temp / ampVal)); // intensity correction


                    temp = Mathf.Min(temp, 1);
                    if (temp < fftOneFrameAgo[i])
                    {
                        temp = (temp * 0.6f) + (fftOneFrameAgo[i] * 0.25f) + (fftTwoFrameAgo[i] * 0.15f);
                    }

                    obj.localScale = new Vector3(1, temp, 1);
                    if (i < discoModeNumFreq) totalSum += temp;

                    fftBarMaterials[i].SetFloat("Height", temp);
                    fftOneFrameAgo[i] = temp;
                    fftTwoFrameAgo[i] = fftOneFrameAgo[i];
                }
            }
            else
            {
                if(!MusicController.isPaused)
                {
                    for (int i = 0; i < fadeTargets.Length; i++)
                    {
                        float b = (fadeTargets[i] > 0.05f) ? 0.5f / fadeTargets[i] : 0f;
                        float g = fadeTargets[i] > 0.2f ? (fadeTargets[i] - 0.2f) * 1.5f : 0f;
                        float r = fadeTargets[i] * 2f;
                        float a = (r + g + b);

                        spectrumTex.SetPixel(spectrumTex.width - 1, i, new Color(r, g, b, a));
                    }
                    //shift all pixels left by one
                    for (int j = 0; j < spectrumTex.width; j++)
                    {
                        for (int k = 0; k < spectrumTex.height; k++)
                        {
                            spectrumTex.SetPixel(j, k, spectrumTex.GetPixel(j + 1, k));
                        }
                    }
                    spectrumTex.Apply();
                }
            }
            float sum = 0;

            for(int i = 0; i < discoModeNumFreq; i++)
            {
                sum += fadeTargets[i];
            }
            sum *= 20f;
            if (mac.currentMenuState == MainAppController.MenuState.advancedOptionsMenu)
            {
                discoModeSum.localScale = new Vector3(Mathf.Min(sum / discoModeMinSum, 1f), 1, 1);
                discoModeSumSliderText.text = (sum / discoModeMinSum).ToString("F4");
            }
            if (sum >= discoModeMinSum)
            {
                discoModeController.ChangeColors();
            }
            yield return new WaitForFixedUpdate();
        }
    }

    //float GetRMS(int i)
    //{
    //    return Mathf.Sqrt(Mathf.Pow(fftOneFrameAgo[i]), 2) + Mathf.Pow(fftTwoFrameAgo[i], 2) + Mathf.Pow()
    //}

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

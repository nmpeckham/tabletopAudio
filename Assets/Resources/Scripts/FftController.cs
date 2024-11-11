using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FftController : MonoBehaviour
{

    public TMP_Text freqText;
    public TMP_Text ampText;

    [Range(0.0f, 6f)]
    public float freqVal = 3f;

    [Range(0.0f, 6f)]
    public float ampVal = 4f;    //higher = more contrast, lower = less (quiet sounds show as louder)

    public GameObject barsParent;
    public FftBar[] pieces;
    private static Material[] fftBarMaterials;

    internal const int binSize = 4;
    internal static float[] fadeTargets;
    private static float[] fftOneFrameAgo;   // values one frame ago
    private static float[] fftTwoFrameAgo;   //values two frames ago
    private static float[] fftThreeFrameAgo; //values three frames ago
    private static float[] fftFourFrameAgo;  //values four frames ago


    public Image spectrumImage;
    private Texture2D spectrumTex;

    public Transform discoModeSum;
    public TMP_Text discoModeSumSliderText;

    private static MainAppController mac;

    private enum FftTypes
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

        fftOneFrameAgo = new float[pieces.Length];
        fftTwoFrameAgo = new float[pieces.Length];
        fftThreeFrameAgo = new float[pieces.Length];
        fftFourFrameAgo = new float[pieces.Length];
        fadeTargets = new float[pieces.Length * binSize];

        StartCoroutine(AdjustScale());
        if (PlayerPrefs.GetInt("fftType") == 1)
        {
            ChangeFftType();
        }
    }

    private IEnumerator AdjustScale()
    {
        float totalSum = 0;
        float currentFFT;

        float r;
        float g;
        float b;
        float a;
        float sum;

        while (true)
        {
            if (mac.currentFPS < 30)
            {
                yield return null;
            }

            if (fftType == FftTypes.bouncingBars)
            {
                for (int i = 0; i < pieces.Length; i++)

                {
                    Transform obj = pieces[i].transform;

                    //sum bands into bins of 4
                    currentFFT = (fadeTargets[4 * i] + fadeTargets[4 * i + 1] + fadeTargets[4 * i + 2] + fadeTargets[4 * i + 3]) / 4;
                    currentFFT *=  ((-i + 11f) / freqVal) + 1f; //frequency correction
                    currentFFT = (Mathf.Sqrt(currentFFT / ampVal)); // intensity correction


                    
                    //Strong average if intensity is descending
                    if (currentFFT < fftOneFrameAgo[i])
                    {
                        currentFFT = (currentFFT * 0.3f) + (fftOneFrameAgo[i] * 0.25f) + (fftTwoFrameAgo[i] * 0.2f) + (fftThreeFrameAgo[i] * 0.15f) + (fftFourFrameAgo[i] * 0.1f); // average over 4 previous frames
                    }
                    //Weak average if ascending
                    else
                    {
                        currentFFT = (currentFFT * 0.8f) + (fftOneFrameAgo[i] * 0.2f); // average over 2 frames

                    }
                    currentFFT = currentFFT < 0.02f ? 0f : currentFFT;
                    currentFFT = Mathf.Min(currentFFT, 1);  //prevent scale > 1
                    obj.localScale = new Vector3(1, currentFFT, 1);
                    if (i < discoModeNumFreq)
                    {
                        totalSum += currentFFT;
                    }

                    fftBarMaterials[i].SetFloat("Height", currentFFT);
                    fftOneFrameAgo  [i] = currentFFT;
                    fftTwoFrameAgo  [i] = fftOneFrameAgo  [i];
                    fftThreeFrameAgo[i] = fftTwoFrameAgo  [i];
                    fftFourFrameAgo [i] = fftThreeFrameAgo[i];
                }
            }
            else
            {

                if (!MusicController.isPaused)
                {
                    for (int j = 0; j < spectrumTex.width; j++)
                    {
                        for (int k = 0; k < spectrumTex.height; k++)
                        {
                            spectrumTex.SetPixel(j, k, spectrumTex.GetPixel(j + 1, k));
                        }
                    }
                    for (int i = 0; i < fadeTargets.Length; i++)
                    {
                        float temp = fadeTargets[i] * ((-i / 40f) + 2f); //frequency correction
                        b = (temp > 0.05f) ? 0.5f / temp : 0f;
                        g = temp > 0.2f ? (temp - 0.2f) * 1.5f : 0f;
                        r = temp * 2f;
                        a = (r + g + b);

                        spectrumTex.SetPixel(spectrumTex.width - 1, i, new Color(r, g, b, a));
                    }
                    //shift all pixels left by one

                    spectrumTex.Apply();
                }
            }
            sum = 0;

            for (int i = 0; i < discoModeNumFreq; i++)
            {
                sum += fadeTargets[i];
            }
            sum *= 20f;
            if (discoModeController.discoModeActive)
            {
                if (sum >= discoModeMinSum)
                {
                    discoModeController.ChangeColors();
                }
            }

            if (mac.currentMenuState == MainAppController.MenuState.advancedOptionsMenu)
            {
                discoModeSum.localScale = new Vector3(Mathf.Min(sum / discoModeMinSum, 1f), 1, 1);
                discoModeSumSliderText.text = (sum / discoModeMinSum).ToString("F4");
            }

            yield return new WaitForFixedUpdate();
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

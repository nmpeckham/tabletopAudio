using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MusicLoadingAnimation : MonoBehaviour
{
    private static Sprite[] spinnerImages;
    private Image spinnerDisplay;
    // Start is called before the first frame update
    public void OnEnable()
    {
        spinnerDisplay = GetComponent<Image>();
        if (spinnerImages == null)
        {
            spinnerImages = Resources.LoadAll<Sprite>("Spinner");
        }

        StartCoroutine(AnimateSpinner());
    }

    private IEnumerator AnimateSpinner()
    {
        while (true)
        {
            foreach (Sprite frame in spinnerImages)
            {
                //print(frame.name);
                spinnerDisplay.sprite = frame;
                yield return null;
            }
        }
    }
}

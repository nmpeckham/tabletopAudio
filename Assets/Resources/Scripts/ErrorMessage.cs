using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Class for error message objects
public class ErrorMessage : MonoBehaviour
{

    public GameObject Spinner;
    public TMP_Text thisText;
    private Image thisImage;
    public Image typeImage;
    public int delayTime = 8;
    // Start is called before the first frame update
    internal void Init()
    {
        thisImage = GetComponent<Image>();
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut(bool waitForDelayTime = true)
    {
        yield return null;
        if(waitForDelayTime) yield return new WaitForSecondsRealtime(delayTime);
        while (thisText.color.a > 0)
        {
            thisText.color = new Color(thisText.color.r, thisText.color.g, thisText.color.b, thisText.color.a - 0.01f); ;
            thisImage.color = new Color(thisImage.color.r, thisImage.color.g, thisImage.color.b, thisImage.color.a - 0.01f);
            typeImage.color = new Color(typeImage.color.r, typeImage.color.g, typeImage.color.b, typeImage.color.a - 0.01f);
            yield return null;
        }
        Destroy(gameObject);
    }

    internal void StartFadeOutNow()
    {
        StartCoroutine(FadeOut(false));
    }
}

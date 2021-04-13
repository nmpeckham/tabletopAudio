using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

//Class for error message objects
public class ErrorMessage : MonoBehaviour
{
    TMP_Text thisText;
    Image thisImage;
    internal int delayTime = 8;
    // Start is called before the first frame update
    void Start()
    {
        thisText = GetComponentInChildren<TMP_Text>();
        thisImage = GetComponent<Image>();
        StartCoroutine("FadeOut");
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSecondsRealtime(delayTime);
        while (thisText.color.a > 0)
        {
            thisText.color = new Color(thisText.color.r, thisText.color.g, thisText.color.b, thisText.color.a - 0.01f); ;
            thisImage.color = new Color(thisImage.color.r, thisImage.color.g, thisImage.color.b, thisImage.color.a - 0.01f); ;
            yield return null;
        }
        Destroy(gameObject);
    }
}

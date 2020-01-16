using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//Class for error message objects
public class ErrorMessage : MonoBehaviour
{
    Color textColor;
    TMP_Text thisText;
    // Start is called before the first frame update
    void Start()
    {
        textColor = Color.red;
        thisText = GetComponent<TMP_Text>();
        StartCoroutine("FadeOut");
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSecondsRealtime(8);
        while(thisText.color.a > 0)
        {
            thisText.color = new Color(thisText.color.r, thisText.color.g, thisText.color.b, thisText.color.a - 0.01f);
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
    }
}

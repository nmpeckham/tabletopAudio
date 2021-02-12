using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollNowPlayingTitle : MonoBehaviour
{
    public ScrollRect scrollArea;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ScrollView());
    }

    IEnumerator ScrollView()
    {
        while(true)
        {
            yield return new WaitForSecondsRealtime(3);
            float amt = Mathf.Abs(0.8f / (scrollArea.content.rect.width - 410));// width of container (410px) must be subtracted for smooth scrolling
            int i = 0;
            while(scrollArea.horizontalNormalizedPosition < 1)
            {
                i++;
                scrollArea.horizontalNormalizedPosition += amt;
                //print(scrollArea.horizontalNormalizedPosition);
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSecondsRealtime(2);
            scrollArea.horizontalNormalizedPosition = 0;
        }
    }

    internal void SongChanged()
    {
        StopAllCoroutines();
        scrollArea.horizontalNormalizedPosition = 0;
        StartCoroutine(ScrollView());
    }
}

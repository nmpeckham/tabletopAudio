using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollNowPlayingTitle : MonoBehaviour
{
    public ScrollRect scrollArea;
    // Start is called before the first frame update
    private readonly float rate = 1f;

    private void Start()
    {
        StartCoroutine(ScrollView());
    }

    private IEnumerator ScrollView()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(3);
            float amt = Mathf.Abs(rate / (scrollArea.content.rect.width - 390));// width of container (390px) must be subtracted for smooth scrolling
            while (scrollArea.horizontalNormalizedPosition < 0.99f)
            {
                scrollArea.horizontalNormalizedPosition += amt;
                yield return null;
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

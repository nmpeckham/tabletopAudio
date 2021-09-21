using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicLoadingAnimation : MonoBehaviour
{
    Sprite[] spinnerImages;
    PlaylistTabs pt;
    public Image spinnerDisplay;
    // Start is called before the first frame update
    public void Init()
    {
        pt = GetComponent<PlaylistTabs>();
        spinnerDisplay.gameObject.SetActive(true);
        spinnerImages = Resources.LoadAll<Sprite>("Spinner");
        StartCoroutine(AnimateSpinner());
    }

    IEnumerator AnimateSpinner()
    {
        while(pt.mainTab.MusicButtons.Count == 0)
        {
            foreach(Sprite frame in spinnerImages)
            {
                if (pt.mainTab.MusicButtons.Count != 0) break;
                spinnerDisplay.sprite = frame;
                yield return new WaitForSecondsRealtime(0.01f);
            }
        }
        spinnerDisplay.gameObject.SetActive(false);
        yield return null;
    }
}

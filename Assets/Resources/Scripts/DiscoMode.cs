using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiscoMode : MonoBehaviour
{
    bool discoModeActive = false;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    internal void ToggleDiscoMode()
    {
        discoModeActive = !discoModeActive;
        StartCoroutine(ToggleDiscoModes());
    }
    IEnumerator ToggleDiscoModes()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("sfxButton"))
        {
            go.GetComponent<SFXButton>().SetDiscoMode(discoModeActive);
            int rand = Random.Range(0, 5);
            for(int i = 0; i < rand; i++)
            {
                if(discoModeActive) yield return new WaitForEndOfFrame();
            }
        }
    }
}

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
        StartCoroutine(StartDiscoModes());
    }
    IEnumerator StartDiscoModes()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("sfxButton"))
        {
            go.GetComponent<SFXButton>().ToggleDiscoMode();
            int rand = Random.Range(2, 10);
            for(int i = 0; i < rand; i++)
            {
                yield return new WaitForEndOfFrame();
            }
        }
    }
}

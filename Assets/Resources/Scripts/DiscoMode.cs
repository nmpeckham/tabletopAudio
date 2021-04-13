using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiscoMode : MonoBehaviour
{
    internal bool discoModeActive = false;
    float currentCooldown = 0;
    internal float cooldown = 15;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        currentCooldown--;
    }

    internal void SetDiscoMode(bool val)
    {
        discoModeActive = val;
        StartCoroutine(ToggleDiscoModes());
    }
    internal void ChangeColors()
    {
        if (currentCooldown <= 0 && discoModeActive)
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("sfxButton"))
            {
                float rand = Random.Range(0f, 1f);
                if(rand > 0.5f) go.GetComponent<SFXButton>().ChangeColor();
            }
            currentCooldown = cooldown;
        }
    }
    IEnumerator ToggleDiscoModes()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("sfxButton"))
        {
            go.GetComponent<SFXButton>().SetDiscoMode(discoModeActive);
            if (discoModeActive)
            {
                int rand = Random.Range(0, 5);
                for (int i = 0; i < rand; i++)
                {
                    yield return null;
                }
            }
        }
    }
}

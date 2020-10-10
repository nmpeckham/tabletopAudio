using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DarkModeController : MonoBehaviour
{
    private MusicController mc;
    private MainAppController mac;
    internal void SwapDarkMode(bool enable)
    {
        mc = GetComponent<MusicController>();
        mac = GetComponent<MainAppController>();
        if (enable)
        {
            GameObject[] imgToChange = GameObject.FindGameObjectsWithTag("imageChangeOnDarkMode");
            foreach (GameObject obj in imgToChange)
            {
                obj.GetComponent<Image>().color = ResourceManager.lightModeGrey;
            }

            GameObject[] textToChange = GameObject.FindGameObjectsWithTag("textChangeOnDarkMode");
            foreach (GameObject obj in textToChange)
            {
                obj.GetComponent<TMP_Text>().color = ResourceManager.lightModeGrey;
            }

            GameObject[] sfxTextToChange = GameObject.FindGameObjectsWithTag("sfxText");
            foreach (GameObject obj in sfxTextToChange)
            {
                obj.GetComponent<TMP_Text>().color = Color.white;
            }

            GameObject[] sfxButtonsToChange = GameObject.FindGameObjectsWithTag("sfxButton");
            foreach (GameObject obj in sfxButtonsToChange)
            {
                obj.GetComponent<Image>().color = ResourceManager.sfxButtonDark;
                try
                {
                    if (obj.GetComponent<SFXButton>().isPlaying)
                    {
                        obj.GetComponent<Image>().color = Color.green;
                    }
                }
                catch (NullReferenceException) { }
            }

            GameObject.FindGameObjectWithTag("mainBackground").GetComponent<Image>().color = ResourceManager.lightModeGrey;

            GameObject[] sfxPageBGs = GameObject.FindGameObjectsWithTag("sfxButtonBG");
            foreach (GameObject obj in sfxPageBGs)
            {
                obj.GetComponent<Image>().color = ResourceManager.darkModeGrey;
            }

            GameObject[] pbTextToChange = GameObject.FindGameObjectsWithTag("pageButtonText");
            foreach (GameObject obj in pbTextToChange)
            {
                obj.GetComponent<TMP_Text>().color = Color.white;
            }

            GameObject[] bgToChange = GameObject.FindGameObjectsWithTag("changesOnDarkMode");
            foreach (GameObject obj in bgToChange)
            {
                Image buttonImg = obj.GetComponent<Image>();
                buttonImg.color = ResourceManager.darkModeGrey;
                try
                {
                    string buttonId = obj.GetComponent<AudioControlButton>().id;
                    if (buttonId == "CROSSFADE" && mc.Crossfade)
                    {
                        buttonImg.color = Color.green;
                    }
                    if (buttonId == "SHUFFLE" && mc.Shuffle)
                    {
                        buttonImg.color = Color.green;
                    }
                }
                catch (NullReferenceException) { }
                try
                {
                    int pageId = obj.GetComponent<PageButton>().id;
                    if (pageId == mac.activePage)
                    {
                        buttonImg.color = Color.red;
                    }
                }
                catch (NullReferenceException) {
                }
            }
            GameObject[] fftBars = GameObject.FindGameObjectsWithTag("fftBar");
            foreach(GameObject bar in fftBars)
            {
                bar.GetComponent<Image>().color = Color.white;
            }
        }
        else
        {
           

            GameObject[] imgToChange = GameObject.FindGameObjectsWithTag("imageChangeOnDarkMode");
            foreach (GameObject obj in imgToChange)
            {
                obj.GetComponent<Image>().color = ResourceManager.darkModeGrey;
            }

            GameObject[] textToChange = GameObject.FindGameObjectsWithTag("textChangeOnDarkMode");
            foreach (GameObject obj in textToChange)
            {
                obj.GetComponent<TMP_Text>().color = Color.black;
            }

            GameObject[] sfxTextToChange = GameObject.FindGameObjectsWithTag("sfxText");
            foreach (GameObject obj in sfxTextToChange)
            {
                obj.GetComponent<TMP_Text>().color = Color.black;
            }

            GameObject[] sfxButtonsToChange = GameObject.FindGameObjectsWithTag("sfxButton");
            foreach (GameObject obj in sfxButtonsToChange)
            {
                obj.GetComponent<Image>().color = ResourceManager.sfxButtonLight;
                try
                {
                    if (obj.GetComponent<SFXButton>().isPlaying)
                    {
                        obj.GetComponent<Image>().color = Color.green;
                    }
                }
                catch (NullReferenceException) { }
            }

            GameObject.FindGameObjectWithTag("mainBackground").GetComponent<Image>().color = Color.white;

            GameObject[] sfxPageBGs = GameObject.FindGameObjectsWithTag("sfxButtonBG");
            foreach (GameObject obj in sfxPageBGs)
            {
                obj.GetComponent<Image>().color = ResourceManager.sfxPageBG;
            }


            GameObject[] pbTextToChange = GameObject.FindGameObjectsWithTag("pageButtonText");
            foreach (GameObject obj in pbTextToChange)
            {
                obj.GetComponent<TMP_Text>().color = Color.black;
            }

            GameObject[] bgToChange = GameObject.FindGameObjectsWithTag("changesOnDarkMode");
            foreach (GameObject obj in bgToChange)
            {
                Image buttonImg = obj.GetComponent<Image>();
                buttonImg.color = ResourceManager.lightModeGrey;
                try
                {
                    string buttonId = obj.GetComponent<AudioControlButton>().id;
                    if (buttonId == "CROSSFADE" && mc.Crossfade)
                    {
                        buttonImg.color = Color.green;
                    }
                    if (buttonId == "SHUFFLE" && mc.Shuffle)
                    {
                        buttonImg.color = Color.green;
                    }
                }
                catch (NullReferenceException) { }
                try
                {
                    int pageId = obj.GetComponent<PageButton>().id;
                    if (pageId == mac.activePage)
                    {
                        buttonImg.color = Color.red;
                    }
                }
                catch (NullReferenceException) { }

            }
            GameObject[] fftBars = GameObject.FindGameObjectsWithTag("fftBar");
            foreach (GameObject bar in fftBars)
            {
                bar.GetComponent<Image>().color = Color.black;
            }
        }
    }

}

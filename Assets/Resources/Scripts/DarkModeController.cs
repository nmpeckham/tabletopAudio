﻿using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DarkModeController : MonoBehaviour
{
    private MusicController mc;
    public Material crossfadeMaterial;
    private SFXPageController spc;
    internal void SwapDarkMode(bool enable)
    {
        mc = GetComponent<MusicController>();
        spc = GetComponent<SFXPageController>();
        
        //Light to Dark
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
                obj.GetComponent<TMP_Text>().material.DisableKeyword("UNDERLAY_ON");
            }

            GameObject[] sfxTextToChange = GameObject.FindGameObjectsWithTag("sfxText");
            foreach (GameObject obj in sfxTextToChange)
            {
                obj.GetComponent<TMP_Text>().color = Color.white;
            }

            spc.pageParents.ForEach(pp =>
            {
                pp.buttons.ForEach(btn =>
                {
                    btn.GetComponent<SFXButton>().ChangeButtonColor(ResourceManager.sfxButtonDark);
                    try
                    {
                        if (btn.GetComponent<SFXButton>().IsPlaying)
                        {
                            btn.GetComponent<Image>().color = Color.green;
                        }
                    }
                    catch (NullReferenceException) { }
                });
            });

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
                    if (buttonId == "PAUSE" && MusicController.isPaused)
                    {
                        buttonImg.color = ResourceManager.orange;
                    }
                }
                catch (NullReferenceException) { }
                try
                {
                    int pageId = obj.GetComponent<PageButton>().id;
                    if (pageId == SFXPageController.activePage)
                    {
                        buttonImg.color = Color.red;
                    }
                }
                catch (NullReferenceException) { }
                crossfadeMaterial.SetColor("ButtonColor", ResourceManager.darkModeGrey);
            }
            foreach(GameObject go in GameObject.FindGameObjectsWithTag("sfxSubcontrolButton"))
            {
                go.GetComponent<Image>().color = Color.white;
            }
        }

        //Dark to Light
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
                obj.GetComponent<TMP_Text>().material.EnableKeyword("UNDERLAY_ON");
            }

            GameObject[] sfxTextToChange = GameObject.FindGameObjectsWithTag("sfxText");
            foreach (GameObject obj in sfxTextToChange)
            {
                obj.GetComponent<TMP_Text>().color = Color.black;

            }

            spc.pageParents.ForEach(pp =>
            {
                pp.buttons.ForEach(btn =>
                {
                    btn.GetComponent<SFXButton>().ChangeButtonColor(ResourceManager.sfxButtonLight); ;
                    try
                    {
                        if (btn.GetComponent<SFXButton>().IsPlaying)
                        {
                            btn.GetComponent<Image>().color = Color.green;
                        }
                    }
                    catch (NullReferenceException) { }
                });
            });

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
                buttonImg.color = ResourceManager.sfxButtonLight;
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
                    if (buttonId == "PAUSE" && MusicController.isPaused)
                    {
                        buttonImg.color = ResourceManager.orange;
                    }
                }
                catch (NullReferenceException) { } 
                try
                {
                    int pageId = obj.GetComponent<PageButton>().id;
                    if (pageId == SFXPageController.activePage)
                    {
                        buttonImg.color = Color.red;
                    }
                }
                catch (NullReferenceException) { }
                crossfadeMaterial.SetColor("ButtonColor", ResourceManager.lightModeGrey);
            }
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("sfxSubcontrolButton"))
            {
                go.GetComponent<Image>().color = Color.black;
            }
        }
    }
}

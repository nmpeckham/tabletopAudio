using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using TMPro;


//Main controller for the app. Handles various tasks
public class MainAppController : MonoBehaviour
{
    internal const int NUMPAGES = 7;
    internal const int NUMBUTTONS = 35;

    internal string mainDirectory;
    internal string musicDirectory;
    internal string sfxDirectory;
    internal string saveDirectory;
    internal char sep;

    internal int activePage = 0;

    public PageParent[] pageParents;
    public GameObject sfxButtonPrefab;
    public List<List<GameObject>> SFXButtons;
    private MusicController mc;
    private EditPageLabel epl;

    public GameObject optionsPanel;

    public GameObject errorMessagesPanel;
    public GameObject errorPrefab;

    private Color stopColor;
    // Start is called before the first frame update
    public void Start()
    {
        //Screen.fullScreen = false;
        //Screen.SetResolution(800, 500, false);
        epl = GetComponent<EditPageLabel>();
        SFXButtons = new List<List<GameObject>>();
        pageParents = GameObject.FindObjectsOfType<PageParent>();
        mc = GetComponent<MusicController>();

        sep = System.IO.Path.DirectorySeparatorChar;
        mainDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic) + sep + "TableTopAudio";
        musicDirectory = mainDirectory + sep + "music";
        sfxDirectory = mainDirectory + sep + "sound effects";
        saveDirectory = mainDirectory + sep + "saves";

        if (!System.IO.Directory.Exists(mainDirectory))
        {
            System.IO.Directory.CreateDirectory(mainDirectory);
        }
        if (!System.IO.Directory.Exists(musicDirectory))
        {
            System.IO.Directory.CreateDirectory(musicDirectory);
        }
        if (!System.IO.Directory.Exists(sfxDirectory))
        {
            System.IO.Directory.CreateDirectory(sfxDirectory);
        }
        if (!System.IO.Directory.Exists(saveDirectory))
        {
            System.IO.Directory.CreateDirectory(saveDirectory);
        }

        MakeSFXButtons();


        pageParents[0].gameObject.transform.SetSiblingIndex(NUMPAGES);

        ResourceManager.pauseImage = Resources.Load<Sprite>("pause");
        ResourceManager.stopImage = Resources.Load<Sprite>("stop");
        ResourceManager.playImage = Resources.Load<Sprite>("play");
    }

    internal bool MakeSFXButtons()
    {
        SFXButtons.Clear();
        for (int i = 0; i < NUMPAGES; i++)
        {
            //pageParents[i].gameObject.SetActive(false);
            SFXButtons.Add(new List<GameObject>());
            for (int j = 0; j < NUMBUTTONS; j++)
            {
                GameObject button = Instantiate(sfxButtonPrefab, pageParents[i].transform);
                SFXButton btn = button.GetComponent<SFXButton>();
                btn.id = j;
                btn.page = i;
                //btn.Label = i.ToString() + ", " + j.ToString();
                //Debug.Log(SFXButtons.Count);
                //Debug.Log(i);
                SFXButtons[i].Add(button);
            }
        }
        return true;
    }

    internal bool ControlButtonClicked(string id)
    {
        switch (id)
        {
            case "STOP-SFX":
                foreach (List<GameObject> page in SFXButtons)
                {
                    foreach (GameObject obj in page)
                    {
                        obj.GetComponent<SFXButton>().Stop();
                    }
                }
                break;                
            case "OPTIONS":
                optionsPanel.SetActive(true);
                break;
            case "STOP-MUSIC":
                mc.Stop();
                break;
            case "PAUSE-MUSIC":
                mc.Pause();
                break;
            case "PLAY-MUSIC":
                mc.Play();
                break;
            case "SHUFFLE":
                mc.Shuffle = !mc.Shuffle;
                break;
            case "NEXT":
                mc.Next();
                break;
            case "PREVIOUS":
                mc.Previous();
                break;
        }
        return true;
    }

    internal void ChangeSFXPage(int pageID)
    {
        pageParents[pageID].gameObject.transform.SetSiblingIndex(NUMPAGES);
        activePage = pageID;
    }

    internal void EditPageLabel(TMP_Text label)
    {
        epl.buttonLabel = label;
        epl.StartEditing();
    }

    internal void ShowErrorMessage(string message)
    {
        GameObject error = Instantiate(errorPrefab, errorMessagesPanel.transform);
        error.GetComponentInChildren<TMP_Text>().text = "Error: " + message;
    }

}

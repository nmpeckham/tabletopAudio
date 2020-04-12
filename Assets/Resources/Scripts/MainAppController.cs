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
    internal const int NUMPAGES = 8;
    internal const int NUMBUTTONS = 35;
    internal const string VERSION = "v0.9"; //save version

    internal string mainDirectory;
    internal string musicDirectory;
    internal string sfxDirectory;
    internal string saveDirectory;
    internal char sep;

    internal int activePage = 0;

    public GameObject pageParentParent; // parent for page parents
    public GameObject pageParentPrefab;
    internal List<PageParent> pageParents;

    public GameObject sfxButtonPrefab;
    internal List<List<GameObject>> sfxButtons;

    public GameObject pageButtonPrefab;
    internal List<GameObject> pageButtons;
    public GameObject pageButtonParent;

    private MusicController mc;
    private EditPageLabel epl;

    public GameObject optionsPanel;

    public GameObject errorMessagesPanel;
    public GameObject errorPrefab;

    // Start is called before the first frame update
    public void Start()
    {
        pageParents = new List<PageParent>();
        pageButtons = new List<GameObject>();
        sfxButtons = new List<List<GameObject>>();

        epl = GetComponent<EditPageLabel>();
        mc = GetComponent<MusicController>();

        sep = System.IO.Path.DirectorySeparatorChar;

        MakeSFXButtons();


        pageParents[0].gameObject.transform.SetSiblingIndex(NUMPAGES);

        ResourceManager.pauseImage = Resources.Load<Sprite>("pause");
        ResourceManager.stopImage = Resources.Load<Sprite>("stop");
        ResourceManager.playImage = Resources.Load<Sprite>("play");
        mainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "TableTopAudio");
        musicDirectory = Path.Combine(mainDirectory, "music");
        sfxDirectory = Path.Combine(mainDirectory, "sound effects");
        saveDirectory = Path.Combine(mainDirectory, "saves");

        SetupFolderStructure(mainDirectory);
    }

    internal void SetupFolderStructure(string directory)
    {
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
        mc.AutoCheckForNewFiles = true;
    }

    internal bool MakeSFXButtons()
    {
        foreach(GameObject g in pageButtons)
        {
            Destroy(g);
        }

        foreach(PageParent o in pageParents)
        {
            Destroy(o.gameObject);
        }

        pageButtons.Clear();

        sfxButtons.Clear();
        pageParents = new List<PageParent>();
        for (int i = 0; i < NUMPAGES; i++)
        {
            GameObject go = Instantiate(pageButtonPrefab, pageButtonParent.transform);
            go.GetComponentInChildren<TMP_Text>().text = (i + 1).ToString() ;
            go.GetComponent<PageButton>().id = i;
            pageButtons.Add(go);
            go.transform.SetSiblingIndex(i + 1);
            if (i == 0) go.GetComponent<Image>().color = Color.red;

            GameObject pp = Instantiate(pageParentPrefab, pageParentParent.transform);
            pageParents.Add(pp.GetComponent<PageParent>());
            sfxButtons.Add(new List<GameObject>());

            for (int j = 0; j < NUMBUTTONS; j++)
            {
                GameObject button = Instantiate(sfxButtonPrefab, pageParents[i].transform);
                SFXButton btn = button.GetComponent<SFXButton>();
                btn.id = j;
                btn.page = i;
                sfxButtons[i].Add(button);
            }
        }
        return true;
    }

    internal bool ControlButtonClicked(string id)
    {
        switch (id)
        {
            case "STOP-SFX":
                foreach (List<GameObject> page in sfxButtons)
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
        Debug.LogError(message);
    }
}

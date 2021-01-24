using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System.Text.Json;
using UnityEngine.Video;


//Main controller for the app. Handles various tasks
public class MainAppController : MonoBehaviour
{
    internal const int NUMPAGES = 8;
    internal const int NUMBUTTONS = 35;
    internal string VERSION;  //save version

    internal string mainDirectory;
    internal string musicDirectory;
    internal string sfxDirectory;
    internal string saveDirectory;
    internal string videoDirectory;
    internal char sep;

    internal int activePage = 0;

    public GameObject pageParentParent; // parent for page parents
    public GameObject pageParentPrefab;
    internal List<PageParent> pageParents;

    public GameObject sfxButtonPrefab;
    internal List<List<GameObject>> sfxButtons;

    public GameObject pageButtonPrefab;
    public List<GameObject> pageButtons;
    public GameObject pageButtonParent;

    private MusicController mc;
    private EditPageLabel epl;
    private DarkModeController dmc;
    private DiscoMode dm;

    public GameObject optionsPanel;

    public GameObject errorMessagesPanel;
    public GameObject errorPrefab;

    public GameObject pause;
    public GameObject play;
    public GameObject stop;

    internal Sprite pauseImage;
    internal Sprite playImage;
    internal Sprite stopImage;

    internal bool darkModeEnabled = false;
    private QuickReferenceController qrc;
    private QuickRefDetailView qrd;

    private GenerateMusicFFTBackgrounds gmfb;

    private VideoPlayer player;

    private OptionsMenuController omc;
    internal bool discoModeAvailable = false;
    internal enum MenuState
    {
        mainAppView,
        editingSFXButton,
        editingPageLabel,
        optionsMenu,
        selectFileToLoad,
        enterSaveFileName,
        selectSFXFile,
        aboutMenu,
        overwriteSaveFile,
        startNewFile,
        deleteMusicFile,
        quickReference,
        quickReferenceDetail,
        playlistSearch,
        advancedOptionsMenu,
        none
    }

    internal MenuState currentMenuState;

    // Start is called before the first frame update
    public void Start()
    {
        //PlayerPrefs.DeleteKey("Crossfade");
        //print(PlayerPrefs.GetFloat("Crossfade") == 0);
        if (PlayerPrefs.GetFloat("Crossfade") == 0) PlayerPrefs.SetFloat("Crossfade", 10);
        VERSION = Application.version;
        pauseImage = pause.GetComponent<SpriteRenderer>().sprite;
        playImage = play.GetComponent<SpriteRenderer>().sprite;
        stopImage = stop.GetComponent<SpriteRenderer>().sprite;

        pageParents = new List<PageParent>();
        pageButtons = new List<GameObject>();
        sfxButtons = new List<List<GameObject>>();

        epl = GetComponent<EditPageLabel>();
        mc = GetComponent<MusicController>();
        dmc = GetComponent<DarkModeController>();
        qrc = GetComponent<QuickReferenceController>();
        qrd = GetComponent<QuickRefDetailView>();
        gmfb = GetComponent<GenerateMusicFFTBackgrounds>();
        dm = GetComponent<DiscoMode>();
        omc = GetComponent<OptionsMenuController>();

        sep = System.IO.Path.DirectorySeparatorChar;

        MakeSFXButtons();

        pageParents[0].gameObject.transform.SetSiblingIndex(NUMPAGES);

        mainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "TableTopAudio");
        musicDirectory = Path.Combine(mainDirectory, "music");
        sfxDirectory = Path.Combine(mainDirectory, "sound effects");
        saveDirectory = Path.Combine(mainDirectory, "saves");
        videoDirectory = Path.Combine(mainDirectory, "videos");

        SetupFolderStructure(mainDirectory);

        bool darkModeEnabled = false;
        try
        {
            darkModeEnabled = Convert.ToBoolean(PlayerPrefs.GetString("darkMode"));
        }
        catch (FormatException){ }
        SwapDarkLightMode(darkModeEnabled);
        LoadQrdObjects();

        MakeCategoryColors();
        this.currentMenuState = MenuState.mainAppView;
        //player = GetComponentInChildren<VideoPlayer>();
        //player.url = Path.Combine(Application.streamingAssetsPath, "avicii.mp4");
        //player.Play();
        //player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        //player.SetTargetAudioSource(1, GetComponent<AudioSource>());
    }

    void MakeCategoryColors()
    {
        if (ResourceManager.categoryColors.Count == 0)
        {
            int i = 0;
            foreach (string category in ResourceManager.dbFiles)
            {
                ResourceManager.categoryColors.Add(category, UIntToColor(ResourceManager.kellysMaxContrastSet[i]));
                i++;
            }
        }
    }

    static public Color UIntToColor(uint color)
    {
        float r = (byte)(color >> 16) / 255f;
        float g = (byte)(color >> 8) / 255f;
        float b = (byte)(color >> 0) / 255f;
        return new Color(r, g, b);
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
            GameObject pageButton = Instantiate(pageButtonPrefab, pageButtonParent.transform);
            pageButton.GetComponentInChildren<TMP_Text>().text = (i + 1).ToString() ;
            pageButton.GetComponent<PageButton>().id = i;
            pageButtons.Add(pageButton);
            pageButton.transform.SetSiblingIndex(i + 1);
            
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
            if (i == 0) pageButton.GetComponent<Image>().color = Color.red;
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
                    foreach (GameObject sfxButton in page)
                    {
                        sfxButton.GetComponent<SFXButton>().Stop();
                    }
                }
                break;                
            case "OPTIONS":
                optionsPanel.SetActive(true);
                currentMenuState = MenuState.optionsMenu;
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
            case "CROSSFADE":
                mc.Crossfade = !mc.Crossfade;
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
        epl.ButtonLabel = label;
        epl.StartEditing();
    }

    internal void ShowErrorMessage(string message)
    {
        GameObject error = Instantiate(errorPrefab, errorMessagesPanel.transform);
        error.GetComponentInChildren<TMP_Text>().text = "Error: " + message;
        Debug.LogError(message);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tilde) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            switch (currentMenuState)
            {
                case MenuState.mainAppView:
                    qrc.ShowLookupMenu();
                    break;
                case MenuState.quickReference:
                    qrc.HideLookupMenu();
                    break;
            }

        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch(currentMenuState)
            {
                case MenuState.mainAppView:
                    ControlButtonClicked("OPTIONS");
                    break;
                case MenuState.optionsMenu:
                    omc.Close();
                    break;
                case MenuState.aboutMenu:
                    omc.CloseAboutMenu();
                    break;
                case MenuState.enterSaveFileName:
                    omc.CloseSaveMenu();
                    break;
                case MenuState.selectFileToLoad:
                    omc.CloseLoadSelection();
                    break;
                case MenuState.editingPageLabel:
                    GetComponent<EditPageLabel>().Cancel();
                    break;
                case MenuState.selectSFXFile:
                    GetComponent<FileSelectViewController>().CloseFileSelection();
                    break;
                case MenuState.editingSFXButton:
                    GetComponent<ButtonEditorController>().CancelEditing();
                    break;
                case MenuState.overwriteSaveFile:
                    omc.CancelOverwriteSave();
                    break;
                case MenuState.startNewFile:
                    omc.CancelNewFile();
                    break;
                case MenuState.deleteMusicFile:
                    GetComponent<MusicController>().CloseDeleteMusicItemTooltip();
                    break;
                case MenuState.quickReference:
                    qrc.HideLookupMenu();
                    break;
                case MenuState.quickReferenceDetail:
                    qrd.CloseQuickReferenceDetail();
                    break;
                case MenuState.playlistSearch:
                    mc.SearchFieldLostFocus();
                    break;
                case MenuState.advancedOptionsMenu:
                    omc.CloseAdvancedOptionsMenu();
                    break;
                case MenuState.none:
                    break;
            }
        }
        if(Input.GetKeyDown(KeyCode.Return))
        {
            switch(currentMenuState)
            {
                case MenuState.optionsMenu:
                    break;
                case MenuState.aboutMenu:
                    break;
                case MenuState.enterSaveFileName:
                    omc.AcceptSaveName();
                    break;
                case MenuState.selectFileToLoad:
                    break;
                case MenuState.editingPageLabel:
                    GetComponent<EditPageLabel>().Confirm();
                    break;
                case MenuState.selectSFXFile:
                    break;
                case MenuState.editingSFXButton:
                    GetComponent<ButtonEditorController>().ApplySettings();
                    break;
                case MenuState.none:
                    break;
            }
        }
        if(Input.GetKeyDown(KeyCode.Space) && currentMenuState == MenuState.mainAppView)
        {
            mc.SpacebarPressed();
        }
        if((Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(0);
            pageButtons[0].GetComponent<Image>().color = Color.red;
        }
        if ((Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(1);
            pageButtons[1].GetComponent<Image>().color = Color.red;
        }
        if ((Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(2);
            pageButtons[2].GetComponent<Image>().color = Color.red;
        }
        if ((Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(3);
            pageButtons[3].GetComponent<Image>().color = Color.red;
        }
        if ((Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(4);
            pageButtons[4].GetComponent<Image>().color = Color.red;
        }
        if ((Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(5);
            pageButtons[5].GetComponent<Image>().color = Color.red;
        }
        if ((Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(6);
            pageButtons[6].GetComponent<Image>().color = Color.red;
        }
        if ((Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) && currentMenuState == MenuState.mainAppView)
        {
            ChangeSFXPage(7);
            pageButtons[7].GetComponent<Image>().color = Color.red;
        }
        if((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.D) && discoModeAvailable) {
            dm.SetDiscoMode(!dm.discoModeActive); //terribly disgusting. Please fix :/
        }
    }
    public void SwapDarkLightMode(bool enable)
    {
        darkModeEnabled = enable;
        dmc.SwapDarkMode(darkModeEnabled);
        PlayerPrefs.SetString("darkMode", enable.ToString());
    }

    public void LoadQrdObjects()
    {
        foreach(string s in ResourceManager.dbFiles)
        {
            try
            {
                string fileLocation = Path.Combine(Application.streamingAssetsPath, s);
                string contents = System.IO.File.ReadAllText(fileLocation);
                QuickRefObject data = JsonSerializer.Deserialize<QuickRefObject>(contents);
                Dictionary<string, Dictionary<string, dynamic>> categoryItems = new Dictionary<string, Dictionary<string, dynamic>>();
                foreach (var item in data.contents)
                {
                    var attributes = new Dictionary<string, dynamic>();
                    foreach (var dict in item)
                    {
                        attributes.Add(dict.Key, dict.Value);
                    }
                    categoryItems.Add(item["index"].ToString(), attributes);
                }
                LoadedFilesData.qrdFiles[s.Replace(".json", "")] = categoryItems;
            }
            catch (FileNotFoundException) 
            {
                ShowErrorMessage("Could not load quick reference file " + s + ". Check that it exists.");
            }
        }
    }

    internal void ToggleDiscoMode()
    {
        this.discoModeAvailable = !this.discoModeAvailable;
        dm.SetDiscoMode(false);
    }
}
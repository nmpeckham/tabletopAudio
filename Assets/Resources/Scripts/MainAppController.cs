using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TMPro;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


//Main controller for the app. Handles various tasks
public class MainAppController : MonoBehaviour
{
    public Canvas mainCanvas;
    internal const int NUMPAGES = 11;
    internal const int NUMBUTTONS = 35;
    internal string VERSION;  //save version

    internal static string appDirectoryName = "TableTopManager";
    internal static string mainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), appDirectoryName);
    internal static Dictionary<string, string> workingDirectories = new()
    {

        { "musicDirectory", Path.Combine(mainDirectory, "music") },
        { "sfxDirectory", Path.Combine(mainDirectory, "sound effects") },
        { "saveDirectory", Path.Combine(mainDirectory, "saves") },
        { "logsDirectory", Path.Combine(mainDirectory, "logs") },
    };

    private MusicController mc;
    private EditPageLabel epl;
    private DarkModeController dmc;
    private DiscoMode dm;
    private PlaylistTabs pt;
    private SFXPageController spc;

    public GameObject optionsPanel;

    public GameObject errorMessagesPanel;

    public GameObject pause;
    public GameObject play;
    public GameObject stop;

    internal Sprite pauseImage;
    internal Sprite playImage;
    internal Sprite stopImage;

    internal static bool darkModeEnabled = false;
    private QuickReferenceController qrc;
    private QuickRefDetailView qrd;

    private GenerateMusicFFTBackgrounds gmfb;

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
        musicRightClickMenu,
        quickReference,
        quickReferenceDetail,
        playlistSearch,
        advancedOptionsMenu,
        editTabLabel,
        shortcutView,
        searchFoldersMenu,
        secretSettingsMenu
    }
    private MenuState CurrentMenuState;

    internal MenuState currentMenuState
    {
        set { 
            CurrentMenuState = value;
            MenuStateChanged();
        }
        get { return CurrentMenuState; }
    }

    private int thankyouMessagesShown = 0;
    internal static Transform tooltipParent;

    public GameObject fpsParent;
    public TMP_Text fpsDisplayText;
    internal readonly List<KeyCode> controlKeys = new()
    {
        KeyCode.LeftControl,
        KeyCode.RightControl
    };

    internal readonly List<KeyCode> altKeys = new()
    {
        KeyCode.LeftAlt,
        KeyCode.RightAlt
    };

    private readonly List<KeyCode> shiftKeys = new()
    {
        KeyCode.LeftShift,
        KeyCode.RightShift
    };

    internal int currentFPS = 0;


    // Start is called before the first frame update

    public void MenuStateChanged()
    {
        GetComponent<AppStateViewDebug>().MenuStateChanged(currentMenuState.ToString());
    }
    public void Start()
    {
        //if(!Application.isEditor)
        //{
        //    //PlayerPrefs.DeleteAll();
        //    Application.targetFrameRate = Screen.currentResolution.refreshRate;
        //}

        Prefabs.LoadAll();
        if (PlayerPrefs.GetFloat("Crossfade") == 0)
        {
            PlayerPrefs.SetFloat("Crossfade", 3);
        }
        bool shortcutsShown = false;
        try
        {
            shortcutsShown = Convert.ToBoolean(PlayerPrefs.GetString("shortcutsShown"));
        }
        catch (FormatException) { }


        try
        {
            thankyouMessagesShown = PlayerPrefs.GetInt("thankYouMessagesShown");
        }
        catch (FormatException) { }

        VERSION = Application.version;
        ResourceManager.Init();
        pauseImage = pause.GetComponent<SpriteRenderer>().sprite;
        playImage = play.GetComponent<SpriteRenderer>().sprite;
        stopImage = stop.GetComponent<SpriteRenderer>().sprite;

        if (!shortcutsShown)
        {
            PlayerPrefs.SetString("darkMode", "true");
        }

        epl = GetComponent<EditPageLabel>();
        mc = GetComponent<MusicController>();
        dmc = GetComponent<DarkModeController>();
        qrc = GetComponent<QuickReferenceController>();
        qrd = GetComponent<QuickRefDetailView>();
        gmfb = GetComponent<GenerateMusicFFTBackgrounds>();
        dm = GetComponent<DiscoMode>();
        omc = GetComponent<OptionsMenuController>();
        pt = GetComponent<PlaylistTabs>();
        spc = GetComponent<SFXPageController>();


        SetupFolderStructure();
        spc.Init();

        bool darkModeEnabled = false;
        try
        {
            darkModeEnabled = Convert.ToBoolean(PlayerPrefs.GetString("darkMode"));
        }
        catch (FormatException) { }
        EnableDarkMode(darkModeEnabled);
        StartCoroutine(LoadQrdObjects());

        MakeCategoryColors();
        currentMenuState = MenuState.mainAppView;
        GetComponent<PlaylistTabs>().Init();
        GetComponent<SearchFoldersController>().Init();
        mc.Init();
        NowPlayingWebpage.Init();
        GetComponent<FftController>().Init();
        tooltipParent = GameObject.Find("Tooltips").transform;

        JobsUtility.JobWorkerCount = SystemInfo.processorCount - 1; // boosts fps by 100% :/
        Screen.fullScreenMode = FullScreenMode.Windowed;

    }

    private void MakeCategoryColors()
    {
        if (ResourceManager.categoryColors.Count == 0)
        {
            int i = 0;
            foreach (string category in ResourceManager.dbFiles)
            {
                ResourceManager.categoryColors.Add(category.Replace(Application.streamingAssetsPath + Path.DirectorySeparatorChar, "").Replace(".json", ""), UIntToColor(ResourceManager.kellysMaxContrastSet[i % ResourceManager.kellysMaxContrastSet.Count]));
                i++;
            }
        }
    }

    public static Color UIntToColor(uint color)
    {
        float r = (byte)(color >> 16) / 255f;
        float g = (byte)(color >> 8) / 255f;
        float b = (byte)(color >> 0) / 255f;
        return new Color(r, g, b);
    }

    internal void SetupFolderStructure()
    {

        if (!Directory.Exists(mainDirectory))
        {
            Directory.CreateDirectory(mainDirectory);
        }
        foreach (string value in workingDirectories.Values)
        {
            if (!Directory.Exists(value))
            {
                Directory.CreateDirectory(value);
            }
        }
    }


    public bool ControlButtonClicked(string id)
    {
        switch (id)
        {
            case "STOP-SFX":
                spc.StopAll();
                break;
            case "OPTIONS":
                optionsPanel.SetActive(true);
                currentMenuState = MenuState.optionsMenu;
                break;
            case "REPEAT":
                mc.Repeat = !mc.Repeat;
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
            case "SORT":
                pt.SortSongs();
                break;
        }
        return true;
    }



    internal void EditPageLabel(TMP_Text label)
    {
        epl.ButtonLabel = label;
        epl.StartEditing();
    }



    internal GameObject ShowErrorMessage(string message, int level = 0, int time = 8, bool sticky = false)
    {
        //level 0 = error, 1 = warn, 2 = info
        GameObject error = Instantiate(Prefabs.errorPrefab, errorMessagesPanel.transform);
        ErrorMessage em = error.GetComponentInChildren<ErrorMessage>();
        em.Init();
        TMP_Text messageText = em.thisText;
        Image messageTypeImage = em.typeImage;
        //means show song loading message and spinner

        if (level == 0)
        {
            Debug.LogError(message);
            messageText.text = "      Error: " + message;
            messageText.color = Color.red;
            messageTypeImage.sprite = ResourceManager.errorImage;
            messageTypeImage.color = Color.red;
        }
        if (level == 1)
        {
            Debug.LogWarning(message);
            messageText.text = "      Warning: " + message;
            messageText.color = Color.yellow;
            messageTypeImage.sprite = ResourceManager.warningImage;
            messageTypeImage.color = Color.yellow;
        }
        if (level == 2)
        {
            Debug.Log(message);
            messageText.text = "      Info: " + message;
            messageText.color = Color.white;
            messageTypeImage.sprite = ResourceManager.infoImage;
        }
        if (sticky)
        {
            messageText.text = "     " + message;
            error.GetComponent<ErrorMessage>().Spinner.SetActive(true);
        }
        else
        {
            error.transform.SetSiblingIndex(0);
        }
        error.GetComponentInChildren<ErrorMessage>().delayTime = time;
        return error;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Z) && ControlKeyPressed() && ShiftKeyPressed() && currentMenuState == MenuState.mainAppView)
        {
            omc.ShowSecretSettings();
        }
        if (Input.GetKeyDown(KeyCode.Q) && ControlKeyPressed())
        {
            fpsParent.SetActive(!fpsParent.activeSelf);
        }
        //Save current file
        if (Input.GetKeyDown(KeyCode.S) && ControlKeyPressed())
        {
            print(omc.currentSaveName);
            if (!string.IsNullOrEmpty(omc.currentSaveName))
            {
                omc.AcceptSaveName(true);
            }
            else
            {
                ControlButtonClicked("OPTIONS");
                omc.OpenSaveNamePanel();
            }
        }
        //Open quick ref menu
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
        currentFPS = (int)(1f / Time.smoothDeltaTime);
        if (fpsParent.activeSelf)
        {
            fpsDisplayText.text = currentFPS.ToString() + "fps";
        }

        //Abandoned in favor menu option
        //if(Input.GetKeyDown(KeyCode.F11))
        //{
        //    if(Screen.fullScreenMode == FullScreenMode.Windowed)
        //    {
        //        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        //    }
        //    else
        //    {
        //        Screen.fullScreenMode = FullScreenMode.Windowed;
        //    }
        //}

        // Exit on alt-F12
        if(AltKeyPressed() && Input.GetKeyDown(KeyCode.F12))
        {
            Application.Quit(0);
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch (currentMenuState)
            {
                case MenuState.mainAppView:
                    ControlButtonClicked("OPTIONS");
                    break;
                case MenuState.optionsMenu:
                    omc.CloseOptionsMenu();
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
                case MenuState.musicRightClickMenu:
                    GetComponent<PlaylistRightClickController>().CloseDeleteMusicItemTooltip();
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
                case MenuState.editTabLabel:
                    GetComponent<PlaylistTabs>().CancelNameChange();
                    break;
                case MenuState.shortcutView:
                    GetComponent<ShortcutViewController>().CloseShortcuts();
                    break;
                case MenuState.searchFoldersMenu:
                    GetComponent<SearchFoldersController>().CloseButtonClicked();
                    break;
            }
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            switch (currentMenuState)
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
                case MenuState.editTabLabel:
                    GetComponent<PlaylistTabs>().ConfirmNameChange();
                    break;
            }
        }
        if(currentMenuState == MenuState.mainAppView)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                mc.SpacebarPressed();
            }
            if ((Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)))
            {
                spc.ChangeSFXPage(0);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)))
            {
                spc.ChangeSFXPage(1);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)))
            {
                spc.ChangeSFXPage(2);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)))
            {
                spc.ChangeSFXPage(3);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)))
            {
                spc.ChangeSFXPage(4);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)))
            {
                spc.ChangeSFXPage(5);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)))
            {
                spc.ChangeSFXPage(6);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)))
            {
                spc.ChangeSFXPage(7);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)))
            {
                spc.ChangeSFXPage(8);
            }
            if ((Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)))
            {
                spc.ChangeSFXPage(9);
            }
        }
        
        if (ControlKeyPressed() && Input.GetKeyDown(KeyCode.D) && discoModeAvailable)
        {
            dm.SetDiscoMode(!dm.discoModeActive); //terribly disgusting. Please fix :/
        }
        if (ControlKeyPressed() && Input.GetKeyDown(KeyCode.F) && currentMenuState == MenuState.mainAppView)
        {
            mc.GiveSearchFieldFocus();
        }
        if (thankyouMessagesShown == 0 && Time.time > 1800 && !Application.isEditor)
        {
            PlayerPrefs.SetInt("thankYouMessagesShown", 1);
            ShowErrorMessage("Thanks for using TableTopAudio for 30 minutes! I'm always looking to improve it.\n Please send me an email at me@nathanpeckham.com with any feedback you have", 2, 10);
            thankyouMessagesShown = 1;
        }
        if (thankyouMessagesShown == 1 && Time.time > 3600 && !Application.isEditor)
        {
            PlayerPrefs.SetInt("thankYouMessagesShown", 2);
            ShowErrorMessage("Thanks for using TableTopAudio for 60 minutes! I'm always looking to improve it.\n Please send me an email at me@nathanpeckham.com with any feedback you have", 2, 10);
            thankyouMessagesShown = 2;
        }
    }

    private bool ShiftKeyPressed()
    {
        return shiftKeys.Any(key => Input.GetKey(key));
    }

    private bool ControlKeyPressed()
    {
        return controlKeys.Any(key => Input.GetKey(key));
    }

    private bool AltKeyPressed()
    {
        return altKeys.Any(key => Input.GetKey(key));
    }

    public void EnableDarkMode(bool enable)
    {
        darkModeEnabled = enable;
        dmc.SwapDarkMode(darkModeEnabled);
        PlayerPrefs.SetString("darkMode", enable.ToString());
    }

    public IEnumerator LoadQrdObjects()
    {
        foreach (string s in ResourceManager.dbFiles)
        {
            if (s[0] != '~') //prefix with ~ to hide.
            {
                if (System.IO.File.Exists(s))
                {
                    string contents = System.IO.File.ReadAllText(s);
                    QuickRefObject data = JsonSerializer.Deserialize<QuickRefObject>(contents);
                    Dictionary<string, Dictionary<string, dynamic>> categoryItems = new();
                    string categoryName = "";
                    foreach (var item in data.Contents)
                    {
                        var attributes = new Dictionary<string, dynamic>();
                        foreach (var dict in item)
                        {
                            attributes.Add(dict.Key, dict.Value);
                            categoryName = s.Replace(".json", "").Replace(Application.streamingAssetsPath + Path.DirectorySeparatorChar, "");
                            if (!attributes.ContainsKey("categoryName"))
                            {
                                attributes.Add("categoryName", categoryName);
                            }

                            if (currentFPS < 30)
                            {
                                yield return null;
                            }
                        }
                        categoryItems.Add(item["index"].ToString(), attributes);
                    }
                    LoadedFilesData.qrdFiles[categoryName] = categoryItems;
                }
                yield return null;
            }

        }
    }

    internal void ToggleDiscoMode()
    {
        discoModeAvailable = !discoModeAvailable;
        dm.SetDiscoMode(false);
    }


}
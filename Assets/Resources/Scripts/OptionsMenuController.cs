using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Controls the options menu
public class OptionsMenuController : MonoBehaviour
{
    public GameObject confirmNewFilePanel;
    public TMP_InputField saveNameField;
    public GameObject saveNamePanel;

    public Toggle autoUpdatePlaylistToggle;

    public GameObject optionsPanel;

    public GameObject aboutMenu;
    public TMP_Text versionText;

    public GameObject loadGameSelectionView;
    public GameObject loadGameScrollView;

    public TMP_InputField crossfadeField;
    private bool hadFormatException = false;

    private static MainAppController mac;
    private static MusicController mc;
    private static SearchFoldersController sfc;
    private static SFXPageController spc;
    private static RemoteTriggerController rtc;

    public TMP_Text saveErrorText;

    public Toggle darkModeToggle;

    public GameObject overwriteSavePanel;

    private string saveFileName;

    private readonly char[] bannedCharacters =
    {
        ':',
        '\\',
        '/',
        '<',
        '>',
        '"',
        '|',
        '?',
        '*'
    };

    public GameObject advancedOptionsMenu;

    private DiscoMode dm;
    private GenerateMusicFFTBackgrounds gmfb;

    public TMP_Text dmCooldownText;
    public TMP_Text dmMinSumText;
    public TMP_Text dmNumFreqText;

    private PlaylistTabs pt;
    internal string currentSaveName = "";

    public TMP_Dropdown fullscreenModeDropdown;

    public Button InitialPlaylistAddMusicButton;

    public GameObject secretSettingsPanel;
    public Toggle enableRemoteTriggerToggle;
    public Button closeRemoteTriggerPanelButton;
    public Button exitRemoteTriggerPanelButton;
    public Button assignButtonsButton;

    // Start is called before the first frame update

    private void Start()
    {
        if (!Application.isEditor)
        {
            optionsPanel.SetActive(false);
        }

        saveErrorText.enabled = false;
        mc = GetComponent<MusicController>();
        mac = GetComponent<MainAppController>();
        sfc = GetComponent<SearchFoldersController>();
        spc = GetComponent<SFXPageController>();
        rtc = GetComponent<RemoteTriggerController>();

        darkModeToggle.onValueChanged.AddListener(DarkModeChanged);
        autoUpdatePlaylistToggle.onValueChanged.AddListener(AutoUpdateChanged);
        crossfadeField.onValueChanged.AddListener(CrossfadeTimeChanged);
        darkModeToggle.SetIsOnWithoutNotify(MainAppController.darkModeEnabled);
        fullscreenModeDropdown.onValueChanged.AddListener(FullScreenModeChanged);
        enableRemoteTriggerToggle.onValueChanged.AddListener(RemoteTriggerToggled);
        InitialPlaylistAddMusicButton.onClick.AddListener(PlaylistAddMusicButtonClicked);
        float val = PlayerPrefs.GetFloat("Crossfade");
        if (val == 0)
        {
            val = 4f; //default crossfade time
        }

        mc.CrossfadeTime = val;
        crossfadeField.text = val.ToString();

        dm = GetComponent<DiscoMode>();
        gmfb = GetComponent<GenerateMusicFFTBackgrounds>();
        pt = GetComponent<PlaylistTabs>();
    }

    private void PlaylistAddMusicButtonClicked()
    {
        OptionMenuButtonClicked("show search folders");
        InitialPlaylistAddMusicButton.transform.parent.gameObject.SetActive(false);
    }



    //0 = Windowed, 1 = Fullscreen Windowed, 2 = Fullscreen Exclusive
    private void FullScreenModeChanged(int idSelected)
    {
        StartCoroutine(ChangeDisplayMode(idSelected));
    }

    IEnumerator ChangeDisplayMode(int idSelected)
    {
        switch (idSelected)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 1:
                //Janky hack to force all canvas layout elements to refresh :/
                //Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                //yield return null;
                Screen.SetResolution(Screen.mainWindowDisplayInfo.width, Screen.mainWindowDisplayInfo.height, FullScreenMode.FullScreenWindow);
                //Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                //Canvas.ForceUpdateCanvases();
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
        }
        yield return null;
    }

    internal void CloseAdvancedOptionsMenu()
    {
        advancedOptionsMenu.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    private void OpenAdvancedOptionsMenu()
    {
        advancedOptionsMenu.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.advancedOptionsMenu;
    }

    internal void OptionMenuSliderChanged(string id, float val)
    {
        switch (id)
        {
            case "DiscoModeCooldown":
                dm.cooldown = val;
                dmCooldownText.text = val.ToString();
                break;
            case "DiscoModeNumFreq":
                FftController.discoModeNumFreq = val;
                dmNumFreqText.text = val.ToString();
                break;
            case "DiscoModeMinSum":
                FftController.discoModeMinSum = val;
                dmMinSumText.text = val.ToString("N2");
                break;
        }
    }

    private void StartNewFile()
    {
        confirmNewFilePanel.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.startNewFile;
    }

    private void ConfirmNewFile()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        LoadedFilesData.songs.Clear();
        LoadedFilesData.sfxClips.Clear();
        LoadedFilesData.DeletedMusicClips.Clear();
        mc.StartNewFile();
    }

    internal void CancelNewFile()
    {
        confirmNewFilePanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    private void ConfirmOverwriteSave()
    {
        Save(saveFileName, true);
        overwriteSavePanel.SetActive(false);
    }

    internal void CancelOverwriteSave()
    {
        overwriteSavePanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.enterSaveFileName;
    }

    private void DarkModeChanged(bool value)
    {
        mac.EnableDarkMode(value);
    }

    private void CrossfadeTimeChanged(string value)
    {
        try
        {
            float val = 0;
            if (hadFormatException)
            {
                // will remove the default 1 that is inputed after an error, when the user types a new value. I think.
                if (value[^1] == '1')
                {
                    val = Convert.ToSingle(value.Remove(value.Length - 1, 1));
                }

                hadFormatException = false;
            }

            else
            {
                val = Mathf.Max(1, Math.Min(30, Convert.ToSingle(value)));
            }

            mc.CrossfadeTime = val;
            crossfadeField.text = val.ToString();
            PlayerPrefs.SetFloat("Crossfade", val);
        }
        catch (FormatException)
        {
            crossfadeField.text = "0";
            mc.CrossfadeTime = 0;
            hadFormatException = true;
        }
    }

    internal void OpenSaveNamePanel()
    {
        saveNamePanel.SetActive(true);
        saveNameField.ActivateInputField();
        saveNameField.text = currentSaveName;
        mac.currentMenuState = MainAppController.MenuState.enterSaveFileName;
    }

    internal void AcceptSaveName(bool overwrite = false)
    {

        bool errorFound = false;
        string text = saveNameField.text;
        print("saving " + text);
        foreach (char item in bannedCharacters)
        {
            if (text.Contains(item))
            {
                errorFound = true;
                saveErrorText.enabled = true;
                break;
            }
        }

        if (!errorFound)
        {
            saveErrorText.enabled = false;
            Save(saveNameField.text, overwrite);
        }
    }

    internal void CloseSaveMenu()
    {
        saveNamePanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    internal void AutoUpdateChanged(bool val)
    {
        if (autoUpdatePlaylistToggle.isOn != val)
        {
            autoUpdatePlaylistToggle.isOn = val;
        }

        if (mc.AutoCheckForNewFiles != val)
        {
            mc.AutoCheckForNewFiles = val;
        }
    }

    internal void CloseLoadSelection()
    {
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
        loadGameSelectionView.SetActive(false);
    }

    private void OpenAboutMenu()
    {
        versionText.text = "<b>TableTopManger v" + mac.VERSION + "</b>";
        aboutMenu.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.aboutMenu;
    }

    internal void CloseAboutMenu()
    {
        aboutMenu.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    internal void CloseOptionsMenu()
    {
        optionsPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    private void Load()
    {
        mac.currentMenuState = MainAppController.MenuState.selectFileToLoad;
        foreach (Transform t in loadGameScrollView.GetComponentsInChildren<Transform>())
        {
            if (t.gameObject.name != "loadGameScrollView")
            {
                Destroy(t.gameObject);
            }
        }

        loadGameSelectionView.SetActive(true);

        try
        {
            foreach (string saveName in Directory.GetFiles(MainAppController.workingDirectories["saveDirectory"]))
            {
                if (Path.GetExtension(saveName) == ".xml" || Path.GetExtension(saveName) == ".txt")
                {
                    string trimmedSaveName = saveName.Replace(MainAppController.workingDirectories["saveDirectory"], "").Replace(Path.GetExtension(saveName), "").Replace(Path.DirectorySeparatorChar.ToString(), "");
                    GameObject scrollItem = Instantiate(Prefabs.loadGameItemPrefab, loadGameScrollView.transform);
                    scrollItem.GetComponent<LoadGameSelectItem>().fileLocation = saveName;
                    scrollItem.GetComponentInChildren<TMP_Text>().SetText(trimmedSaveName);
                }
            }
        }
        catch(DirectoryNotFoundException)
        {
            mac.SetupFolderStructure();
        }

    }
    #region SecretSettings
    internal void ShowSecretSettings()
    {
        secretSettingsPanel.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.secretSettingsMenu;
    }

    internal void CloseSecretSettings()
    {
        secretSettingsPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    private void RemoteTriggerToggled(bool state)
    {
        if (state)
        {
            rtc.Init();
        }
        else
        {
            rtc.Close();
        }
    }
    #endregion

    public void LoadItemSelected(string fileLocation)
    {
        currentSaveName = Path.GetFileName(fileLocation).Replace(Path.GetExtension(fileLocation), "");
        mc.AutoCheckForNewFiles = false;
        autoUpdatePlaylistToggle.isOn = false;
        mc.Stop();
        mac.ControlButtonClicked("STOP-SFX");
        DestroyItems();
        spc.MakeSFXButtons();
        pt.TabClicked(0);
        pt.DeleteAllTabs();
        MusicController.fileType = MusicController.FileTypes.none;
        mc.ChangePlaybackScrubberActive(false);
        sfc.ClearSearchFolders();

        XmlDocument file = new();
        try
        {
            file.Load(fileLocation);

            string appLocation = "/" + file.DocumentElement.Name;

            //string version = Convert.ToString(file.SelectSingleNode("/" + appLocation + "-Save-File/version").InnerText);
            if (true)//version == MainAppController.VERSION) //No version checking yet
            {
                LoadedFilesData.songs.Clear();
                LoadedFilesData.sfxClips.Clear();
                LoadedFilesData.DeletedMusicClips.Clear();

                float masterVolume = Convert.ToSingle(file.SelectSingleNode(appLocation + "/masterVolume").InnerText);
                float musicVolume = Convert.ToSingle(file.SelectSingleNode(appLocation + "/musicVolume").InnerText);
                bool shuffle = Convert.ToBoolean(file.SelectSingleNode(appLocation + "/shuffle").InnerText);
                bool crossfade = false;
                try
                {
                    crossfade = Convert.ToBoolean(file.SelectSingleNode(appLocation + "/crossfade").InnerText);
                }
                catch (NullReferenceException) { }
                Camera.main.GetComponent<VolumeController>().MasterVolume = masterVolume;
                mc.MusicVolume = musicVolume;

                XmlNodeList watchFolders = file.SelectNodes(appLocation + "/watchFolders/path");
                print(watchFolders.Count);
                foreach(XmlNode p in watchFolders)
                {
                    sfc.AddSearchFolderItem(p.InnerText);
                }

                XmlNodeList pages = file.SelectNodes(appLocation + "/SFX-Buttons/page");

                foreach (XmlNode p in pages)
                {
                    int page = Convert.ToInt32(p.SelectSingleNode("id").InnerText);
                    string pageLabel = page.ToString();
                    try
                    {
                        pageLabel = p.SelectSingleNode("label").InnerText;
                    }
                    catch (NullReferenceException) { }

                    spc.pageButtons[page].GetComponent<PageButton>().Label = pageLabel;
                    foreach (XmlNode b in p.SelectNodes("button"))
                    {
                        string label = b.SelectSingleNode("label").InnerText;
                        int id = Convert.ToInt32(b.SelectSingleNode("id").InnerText);
                        string clipPath = null;
                        try
                        {
                            clipPath = b.SelectSingleNode("clipPath").InnerText;
                        }
                        catch (NullReferenceException)
                        {
                            try
                            {
                                clipPath = b.SelectSingleNode("clipID").InnerText;
                            }
                            catch (Exception)
                            {
                                mac.ShowErrorMessage("Could not load save file. Error finding clip path", 0);
                            }
                        }
                        float localVolume = Convert.ToSingle(b.SelectSingleNode("localVolume").InnerText);
                        bool loop = Convert.ToBoolean(b.SelectSingleNode("loop").InnerText);
                        bool randomizeLoopTime = Convert.ToBoolean(b.SelectSingleNode("randomizeLoopDelay").InnerText);
                        float minLoopDelay = Convert.ToSingle(b.SelectSingleNode("minLoopDelay").InnerText);
                        float maxLoopDelay = Convert.ToSingle(b.SelectSingleNode("maxLoopDelay").InnerText);
                        float minFadeVolume = 0;
                        float maxFadeVolume = 1;
                        bool ignoreOnPlayAll = false;
                        try
                        {
                            ignoreOnPlayAll = Convert.ToBoolean(b.SelectSingleNode("ignoreOnPlayAll").InnerText);
                        }
                        catch (NullReferenceException) { }
                        try
                        {
                            minFadeVolume = Convert.ToSingle(b.SelectSingleNode("minFadeVolume").InnerText);
                            maxFadeVolume = Convert.ToSingle(b.SelectSingleNode("maxFadeVolume").InnerText);
                        }
                        catch (NullReferenceException) { }
                        float edgeRed = 1f;
                        float edgeGreen = 1f;
                        float edgeBlue = 1f;

                        try
                        {
                            XmlNode c = b.SelectSingleNode("edgeColor");
                            {
                                print(c.Attributes);
                                edgeRed = Convert.ToSingle(c.SelectSingleNode("r").InnerText);
                                edgeGreen = Convert.ToSingle(c.SelectSingleNode("g").InnerText);
                                edgeBlue = Convert.ToSingle(c.SelectSingleNode("b").InnerText);
                                print(clipPath);
                                print(edgeRed);
                                print(edgeGreen);
                                print(edgeBlue);

                            }
                        }
                        catch (NullReferenceException) { }

                        SFXButton sfxBtn = spc.pageParents[page].buttons[id].GetComponent<SFXButton>();
                        sfxBtn.Label = label;
                        sfxBtn.id = id;
                        sfxBtn.FileName = clipPath;
                        sfxBtn.LocalVolume = localVolume;
                        sfxBtn.ChangeMasterVolume(masterVolume);
                        sfxBtn.LoopEnabled = loop;
                        sfxBtn.RandomizeLoopDelay = randomizeLoopTime;
                        sfxBtn.MinLoopDelay = minLoopDelay;
                        sfxBtn.MaxLoopDelay = maxLoopDelay;
                        sfxBtn.minimumFadeVolume = minFadeVolume;
                        sfxBtn.maximumFadeVolume = maxFadeVolume;
                        sfxBtn.IgnorePlayAll = ignoreOnPlayAll;
                        sfxBtn.ButtonEdgeColor = new Color(edgeRed, edgeGreen, edgeBlue);

                    }
                }
                LoadedFilesData.DeletedMusicClips.Clear();
                XmlNodeList deleted = file.SelectNodes(appLocation + "/removed/a");
                mc.AutoCheckForNewFiles = true;
                foreach (XmlNode d in deleted)
                {
                    LoadedFilesData.DeletedMusicClips.Add(d.InnerText);
                    print("added " + d.InnerText);
                }
                //print(LoadedFilesData.DeletedMusicClips[0]);
                int tabId = 0;
                foreach (XmlNode n in file.SelectNodes(appLocation + "/playlist/tab"))
                {
                    string label = n.SelectSingleNode("label").InnerText.ToString();
                    if (label != "*")
                    {
                        pt.TabClicked(-1);
                        PlaylistTabs.tabs.Last().LabelText = label;
                    }
                    List<string> files = new();
                    foreach (XmlNode s in n)
                    {
                        if (s.Name == "song")
                        {
                            files.Add(s.InnerText.Replace(Path.Combine("TableTopAudio", "music"), Path.Combine(MainAppController.appDirectoryName, "music")));  //support for saves made on previous versions
                        }
                    }
                    files.ForEach(file => mc.AddNewSong(file, tabId, true));
                    tabId++;
                }
                loadGameSelectionView.SetActive(false);

                InitialPlaylistAddMusicButton.transform.parent.gameObject.SetActive(false);
                autoUpdatePlaylistToggle.isOn = true;
                spc.pageParents[0].gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES);
                mac.currentMenuState = MainAppController.MenuState.optionsMenu;
            }
        }
        catch (XmlException)
        {
            mac.ShowErrorMessage("Loading failed: Malformed save file format", 0);
        }
        if (MainAppController.darkModeEnabled)
        {
            mac.EnableDarkMode(true);
        }
        saveNameField.text = currentSaveName;
        StartCoroutine(RebuildLayout());
        CloseOptionsMenu();
    }

    private IEnumerator RebuildLayout()
    {
        yield return null;
        pt.TabClicked(0);
        spc.ChangeSFXPage(0);
    }

    private bool DestroyItems()
    {
        foreach (MusicButton mb in pt.mainTab.MusicButtons)
        {
            Destroy(mb.gameObject);
        }
        pt.mainTab.MusicButtons.Clear();
        mc.ClearPlayNextList();
        mc.ClearPlaylistSearch();
        LoadedFilesData.songs.Clear();
        return true;
    }

    private void Save(string fileName, bool overwrite = false)
    {
        saveFileName = fileName;
        List<string> presentSaves = new();
        try
        {
            presentSaves = Directory.GetFiles(MainAppController.workingDirectories["saveDirectory"]).ToList();
        }
        catch (DirectoryNotFoundException)
        {
            mac.SetupFolderStructure();
        }
        presentSaves = presentSaves.Select(save => save.ToLower()).ToList();    //check for files with same name but different capitalizations
        if (((!presentSaves.Contains(Path.Combine(MainAppController.workingDirectories["saveDirectory"], fileName + ".xml").ToLower())) && !(Application.platform == RuntimePlatform.LinuxPlayer)) || overwrite)    //linux is case sensitive
        {
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(MainAppController.workingDirectories["saveDirectory"], fileName + ".xml")))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement(MainAppController.appDirectoryName + "-Save-File");
                {
                    writer.WriteElementString("version", mac.VERSION);
                    writer.WriteElementString("masterVolume", Camera.main.GetComponent<VolumeController>().MasterVolume.ToString("N1"));
                    writer.WriteElementString("musicVolume", mc.MusicVolume.ToString("N3"));
                    writer.WriteElementString("shuffle", mc.Shuffle.ToString());
                    writer.WriteElementString("crossfade", mc.Crossfade.ToString());

                    writer.WriteStartElement("watchFolders");
                    {
                        foreach(string s in sfc.searchFolders)
                        {
                            writer.WriteElementString("path", s);
                        }
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("SFX-Buttons");
                    {
                        for (int i = 0; i < MainAppController.NUMPAGES; i++)
                        {
                            writer.WriteStartElement("page");
                            writer.WriteElementString("id", i.ToString());
                            writer.WriteElementString("label", spc.pageButtons[i].GetComponent<PageButton>().Label);
                            {
                                for (int j = 0; j < MainAppController.NUMBUTTONS; j++)
                                {
                                    SFXButton button = spc.pageParents[i].buttons[j].GetComponent<SFXButton>();
                                    if (!string.IsNullOrEmpty(button.FileName))
                                    {
                                        writer.WriteStartElement("button");
                                        {
                                            writer.WriteElementString("id", button.id.ToString());
                                            writer.WriteElementString("label", button.Label);
                                            writer.WriteElementString("clipPath", button.FileName);
                                            writer.WriteElementString("localVolume", button.LocalVolume.ToString("N2"));
                                            writer.WriteElementString("loop", button.LoopEnabled.ToString());
                                            writer.WriteElementString("minLoopDelay", button.MinLoopDelay.ToString("N0"));
                                            writer.WriteElementString("randomizeLoopDelay", button.RandomizeLoopDelay.ToString());
                                            writer.WriteElementString("maxLoopDelay", button.MaxLoopDelay.ToString("N0"));
                                            writer.WriteElementString("minFadeVolume", button.minimumFadeVolume.ToString("N2"));
                                            writer.WriteElementString("maxFadeVolume", button.maximumFadeVolume.ToString("N2"));
                                            writer.WriteElementString("ignoreOnPlayAll", button.IgnorePlayAll.ToString());
                                            if (button.ButtonEdgeColor != Color.white)   // ignore buttons with default color (white)
                                            {
                                                writer.WriteStartElement("edgeColor");
                                                {
                                                    writer.WriteElementString("r", button.ButtonEdgeColor.r.ToString("N3"));
                                                    writer.WriteElementString("g", button.ButtonEdgeColor.g.ToString("N3"));
                                                    writer.WriteElementString("b", button.ButtonEdgeColor.b.ToString("N3"));
                                                }
                                            }
                                        }
                                        writer.WriteEndElement();
                                    }
                                }
                            }
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("playlist");
                    {
                        foreach (PlaylistTab pt in PlaylistTabs.tabs)
                        {
                            writer.WriteStartElement("tab");
                            writer.WriteElementString("label", pt.LabelText);
                            foreach (MusicButton mb in pt.MusicButtons)
                            {
                                writer.WriteElementString("song", mb.Song.FileName);
                            }
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("removed");
                    {
                        print(LoadedFilesData.DeletedMusicClips.Count);
                        foreach(string s in LoadedFilesData.DeletedMusicClips)
                        {
                                writer.WriteElementString("a", s);
                        }
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndDocument();
            }
            saveNamePanel.SetActive(false);
            optionsPanel.SetActive(false);
            mac.currentMenuState = MainAppController.MenuState.mainAppView;
            currentSaveName = fileName;
        }
        else
        {
            mac.currentMenuState = MainAppController.MenuState.overwriteSaveFile;
            overwriteSavePanel.SetActive(true);
        }
    }

    internal void OptionMenuButtonClicked(string id, bool state = false)
    {
        switch (id)
        {
            case "shortcuts":
                GetComponent<ShortcutViewController>().ShowShortcuts();
                break;
            case "close":
                CloseOptionsMenu();
                break;
            case "load":
                Load();
                break;
            case "save":
                OpenSaveNamePanel();
                break;
            case "open about":
                OpenAboutMenu();
                break;
            case "close about":
                CloseAboutMenu();
                break;
            case "close load selection":
                CloseLoadSelection();
                break;
            case "accept save name":
                AcceptSaveName();
                break;
            case "close save menu":
                CloseSaveMenu();
                break;
            case "confirm overwrite":
                ConfirmOverwriteSave();
                break;
            case "cancel overwrite":
                CancelOverwriteSave();
                break;
            case "confirm new file":
                ConfirmNewFile();
                break;
            case "cancel new file":
                CancelNewFile();
                break;
            case "new file":
                StartNewFile();
                break;
            case "EnableDiscoMode":
                mac.ToggleDiscoMode();
                break;
            case "EnableDynamicPlaylistBackgrounds":
                if (state)
                {
                    gmfb.Begin();
                }
                else
                {
                    gmfb.StopGeneration();
                }

                break;
            case "show advanced options":
                OpenAdvancedOptionsMenu();
                break;
            case "close advanced options":
                CloseAdvancedOptionsMenu();
                break;
            case "show search folders":
                ShowSearchFolders();
                break;
            case "reset removed songs":
                ResetRemovedSongs();
                break;
            case "close secret settings":
                CloseSecretSettings();
                break;
            case "assignRemoteButtons":
                AssignRemoteButtons();
                break;
            default:
                print("No action for button " + id);
                break;
        }
    }

    private void AssignRemoteButtons()
    {
        
    }

    private void ResetRemovedSongs()
    {
       print("resetting");
       LoadedFilesData.DeletedMusicClips.Clear();
    }

    private void ShowSearchFolders()
    {
        CloseOptionsMenu();
        InitialPlaylistAddMusicButton.transform.parent.gameObject.SetActive(false);
        GetComponent<ShortcutViewController>().CloseShortcuts();
        GetComponent<SearchFoldersController>().ShowSearchFolderMenu();
        
    }
}

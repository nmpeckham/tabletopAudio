using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Xml;
using TMPro;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

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

    public TMP_Text saveErrorText;

    public Toggle darkModeToggle;

    public GameObject overwriteSavePanel;

    private string saveFileName;

    private char[] bannedCharacters =
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

    // Start is called before the first frame update
    void Start()
    {
        if (!Application.isEditor) optionsPanel.SetActive(false);
        saveErrorText.enabled = false;
        mc = GetComponent<MusicController>();
        mac = GetComponent<MainAppController>();
        darkModeToggle.onValueChanged.AddListener(DarkModeChanged);
        autoUpdatePlaylistToggle.onValueChanged.AddListener(AutoUpdateChanged);
        crossfadeField.onValueChanged.AddListener(CrossfadeTimeChanged);
        darkModeToggle.SetIsOnWithoutNotify(mac.darkModeEnabled);
        float val = PlayerPrefs.GetFloat("Crossfade");
        if (val == 0) val = 10;
        mc.CrossfadeTime = val;
        crossfadeField.text = val.ToString();

        dm = GetComponent<DiscoMode>();
        gmfb = GetComponent<GenerateMusicFFTBackgrounds>();
        pt = GetComponent<PlaylistTabs>();
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
        switch(id)
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

    void StartNewFile()
    {
        confirmNewFilePanel.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.startNewFile;
    }

    void ConfirmNewFile()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    internal void CancelNewFile()
    {
        confirmNewFilePanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    void ConfirmOverwriteSave()
    {
        Save(saveFileName, true);
        overwriteSavePanel.SetActive(false);
    }

    internal void CancelOverwriteSave()
    {
        overwriteSavePanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.enterSaveFileName;
    }

    void DarkModeChanged(bool value)
    {
        mac.SwapDarkLightMode(value);
    }

    void CrossfadeTimeChanged(string value)
    {
        try
        {
            float val = 0;
            if (hadFormatException)
            {
                // will remove the default 1 that is inputed after an error, when the user types a new value. I think.
                if (value[value.Length - 1] == '1') val = Convert.ToSingle(value.Remove(value.Length - 1, 1));
                hadFormatException = false;
            }

            else val = Mathf.Max(1, Math.Min(30, Convert.ToSingle(value)));
            mc.CrossfadeTime = val;
            crossfadeField.text = val.ToString();
            PlayerPrefs.SetFloat("Crossfade", val);
        }
        catch(FormatException)
        {
            crossfadeField.text = "0";
            mc.CrossfadeTime = 0;
            hadFormatException = true;
        }
    }

    void OpenSaveNamePanel()
    {
        saveNamePanel.SetActive(true);
        saveNameField.ActivateInputField();
        mac.currentMenuState = MainAppController.MenuState.enterSaveFileName;
    }

    internal void AcceptSaveName()
    {
        bool errorFound = false;
        string text = saveNameField.text;
        foreach (char item in bannedCharacters)
        {
            if (text.Contains(item))
            {
                errorFound = true;
                saveErrorText.enabled = true;
                break;
            }
        }

        if(!errorFound)
        {
            saveErrorText.enabled = false;
            Save(saveNameField.text);
        }
    }

    internal void CloseSaveMenu()
    {
        saveNamePanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    internal void AutoUpdateChanged(bool val)
    {
        if (autoUpdatePlaylistToggle.isOn != val) autoUpdatePlaylistToggle.isOn = val;
        if(mc.AutoCheckForNewFiles != val) mc.AutoCheckForNewFiles = val;
    }

    internal void CloseLoadSelection()
    {
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
        loadGameSelectionView.SetActive(false);
    }

    void OpenAboutMenu()
    {
        versionText.text = "TableTop Audio v" + mac.VERSION;
        aboutMenu.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.aboutMenu;
    }

    internal void CloseAboutMenu()
    {
        aboutMenu.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    internal void Close()
    {
        optionsPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    void Load()
    {
        mac.currentMenuState = MainAppController.MenuState.selectFileToLoad;
        foreach (Transform t in loadGameScrollView.GetComponentsInChildren<Transform>())
        {
            if(t.gameObject.name != "loadGameScrollView") Destroy(t.gameObject);
        }

        loadGameSelectionView.SetActive(true);
        
        foreach (string saveName in Directory.GetFiles(mac.saveDirectory))
        {
            if(Path.GetExtension(saveName) == ".xml" || Path.GetExtension(saveName) == ".xml")
            {
                print(Path.GetDirectoryName(mac.saveDirectory));
                string trimmedSaveName = saveName.Replace(mac.saveDirectory, "");
                GameObject scrollItem = Instantiate(Prefabs.loadGameItemPrefab, loadGameScrollView.transform);
                scrollItem.GetComponent<LoadGameSelectItem>().fileLocation = saveName;
                scrollItem.GetComponentInChildren<TMP_Text>().SetText(trimmedSaveName);
            }
        }
    }

    internal void LoadItemSelected(string fileLocation)
    {
        mc.AutoCheckForNewFiles = false;
        autoUpdatePlaylistToggle.isOn = false;         
        mc.Stop();
        mac.ControlButtonClicked("STOP-SFX");
        DestroyItems();
        mac.MakeSFXButtons();
        pt.TabClicked(0);
        pt.DeleteAllTabs();


        XmlDocument file = new XmlDocument();
        try
        {
            file.Load(fileLocation);
            string version = Convert.ToString(file.SelectSingleNode("/TableTopAudio-Save-File/version").InnerText);
            if (true)//version == MainAppController.VERSION) //No version checking yet
            {
                LoadedFilesData.songs.Clear();
                LoadedFilesData.sfxClips.Clear();
                LoadedFilesData.deletedMusicClips.Clear();

                float masterVolume = Convert.ToSingle(file.SelectSingleNode("/TableTopAudio-Save-File/masterVolume").InnerText);
                float musicVolume = Convert.ToSingle(file.SelectSingleNode("/TableTopAudio-Save-File/musicVolume").InnerText);
                bool shuffle = Convert.ToBoolean(file.SelectSingleNode("/TableTopAudio-Save-File/shuffle").InnerText);
                mc.MasterVolume = masterVolume;
                mc.MusicVolume = musicVolume;

                XmlNodeList pages = file.SelectNodes("/TableTopAudio-Save-File/SFX-Buttons/page");

                foreach (XmlNode p in pages)
                {
                    int page = Convert.ToInt32(p.SelectSingleNode("id").InnerText);
                    string pageLabel = page.ToString();
                    try
                    {
                        pageLabel = p.SelectSingleNode("label").InnerText;
                    }
                    catch (NullReferenceException) { }

                    mac.pageButtons[page].GetComponent<PageButton>().Label = pageLabel;
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
                                mac.ShowErrorMessage("Could not load save file. Error finding clip path");
                            }
                        }
                        clipPath = clipPath.Replace(Path.DirectorySeparatorChar.ToString(), "");
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

                        SFXButton sfxBtn = mac.sfxButtons[page][id].GetComponent<SFXButton>();
                        sfxBtn.Label = label;
                        sfxBtn.id = id;
                        sfxBtn.FileName = clipPath;
                        sfxBtn.LocalVolume = localVolume;
                        sfxBtn.ChangeMasterVolume(masterVolume);
                        sfxBtn.Loop = loop;
                        sfxBtn.RandomizeLoopDelay = randomizeLoopTime;
                        sfxBtn.MinLoopDelay = minLoopDelay;
                        sfxBtn.MaxLoopDelay = maxLoopDelay;
                        sfxBtn.minimumFadeVolume = minFadeVolume;
                        sfxBtn.maximumFadeVolume = maxFadeVolume;
                        sfxBtn.IgnorePlayAll = ignoreOnPlayAll;

                    }
                }

                int tabId = 0;
                foreach (XmlNode n in file.SelectNodes("/TableTopAudio-Save-File/playlist/tab"))
                {
                    string label = n.SelectSingleNode("label").InnerText.ToString();
                    if (label != "*")
                    {
                        pt.TabClicked(-1);
                        PlaylistTabs.tabs.Last().LabelText = label;
                    }
                    List<string> files = new List<string>();
                    foreach (XmlNode s in n)
                    {
                        if(s.Name == "song") files.Add(s.InnerText);
                    }
                    print("Label: " + label);
                    print("# tabs: " + PlaylistTabs.tabs.Count);
                    mc.InitLoadFiles(files, tabId);
                    tabId++;
                }                
                loadGameSelectionView.SetActive(false);
                mac.pageParents[0].gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES);
                mac.currentMenuState = MainAppController.MenuState.optionsMenu;
            }
        }
        catch(XmlException)
        {
            mac.ShowErrorMessage("Loading failed: Malformed save file format");
        }

        if (mac.darkModeEnabled) mac.SwapDarkLightMode(true);
        StartCoroutine(RebuildLayout());
    }

    IEnumerator RebuildLayout()
    {
        yield return null;
        pt.TabClicked(0);
        mac.ChangeSFXPage(0);
    }

    bool DestroyItems()
    {
        foreach(MusicButton mb in pt.mainTab.MusicButtons)
        {
            Destroy(mb.gameObject);
        }
        pt.mainTab.MusicButtons.Clear();
        mc.ClearPlayNextList();
        mc.ClearPlaylistSearch();
        LoadedFilesData.songs.Clear();
        return true;
    }

    void Save(string filename, bool overwrite=false)
    {
        saveFileName = filename;
        if ((!System.IO.Directory.GetFiles(mac.saveDirectory).Contains(Path.Combine(mac.saveDirectory, filename +".xml"))) || overwrite)
        {
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(mac.saveDirectory, filename + ".xml")))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("TableTopAudio-Save-File");
                {
                    writer.WriteElementString("version", mac.VERSION);
                    writer.WriteElementString("masterVolume", mc.MasterVolume.ToString("N1"));
                    writer.WriteElementString("musicVolume", mc.MusicVolume.ToString("N1"));
                    writer.WriteElementString("shuffle", mc.Shuffle.ToString());

                    writer.WriteStartElement("SFX-Buttons");
                    {
                        for (int i = 0; i < MainAppController.NUMPAGES; i++)
                        {
                            writer.WriteStartElement("page");
                            writer.WriteElementString("id", i.ToString());
                            writer.WriteElementString("label", mac.pageButtons[i].GetComponent<PageButton>().Label);
                            {
                                for (int j = 0; j < MainAppController.NUMBUTTONS; j++)
                                {
                                    SFXButton button = mac.sfxButtons[i][j].GetComponent<SFXButton>();
                                    if (!(string.IsNullOrEmpty(button.Label)))
                                    {
                                        writer.WriteStartElement("button");
                                        {
                                            writer.WriteElementString("id", button.id.ToString());
                                            writer.WriteElementString("label", button.Label);
                                            writer.WriteElementString("clipPath", button.FileName.Replace(mac.sfxDirectory, ""));
                                            writer.WriteElementString("localVolume", button.LocalVolume.ToString("N2"));
                                            writer.WriteElementString("loop", button.Loop.ToString());
                                            writer.WriteElementString("minLoopDelay", button.MinLoopDelay.ToString("N0"));
                                            writer.WriteElementString("randomizeLoopDelay", button.RandomizeLoopDelay.ToString());
                                            writer.WriteElementString("maxLoopDelay", button.MaxLoopDelay.ToString("N0"));
                                            writer.WriteElementString("minFadeVolume", button.minimumFadeVolume.ToString("N2"));
                                            writer.WriteElementString("maxFadeVolume", button.maximumFadeVolume.ToString("N2"));
                                            writer.WriteElementString("ignoreOnPlayAll", button.IgnorePlayAll.ToString());
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
                    foreach(PlaylistTab pt in PlaylistTabs.tabs)
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
            }
            writer.WriteEndDocument();
            }
            saveNamePanel.SetActive(false);
            optionsPanel.SetActive(false);
            mac.currentMenuState = MainAppController.MenuState.mainAppView;
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
            case "close":
                Close();
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
                if (state) gmfb.Begin();
                else gmfb.StopGeneration();
                break;
            case "show advanced options":
                OpenAdvancedOptionsMenu();
                break;
            case "close advanced options":
                CloseAdvancedOptionsMenu();
                break;
            default:
                print("No action for button " + id);
                break;
        }
    }
}

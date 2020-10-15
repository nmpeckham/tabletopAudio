﻿using System.Collections;
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
    public Button load;
    public Button save;
    public Button close;
    public Button about;
    public Button closeAbout;
    public Button closeLoadSelection;
    public Button newFileButton;

    public GameObject confirmNewFilePanel;
    public Button confirmNewFileButton;
    public Button cancelNewFileButton;

    public Button acceptSaveName;
    public Button closeSaveNameMenu;
    public TMP_InputField saveNameField;
    public GameObject saveNamePanel;

    public Toggle autoUpdatePlaylistToggle;

    public GameObject optionsPanel;

    public GameObject aboutMenu;
    public TMP_Text versionText;

    public GameObject loadGameSelectionView;
    public GameObject loadGameScrollView;
    public GameObject loadGameItemPrefab;

    public TMP_InputField crossfadeField;
    private bool hadFormatException = false;

    private static MainAppController mac;
    private static MusicController mc;

    public TMP_Text saveErrorText;

    public Toggle darkModeToggle;

    public GameObject overwriteSavePanel;
    public Button cancelOverwriteSavePanel;
    public Button confirmOverwriteSavePanel;

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

    // Start is called before the first frame update
    void Start()
    {
        saveErrorText.enabled = false;
        mc = GetComponent<MusicController>();
        mac = GetComponent<MainAppController>();
        darkModeToggle.onValueChanged.AddListener(DarkModeChanged);
        autoUpdatePlaylistToggle.onValueChanged.AddListener(AutoUpdateChanged);
        close.onClick.AddListener(Close);
        load.onClick.AddListener(Load);
        save.onClick.AddListener(OpenSaveNamePanel);
        about.onClick.AddListener(OpenAboutMenu);
        closeAbout.onClick.AddListener(CloseAboutMenu);
        closeLoadSelection.onClick.AddListener(CloseLoadSelection);
        acceptSaveName.onClick.AddListener(AcceptSaveName);
        closeSaveNameMenu.onClick.AddListener(CloseSaveMenu);
        crossfadeField.onValueChanged.AddListener(CrossfadeTimeChanged);
        confirmOverwriteSavePanel.onClick.AddListener(ConfirmOverwriteSave);
        cancelOverwriteSavePanel.onClick.AddListener(CancelOverwriteSave);
        confirmNewFileButton.onClick.AddListener(ConfirmNewFile);
        cancelNewFileButton.onClick.AddListener(CancelNewFile);
        newFileButton.onClick.AddListener(StartNewFile);
        darkModeToggle.SetIsOnWithoutNotify(mac.darkModeEnabled);
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
        mac.swapDarkLightMode(value);
    }

    void CrossfadeTimeChanged(string value)
    {
        try
        {
            float val = 0;
            if (hadFormatException)
            {
                if (value[value.Length - 1] == '0') val = Convert.ToSingle(value.Remove(value.Length - 1, 1));
                hadFormatException = false;
            }

            else val = Math.Min(30, Convert.ToSingle(value));
            mc.CrossfadeTime = val;
            crossfadeField.text = val.ToString();
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
            if(Path.GetExtension(saveName) == ".xml" || Path.GetExtension(saveName) == ".txt")
            {
                string trimmedSaveName = saveName.Replace(mac.saveDirectory + mac.sep, "");
                GameObject scrollItem = Instantiate(loadGameItemPrefab, loadGameScrollView.transform);
                scrollItem.GetComponent<LoadGameSelectItem>().fileLocation = saveName;
                scrollItem.GetComponentInChildren<TMP_Text>().SetText(trimmedSaveName);
            }
        }
    }

    internal IEnumerator LoadItemSelected(string fileLocation)
    {
        mc.AutoCheckForNewFiles = false;
        autoUpdatePlaylistToggle.isOn = false;         
        mc.Stop();
        yield return new WaitUntil(() => mac.ControlButtonClicked("STOP-SFX"));
        yield return new WaitUntil(() => DestroyItems());
        yield return new WaitUntil(() => mac.MakeSFXButtons());
        XmlDocument file = new XmlDocument();
        try
        {
            file.Load(fileLocation);
            string version = Convert.ToString(file.SelectSingleNode("/TableTopAudio-Save-File/version").InnerText);
            if (true)//version == MainAppController.VERSION) //No version checking yet
            {
                LoadedFilesData.musicClips.Clear();
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
                List<string> files = new List<string>();
                string[] HDfiles = System.IO.Directory.GetFiles(mac.musicDirectory);
                foreach (XmlNode n in file.SelectNodes("/TableTopAudio-Save-File/SFX-Buttons/playlist/song"))
                {
                    if (HDfiles.Contains(Path.Combine(mac.musicDirectory, n.InnerText.Replace(Path.DirectorySeparatorChar + "music" + Path.DirectorySeparatorChar, ""))))
                    {
                        files.Add(n.InnerText.Replace(Path.DirectorySeparatorChar + "music" + Path.DirectorySeparatorChar, ""));
                    }
                    else
                    {
                        mac.ShowErrorMessage("Could not find file " + Path.Combine(n.InnerText));
                    }

                }

                foreach (XmlNode n in file.SelectNodes("/TableTopAudio-Save-File/playlist/song"))
                {
                    if (HDfiles.Contains(Path.Combine(mac.musicDirectory, n.InnerText.Replace(Path.DirectorySeparatorChar + "music" + Path.DirectorySeparatorChar, ""))))
                    {
                        files.Add(n.InnerText.Replace(Path.DirectorySeparatorChar + "music" + Path.DirectorySeparatorChar, ""));
                    }
                    else
                    {
                        mac.ShowErrorMessage("Could not find file " + Path.Combine(n.InnerText));
                    }

                }
                mc.InitLoadFiles(files);
                loadGameSelectionView.SetActive(false);
                mac.pageParents[0].gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES);
                mac.currentMenuState = MainAppController.MenuState.optionsMenu;
            }
        }
        catch(XmlException)
        {
            mac.ShowErrorMessage("Loading failed: Malformed save file format");
        }

        if (mac.darkModeEnabled) mac.swapDarkLightMode(true);
        yield return null;
        //else
        //{
        //    mc.AutoCheckForNewFiles = true;
        //    autoUpdatePlaylistToggle.isOn = true;
        //    mac.ShowErrorMessage("This save file was saved with a different version of TableTopAudio");
        //    yield return null;
        //}

    }

    bool DestroyItems()
    {
        foreach (List<GameObject> page in mac.sfxButtons)
        {
            foreach (GameObject button in page)
            {
                Destroy(button);
            }
        }

        foreach(Transform songItem in mc.musicScrollView.gameObject.GetComponentsInChildren<Transform>())
        {
            if(songItem.gameObject.name != "Content") Destroy(songItem.gameObject);
        }

        mc.musicButtons.Clear();
        LoadedFilesData.musicClips.Clear();
        return true;
    }

    void Save(string filename, bool overwrite=false)
    {
        saveFileName = filename;
        if ((!System.IO.Directory.GetFiles(mac.saveDirectory).Contains(Path.Combine(mac.saveDirectory, filename +".txt"))) || overwrite)
        {
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(mac.saveDirectory, filename + ".txt")))
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
                    foreach (MusicButton mb in mc.musicScrollView.GetComponentsInChildren<MusicButton>())
                    {
                        writer.WriteElementString("song", mb.FileName);
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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Xml;
using TMPro;
using System;
using System.Linq;

//Controls the options menu
public class OptionsMenuController : MonoBehaviour
{
    public Button load;
    public Button save;
    public Button close;
    public Button about;
    public Button closeAbout;
    public Button closeLoadSelection;

    public Button acceptSaveName;
    public Button closeSaveNameMenu;
    public TMP_InputField saveNameField;
    public GameObject saveNamePanel;

    public Toggle autoUpdatePlaylistToggle;

    public GameObject optionsPanel;
    public GameObject aboutMenu;
    public GameObject loadGameSelectionView;
    public GameObject loadGameScrollView;
    public GameObject loadGameItemPrefab;
    private static MainAppController mac;
    private static MusicController mc;


    // Start is called before the first frame update
    void Start()
    {
        mc = GetComponent<MusicController>();
        mac = GetComponent<MainAppController>();
        autoUpdatePlaylistToggle.onValueChanged.AddListener(AutoUpdateChanged);
        close.onClick.AddListener(Close);
        load.onClick.AddListener(Load);
        save.onClick.AddListener(OpenSaveNamePanel);
        about.onClick.AddListener(OpenAboutMenu);
        closeAbout.onClick.AddListener(CloseAboutMenu);
        closeLoadSelection.onClick.AddListener(CloseLoadSelection);
        acceptSaveName.onClick.AddListener(AcceptSaveName);
        closeSaveNameMenu.onClick.AddListener(CloseSaveMenu);
    }

    void OpenSaveNamePanel()
    {
        saveNamePanel.SetActive(true);
    }

    void AcceptSaveName()
    {
        Save(saveNameField.text);
        saveNamePanel.SetActive(false);
    }

    void CloseSaveMenu()
    {
        saveNamePanel.SetActive(false);
    }

    void AutoUpdateChanged(bool val)
    {
        if (autoUpdatePlaylistToggle.isOn != val) autoUpdatePlaylistToggle.isOn = val;
        if(mc.AutoCheckForNewFiles != val) mc.AutoCheckForNewFiles = val;
    }

    void CloseLoadSelection()
    {
        loadGameSelectionView.SetActive(false);
    }

    void OpenAboutMenu()
    {
        aboutMenu.SetActive(true);
    }

    void CloseAboutMenu()
    {
        aboutMenu.SetActive(false);
    }

    void Close()
    {
        optionsPanel.SetActive(false);
    }

    void Load()
    {
        foreach (Transform t in loadGameScrollView.GetComponentsInChildren<Transform>())
        {
            if(t.gameObject.name != "loadGameScrollView") Destroy(t.gameObject);
        }

        loadGameSelectionView.SetActive(true);
        
        foreach (string saveName in Directory.GetFiles(mac.saveDirectory))
        {
            string trimmedSaveName = saveName.Replace(mac.saveDirectory + mac.sep, "");
            GameObject scrollItem = Instantiate(loadGameItemPrefab, loadGameScrollView.transform);
            scrollItem.GetComponent<LoadGameSelectItem>().fileLocation = saveName;
            scrollItem.GetComponentInChildren<TMP_Text>().SetText(trimmedSaveName);
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
        file.Load(fileLocation);

        float masterVolume = Convert.ToSingle(file.SelectSingleNode("/TableTopAudio-Save-File/masterVolume").InnerText);
        float musicVolume = Convert.ToSingle(file.SelectSingleNode("/TableTopAudio-Save-File/musicVolume").InnerText);
        bool shuffle = Convert.ToBoolean(file.SelectSingleNode("/TableTopAudio-Save-File/shuffle").InnerText);

        mc.MasterVolume = masterVolume;
        mc.MusicVolume = musicVolume;

        XmlNodeList pages = file.SelectNodes("/TableTopAudio-Save-File/SFX-Buttons/page");
        foreach (XmlNode p in pages)
        {
            int page = Convert.ToInt32(p.SelectSingleNode("id").InnerText);
            mac.SFXButtons.Add(new List<GameObject>());
            foreach (XmlNode b in p.SelectNodes("button"))
            {
                //Debug.Log(b.SelectSingleNode("label").InnerText);
                string label = b.SelectSingleNode("label").InnerText;
                int id = Convert.ToInt32(b.SelectSingleNode("id").InnerText);
                string clipID = b.SelectSingleNode("clipID").InnerText;
                float localVolume = Convert.ToSingle(b.SelectSingleNode("localVolume").InnerText);
                //Debug.Log(b.SelectSingleNode("localVolume").InnerText);
                bool loop = Convert.ToBoolean(b.SelectSingleNode("loop").InnerText);
                bool randomizeLoopTime = Convert.ToBoolean(b.SelectSingleNode("randomizeLoopDelay").InnerText);
                float minLoopDelay = Convert.ToSingle(b.SelectSingleNode("minLoopDelay").InnerText);
                float maxLoopDelay = Convert.ToSingle(b.SelectSingleNode("maxLoopDelay").InnerText);

                SFXButton sfxBtn = mac.SFXButtons[page][id].GetComponent<SFXButton>();
                sfxBtn.Label = label;
                sfxBtn.id = id;
                sfxBtn.clipID = clipID;
                //Debug.Log(clipID);
                sfxBtn.LocalVolume = localVolume;
                sfxBtn.Loop = loop;
                sfxBtn.RandomizeLoopDelay = randomizeLoopTime;
                sfxBtn.MinLoopDelay = minLoopDelay;
                sfxBtn.MaxLoopDelay = maxLoopDelay;

            }
        }
        List<string> files = new List<string>();
        string[] HDfiles = System.IO.Directory.GetFiles(mac.musicDirectory);
        foreach (XmlNode n in file.SelectNodes("/TableTopAudio-Save-File/SFX-Buttons/playlist/song"))
        {
            if(HDfiles.Contains(n.InnerText)) {
                files.Add(n.InnerText);
            }
            else
            {
                mac.ShowErrorMessage("Could not find file " + n.InnerText.Replace(mac.musicDirectory + mac.sep, ""));
            }
            
        }
        mc.InitLoadFiles(files);
        loadGameSelectionView.SetActive(false);
        yield return null;
    }

    bool DestroyItems()
    {
        foreach (List<GameObject> page in mac.SFXButtons)
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

    void Save(string filename)
    {
        //TODO: Add option to overwrite existing save file
        if(!System.IO.Directory.GetFiles(mac.saveDirectory).Contains(Path.Combine(mac.saveDirectory, filename +".txt")))
        {
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(mac.saveDirectory, filename + ".txt")))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("TableTopAudio-Save-File");
                {
                    writer.WriteElementString("version", "v0.1");
                    writer.WriteElementString("masterVolume", mc.MasterVolume.ToString("N1"));
                    writer.WriteElementString("musicVolume", mc.MusicVolume.ToString("N1"));
                    writer.WriteElementString("shuffle", mc.Shuffle.ToString());

                    writer.WriteStartElement("SFX-Buttons");
                    {
                        for (int i = 0; i < MainAppController.NUMPAGES; i++)
                        {
                            writer.WriteStartElement("page");
                            writer.WriteElementString("id", i.ToString());
                            {
                                for (int j = 0; j < MainAppController.NUMBUTTONS; j++)
                                {
                                    SFXButton button = mac.SFXButtons[i][j].GetComponent<SFXButton>();
                                    if (!(string.IsNullOrEmpty(button.Label)))
                                    {
                                        writer.WriteStartElement("button");
                                        {
                                            writer.WriteElementString("id", button.id.ToString());
                                            writer.WriteElementString("label", button.Label);
                                            writer.WriteElementString("clipID", button.clipID);
                                            writer.WriteElementString("localVolume", button.LocalVolume.ToString("N1"));
                                            writer.WriteElementString("loop", button.Loop.ToString());
                                            writer.WriteElementString("minLoopDelay", button.MinLoopDelay.ToString("N0"));
                                            writer.WriteElementString("randomizeLoopDelay", button.RandomizeLoopDelay.ToString());
                                            writer.WriteElementString("maxLoopDelay", button.MaxLoopDelay.ToString("N0"));
                                        }
                                        writer.WriteEndElement();
                                    }
                                }
                            }
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteStartElement("playlist");
                    {
                        //Debug.Log(mc.musicScrollView.name);
                        foreach (MusicButton mb in mc.musicScrollView.GetComponentsInChildren<MusicButton>())
                        {
                            writer.WriteElementString("song", mb.file);
                        }
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }
        else
        {
            mac.ShowErrorMessage("Save with that name already exists");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

//Class for editing Sound effect buttons
public class ButtonEditorController : MonoBehaviour
{
    private int buttonID;
    public TMP_InputField buttonLabelInput;
    public Button applyButton;
    public Button cancelButton;
    public Button changeFileButton;

    public Toggle loopButton;
    public Toggle randomizeLoopButton;
    private MainAppController mac;
    public GameObject editButtonPanel;
    private FileSelectViewController fsvc;
    public TMP_Text fileNameLabel;
    public TMP_InputField minLoopDelay;
    public TMP_InputField maxLoopDelay;

    public Slider minimumVolumeSlider;
    public Slider maximumVolumeSlider;

    public TMP_Text minVolumeLabel;
    public TMP_Text maxVolumeLabel;

    public GameObject randomizeLoopPanel;
    public GameObject minLoopTimePanel;
    public GameObject maxLoopTimePanel;

    public TMP_Text minLoopDelayLabel;

    internal AudioClip newClip;
    internal string clipID = null;

    public Button clearFileButton;
    // Start is called before the first frame update
    void Start()
    {
        //Assign listeners
        newClip = null;
        applyButton.onClick.AddListener(ApplySettings);
        cancelButton.onClick.AddListener(CancelEditing);
        changeFileButton.onClick.AddListener(ChangeFile);
        clearFileButton.onClick.AddListener(ClearFile);
        mac = Camera.main.GetComponent<MainAppController>();
        fsvc = Camera.main.GetComponent<FileSelectViewController>();
        loopButton.onValueChanged.AddListener(LoopChanged);
        randomizeLoopButton.onValueChanged.AddListener(RandomizeChanged);

        minimumVolumeSlider.onValueChanged.AddListener(MinVolumeChanged);
        maximumVolumeSlider.onValueChanged.AddListener(maxVolumeChanged);
    }

    void MinVolumeChanged(float val)
    {
        if (val < maximumVolumeSlider.value) minVolumeLabel.text = (val).ToString("N0") + "%";
        else
        {
            minimumVolumeSlider.SetValueWithoutNotify(maximumVolumeSlider.value - 1);
            minVolumeLabel.text = (maximumVolumeSlider.value - 1).ToString("N0") + "%";
        }
    }

    void maxVolumeChanged(float val)
    {
        if (val > minimumVolumeSlider.value) maxVolumeLabel.text = (val).ToString("N0") + "%";
        else
        {
            maximumVolumeSlider.SetValueWithoutNotify(minimumVolumeSlider.value + 1);
            maxVolumeLabel.text = (minimumVolumeSlider.value + 1).ToString("N0") + "%";
        }
    }

    //Called when "Randomize Loop Delay" is changed
    void RandomizeChanged(bool val)
    {
        if (val)
        {
            maxLoopTimePanel.SetActive(true);
            minLoopDelayLabel.text = "Min Loop Delay (sec):";
        }
        else
        {
            maxLoopTimePanel.SetActive(false);
            minLoopDelayLabel.text = "Loop Delay (sec):";
        }
    }

    // Called when "loop" is changed
    void LoopChanged(bool val)
    {
        if(val)
        {
            randomizeLoopPanel.SetActive(true);
            minLoopTimePanel.SetActive(true);
            if (randomizeLoopButton.isOn)
            {
                maxLoopTimePanel.SetActive(true);
            }
        }
        else
        {
            randomizeLoopPanel.SetActive(false);
            minLoopTimePanel.SetActive(false);
            maxLoopTimePanel.SetActive(false);
        }
    }

    void ApplySettings()
    {
        //Applies all changed settings
        SFXButton button = mac.sfxButtons[mac.activePage][buttonID].GetComponent<SFXButton>();
        if (System.String.IsNullOrEmpty(fileNameLabel.text))    //No file selected
        {
            button.Stop();
            button.ClearActiveClip();
        }
        if((clipID != button.FileName && !String.IsNullOrEmpty(clipID)) || String.IsNullOrEmpty(button.FileName))     //Clip selected, different from current
        {
            button.FileName = clipID;
        }
        button.Loop = loopButton.isOn;
        button.RandomizeLoopDelay = randomizeLoopButton.isOn;
        button.MinLoopDelay = Convert.ToInt32(minLoopDelay.text);
        button.MaxLoopDelay = Convert.ToInt32(maxLoopDelay.text);
        button.minimumFadeVolume = minimumVolumeSlider.value / 100;
        button.maximumFadeVolume = maximumVolumeSlider.value / 100;

        if (!String.IsNullOrEmpty(minLoopDelay.text)) button.MinLoopDelay = Convert.ToInt32(minLoopDelay.text);
        else button.MinLoopDelay = 0;
        if(clipID == null) button.FileName = "";
        else button.FileName = clipID;

        Debug.Log(System.IO.Path.GetExtension(buttonLabelInput.text));
        string newText = buttonLabelInput.text.Replace(mac.sfxDirectory + mac.sep, "");
        button.Label = newText;
        button.GetComponentInChildren<TMP_Text>().text = newText;
        editButtonPanel.SetActive(false);
    }

    void CancelEditing()
    {
        editButtonPanel.SetActive(false);
    }

    void ChangeFile()
    {
        fsvc.LoadFileSelectionView(buttonID);
    }

    public void StartEditing(int id)
    {
        //Prepare UI for user to begin editing
        buttonID = id;
        
        SFXButton button = mac.sfxButtons[mac.activePage][id].GetComponent<SFXButton>();
        loopButton.isOn = button.Loop;
        minLoopDelay.text = button.MinLoopDelay.ToString("N0");
        newClip = button.aSource.clip;
        clipID = button.FileName;
        if (!String.IsNullOrEmpty(button.FileName)) fileNameLabel.text = clipID.Replace(mac.sfxDirectory + mac.sep, "");
        else fileNameLabel.text = "";

        minimumVolumeSlider.value = button.minimumFadeVolume * 100;
        maximumVolumeSlider.value = button.maximumFadeVolume * 100;

        maxVolumeLabel.text = (button.maximumFadeVolume * 100).ToString("N0") + "%";
        minVolumeLabel.text = (button.minimumFadeVolume * 100).ToString("N0") + "%";

        randomizeLoopButton.isOn = button.RandomizeLoopDelay;
        minLoopDelay.text = button.MinLoopDelay.ToString("N0");
        maxLoopDelay.text = button.MaxLoopDelay.ToString("N0");
        randomizeLoopPanel.SetActive(loopButton.isOn);
        minLoopTimePanel.SetActive(loopButton.isOn);
        maxLoopTimePanel.SetActive(randomizeLoopButton.isOn);
        minLoopDelayLabel.text = randomizeLoopButton.isOn ? "Min Loop Delay (sec):" : "Loop Delay (sec):";

        string currentLabel = mac.sfxButtons[mac.activePage][buttonID].GetComponentInChildren<TMP_Text>().text;
        editButtonPanel.SetActive(true);
        buttonLabelInput.text = String.IsNullOrEmpty(currentLabel) ? "Button Label..." : currentLabel;
    }

    //Called when button file is changed
    internal void UpdateFile(AudioClip clip, string newClipID)
    {

        newClip = clip;
        clipID = newClipID;
        //Debug.Log(clipID.Replace(mac.sfxDirectory + mac.sep, ""));
        string newLabel = clipID.Replace(mac.sfxDirectory + mac.sep, "").Replace(System.IO.Path.GetExtension(clipID), "");
        fileNameLabel.text = newLabel;
        buttonLabelInput.text = newLabel;
    }

    //Called when file is cleared
    internal void ClearFile()
    {
        clipID = null;
        newClip = null;
        
        fileNameLabel.text = "";
        buttonLabelInput.text = "";
    }
}

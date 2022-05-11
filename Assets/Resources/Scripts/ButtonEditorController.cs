using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Class for editing Sound effect buttons
public class ButtonEditorController : MonoBehaviour
{
    private int buttonID;
    public TMP_InputField buttonLabelInput;
    public TMP_Text placeholderText;

    public Button applyButton;
    public Button cancelButton;
    public Button changeFileButton;

    public Toggle ignoreOnPlayAllButton;
    public Toggle loopButton;
    public Toggle randomizeLoopButton;

    private MainAppController mac;
    private SFXPageController spc;
    public GameObject editButtonPanel;
    private FileSelectViewController fsvc;

    public TMP_Text fileNameLabel;

    public TMP_InputField minLoopDelay;
    public TMP_InputField maxLoopDelay;

    public Slider minimumVolumeSlider;
    public Slider maximumVolumeSlider;

    public TMP_Text minVolumeLabel;
    public TMP_Text maxVolumeLabel;
    public TMP_Text minLoopDelayLabel;

    public GameObject randomizeLoopPanel;
    public GameObject minLoopTimePanel;
    public GameObject maxLoopTimePanel;

    public GameObject buttonColorPanel;
    public Button changeColorButton;
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public TMP_Text redText;
    public TMP_Text greenText;
    public TMP_Text blueText;
    public Image previewColorImage;
    public Button CancelColorButton;
    public Button AcceptColorButton;

    private Color imageColor = Color.white;

    private float redValue = 1f;
    private float greenValue = 1f;
    private float blueValue = 1f;

    internal string clipID = null;

    public Button clearFileButton;

    // Start is called before the first frame update
    private void Start()
    {
        //Assign listeners
        clipID = null;
        applyButton.onClick.AddListener(ApplySettings);
        cancelButton.onClick.AddListener(CancelEditing);
        changeFileButton.onClick.AddListener(ChangeFile);
        clearFileButton.onClick.AddListener(ClearFile);
        mac = Camera.main.GetComponent<MainAppController>();
        fsvc = Camera.main.GetComponent<FileSelectViewController>();
        spc = Camera.main.GetComponent<SFXPageController>();
        loopButton.onValueChanged.AddListener(LoopChanged);
        randomizeLoopButton.onValueChanged.AddListener(RandomizeChanged);

        minimumVolumeSlider.onValueChanged.AddListener(MinVolumeChanged);
        maximumVolumeSlider.onValueChanged.AddListener(MaxVolumeChanged);

        buttonLabelInput.onSelect.AddListener(TextSelected);
        changeColorButton.onClick.AddListener(ShowChangeButtonColorMenu);
        redSlider.onValueChanged.AddListener(r => ColorSliderChanged(0, r));
        greenSlider.onValueChanged.AddListener(g => ColorSliderChanged(1, g));
        blueSlider.onValueChanged.AddListener(b => ColorSliderChanged(2, b));

        CancelColorButton.onClick.AddListener(CancelColorClicked);
        AcceptColorButton.onClick.AddListener(AcceptColorClicked);
    }

    private void CancelColorClicked()
    {
        buttonColorPanel.SetActive(false);
    }

    private void AcceptColorClicked()
    {
        buttonColorPanel.SetActive(false);
        changeColorButton.GetComponent<Image>().color = imageColor;

    }

    private void ColorSliderChanged(int color, float value)
    {
        if (color == 0)
        {
            redValue = value;
        }
        else if (color == 1)
        {
            greenValue = value;
        }
        else if (color == 2)
        {
            blueValue = value;
        }
        UpdateColorSliderLabels();
        imageColor = new Color(redValue, greenValue, blueValue);
        previewColorImage.color = imageColor;
    }

    private void UpdateColorSliderLabels()
    {
        redText.text = (redValue * 100f).ToString("N0") + "%";
        greenText.text = (greenValue * 100f).ToString("N0") + "%";
        blueText.text = (blueValue * 100f).ToString("N0") + "%";
    }

    private void ShowChangeButtonColorMenu()
    {
        imageColor = changeColorButton.GetComponent<Image>().color;
        previewColorImage.color = imageColor;
        redValue = imageColor.r;
        greenValue = imageColor.g;
        blueValue = imageColor.b;
        redSlider.SetValueWithoutNotify(redValue);
        greenSlider.SetValueWithoutNotify(greenValue);
        blueSlider.SetValueWithoutNotify(blueValue);

        UpdateColorSliderLabels();
        buttonColorPanel.SetActive(true);
    }

    private void TextSelected(string val)
    {
        placeholderText.text = "";
    }

    private void MinVolumeChanged(float val)
    {
        if (val < maximumVolumeSlider.value)
        {
            minVolumeLabel.text = (val).ToString("N0") + "%";
        }
        else
        {
            minimumVolumeSlider.SetValueWithoutNotify(maximumVolumeSlider.value - 1);
            minVolumeLabel.text = (maximumVolumeSlider.value - 1).ToString("N0") + "%";
        }
    }

    private void MaxVolumeChanged(float val)
    {
        if (val > minimumVolumeSlider.value)
        {
            maxVolumeLabel.text = (val).ToString("N0") + "%";
        }
        else
        {
            maximumVolumeSlider.SetValueWithoutNotify(minimumVolumeSlider.value + 1);
            maxVolumeLabel.text = (minimumVolumeSlider.value + 1).ToString("N0") + "%";
        }
    }

    //Called when "Randomize Loop Delay" is changed
    private void RandomizeChanged(bool val)
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
    private void LoopChanged(bool val)
    {
        if (val)
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

    internal void ApplySettings()
    {
        //Applies all changed settings
        SFXButton button = spc.pageParents[SFXPageController.activePage].buttons[buttonID].GetComponent<SFXButton>();
        button.Stop();

        button.FileName = clipID;
        button.LoopEnabled = loopButton.isOn;
        button.RandomizeLoopDelay = randomizeLoopButton.isOn;
        button.MinLoopDelay = Convert.ToInt32(minLoopDelay.text);
        button.MaxLoopDelay = Convert.ToInt32(maxLoopDelay.text);
        button.minimumFadeVolume = minimumVolumeSlider.value / 100;
        button.maximumFadeVolume = maximumVolumeSlider.value / 100;
        button.IgnorePlayAll = ignoreOnPlayAllButton.isOn;
        button.ButtonEdgeColor = changeColorButton.GetComponent<Image>().color;

        if (!String.IsNullOrEmpty(minLoopDelay.text))
        {
            button.MinLoopDelay = Convert.ToInt32(minLoopDelay.text);
        }
        else
        {
            button.MinLoopDelay = 0;
        }

        string newText = buttonLabelInput.text.Replace(MainAppController.workingDirectories["sfxDirectory"], "");
        button.Label = newText;
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
        editButtonPanel.SetActive(false);
    }

    internal void CancelEditing()
    {
        editButtonPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    private void ChangeFile()
    {
        fsvc.LoadFileSelectionView();
        mac.currentMenuState = MainAppController.MenuState.selectSFXFile;
    }

    public void StartEditing(int id)
    {
        //Prepare UI for user to begin editing
        buttonID = id;
        SFXButton button = spc.pageParents[SFXPageController.activePage].buttons[buttonID].GetComponent<SFXButton>();
        loopButton.isOn = button.LoopEnabled;
        minLoopDelay.text = button.MinLoopDelay.ToString("N0");
        clipID = button.FileName;

        if (!String.IsNullOrEmpty(button.FileName))
        {
            fileNameLabel.text = Path.GetFileName(clipID);
        }
        else
        {
            fileNameLabel.text = "";
        }

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
        ignoreOnPlayAllButton.isOn = button.IgnorePlayAll;
        changeColorButton.GetComponent<Image>().color = button.ButtonEdgeColor;

        string currentLabel = spc.pageParents[SFXPageController.activePage].buttons[buttonID].GetComponentInChildren<TMP_Text>().text;
        if (String.IsNullOrEmpty(currentLabel))
        {
            buttonLabelInput.text = "";
            placeholderText.text = "Type a button label...";
        }
        else
        {
            buttonLabelInput.text = currentLabel;
            placeholderText.text = "";
        }
        mac.currentMenuState = MainAppController.MenuState.editingSFXButton;

        editButtonPanel.SetActive(true);
    }

    //Called when button file is changed
    internal void UpdateFile(string newClipID)
    {

        clipID = newClipID;

        string newLabel = Path.GetFileName(clipID);
        fileNameLabel.text = newLabel;
        newLabel = Path.GetFileNameWithoutExtension(clipID);
        buttonLabelInput.text = newLabel;
        placeholderText.text = "";
    }

    //Called when file is cleared
    internal void ClearFile()
    {
        clipID = "";
        fileNameLabel.text = "";
        buttonLabelInput.text = "";
        placeholderText.text = "Type a button label...";

    }
}

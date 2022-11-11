using Extensions;
using NLayer;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Base class for sfx buttons
public class SFXButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Stores only clip path, without directory information. Ex: song.mp3
    private string fileName = null;
    private static SFXPageController spc;
    public string FileName
    {
        get => fileName;
        set
        {
            if (string.IsNullOrEmpty(value) && playbackBarRect.gameObject.activeSelf)
            {
                playbackBarRect.gameObject.SetActive(false);
            }
            else if (!string.IsNullOrEmpty(value) && !playbackBarRect.gameObject.activeSelf)
            {
                playbackBarRect.gameObject.SetActive(true);
            }

            if (FileName != value)
            {
                Stop();
            }

            if (!string.IsNullOrEmpty(value))
            {
                fileName = value;
                string extension = Path.GetExtension(FileName);
                if (extension == ".mp3")
                {
                    StreamMP3File();
                }
                else if (extension == ".ogg")
                {
                    StreamOggFile();
                }
                buttonHasTrackImage.gameObject.SetActive(true);
                thisButton.interactable = true;
                progressText.gameObject.SetActive(true);
                progressText.text = "0:00/" + (aSource.clip.length / 60f).ToString("N0") + ":" + Mathf.FloorToInt(aSource.clip.length % 60).ToString("D2");

            }
            else
            {
                playbackBarRect.sizeDelta = new Vector2(0, playbackBarRect.sizeDelta.y);
                buttonHasTrackImage.gameObject.SetActive(false);
                thisButton.interactable = false;
                progressText.gameObject.SetActive(false);
            }

        }
    }
    public bool IsPlaying => aSource.isPlaying;
    public int page;
    public int id;
    private static ButtonEditorController bec;
    public static FileSelectViewController vc;
    private static MainAppController mac;
    private Button thisButton;
    private static (int page, int id) hasPointer = (-1, -1);    //page, id
    public AudioSource aSource;
    private Image bgImage;
    private GameObject playBackBar;
    private TMP_Text TMPLabel;
    public Image buttonHasTrackImage;

    public Image buttonEdgeImage;
    private Color buttonEdgeColor = Color.white;
    internal Color ButtonEdgeColor
    {
        get => buttonEdgeColor;
        set
        {
            buttonEdgeColor = value;
            buttonEdgeImage.color = buttonEdgeColor;
        }
    }

    private MpegFile stream;
    private NVorbis.VorbisReader vorbis;

    private float localVolume = 1;
    private string label;

    private Slider volumeSlider;
    private float rectWidth;

    private float masterVolume = 1f;
    internal float minimumFadeVolume = 0f;
    internal float maximumFadeVolume = 1f;

    //private bool isWaiting = false;
    private bool ignorePlayAll = false;

    private float waitStartedTime;
    private float timeToWait;
    private RectTransform playbackBarRect;

    public Button fadeInButton;
    public Button fadeOutButton;
    private Coroutine activeFadeInRoutine;
    private Coroutine activeFadeOutRoutine;
    private const float FADE_RATE = 0.004f;

    public TMP_Text ignorePlayAllIndicator;

    private float framesSinceColorUpdate;
    internal static bool fadeToBlack = false;

    public TMP_Text progressText;
    private Color currentColor;
    private Coroutine discoCR;

    private bool stopped = true;
    private bool loopDelayRoutineActive = false;

    internal bool IgnorePlayAll
    {
        get => ignorePlayAll;
        set
        {
            ignorePlayAll = value;
            ignorePlayAllIndicator.gameObject.SetActive(ignorePlayAll);
        }
    }


    internal string Label
    {
        get => label;
        set
        {
            label = value;
            TMPLabel.SetText(label);
        }
    }

    internal float LocalVolume
    {
        get => localVolume.ToLog();

        set
        {
            localVolume = value.ToActual();
            volumeSlider.SetValueWithoutNotify(value);
        }
    }

    public bool LoopEnabled { get; set; } = false;
    public float MinLoopDelay { get; set; } = 0;
    public float MaxLoopDelay { get; set; } = 0;
    public bool RandomizeLoopDelay { get; set; } = false;
    internal void Init()
    {

        fadeInButton.onClick.AddListener(delegate { FadeVolume("in", false); });
        fadeOutButton.onClick.AddListener(delegate { FadeVolume("out", false); });
        TMPLabel = GetComponentInChildren<TMP_Text>();
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(Clicked);
        vc = Camera.main.GetComponent<FileSelectViewController>();
        aSource = GetComponent<AudioSource>();
        bec = Camera.main.GetComponent<ButtonEditorController>();
        bgImage = GetComponent<Image>();
        rectWidth = GetComponent<RectTransform>().sizeDelta.x;
        playBackBar = GetComponentInChildren<PlaybackTimer>().gameObject;
        volumeSlider = GetComponentInChildren<Slider>();
        playbackBarRect = playBackBar.GetComponent<RectTransform>();
        mac = Camera.main.GetComponent<MainAppController>();
        volumeSlider.onValueChanged.AddListener(VolumeSliderAdjusted);
        currentColor = GetComponent<Image>().color;
        spc = Camera.main.GetComponent<SFXPageController>();
    }


    internal void SetDiscoMode(bool active)
    {
        if (!active)
        {
            if (discoCR != null)
            {
                StopCoroutine(discoCR);
            }

            discoCR = null;
            GetComponent<Image>().color = MainAppController.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight;
            currentColor = GetComponent<Image>().color;
        }
    }

    internal void ChangeButtonColor(Color newColor)
    {
        GetComponent<Image>().color = newColor;
        buttonHasTrackImage.color = newColor;
    }

    private IEnumerator DiscoModeUpdate()
    {
        const int numSteps = 100;
        while (true)
        {
            Image btnImage = GetComponent<Image>();
            Color newColor = btnImage.color;
            while (btnImage.color == newColor)
            {
                newColor = GetNewColor();
            }
            newColor *= 0.3f;
            currentColor = btnImage.color;
            for (int i = 0; i < numSteps; i++)
            {
                float fadeRatio = ((float)i / numSteps);

                float newR = Mathf.Lerp(currentColor.r, newColor.r, fadeRatio);
                float newG = Mathf.Lerp(currentColor.g, newColor.g, fadeRatio);
                float newB = Mathf.Lerp(currentColor.b, newColor.b, fadeRatio);
                if (fadeToBlack)
                {
                    newR -= (framesSinceColorUpdate / 100f);
                    newG -= (framesSinceColorUpdate / 100f);
                    newB -= (framesSinceColorUpdate / 100f);
                }

                btnImage.color = new Color(newR, newG, newB);

                yield return null;
                if (UnityEngine.Random.Range(0, 2) == 1)
                {
                    yield return null;
                }

                if (UnityEngine.Random.Range(0, 2) == 1)
                {
                    yield return null;
                }
            }
        }
    }

    internal void ChangeColor()
    {
        if (discoCR != null)
        {
            StopCoroutine(discoCR);
        }

        Image btnImage = GetComponent<Image>();
        Color newColor = GetNewColor();
        int iter = 0;
        while (newColor == btnImage.color)
        {
            GetNewColor();
            iter++;
            if (iter > 100)
            {
                break;
            }
        }
        btnImage.color = newColor;
        discoCR = StartCoroutine(DiscoModeUpdate());
        framesSinceColorUpdate = 0;
    }

    internal void FadeVolume(string type, bool fromFadeAll = false)
    {
        if (fromFadeAll && IgnorePlayAll) { }
        else
        {
            if (type == "in")
            {
                if (activeFadeOutRoutine != null)
                {
                    StopCoroutine(activeFadeOutRoutine);
                    activeFadeOutRoutine = null;
                }
                if (activeFadeInRoutine != null)
                {
                    StopCoroutine(activeFadeInRoutine);
                    activeFadeInRoutine = null;
                }
                activeFadeInRoutine = StartCoroutine(FadeInRoutine());
            }
            else if (type == "out")
            {
                if (activeFadeInRoutine != null)
                {
                    StopCoroutine(activeFadeInRoutine);
                    activeFadeInRoutine = null;
                }
                if (activeFadeOutRoutine != null)
                {
                    StopCoroutine(activeFadeOutRoutine);
                    activeFadeOutRoutine = null;
                }
                activeFadeOutRoutine = StartCoroutine(FadeOutRoutine());
            }
        }
    }

    private Color GetNewColor()
    {
        int colorIndex = UnityEngine.Random.Range(0, DiscoMode.colours.Count - 1);
        return DiscoMode.colours[colorIndex];
    }

    private IEnumerator FadeInRoutine()
    {
        while (LocalVolume < maximumFadeVolume)
        {
            ChangeLocalVolume(LocalVolume + FADE_RATE);
            yield return null;
        }
        activeFadeInRoutine = null;
        yield break;
    }

    private IEnumerator FadeOutRoutine()
    {
        while (LocalVolume > minimumFadeVolume)
        {
            ChangeLocalVolume(Mathf.Max(0, LocalVolume - FADE_RATE));
            yield return null;
        }
        activeFadeOutRoutine = null;
        yield break;
    }

    private void Clicked()
    {
        if (!stopped)
        {
            Stop();
        }
        else
        {
            Play();
        }
    }

    private void ChangeLocalVolume(float newLocalVol)
    {
        if (newLocalVol > 0 && LocalVolume <= 0 && !stopped)
        {
            spc.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources++;
        }
        else if (newLocalVol <= 0 && LocalVolume > 0 && !stopped)
        {
            spc.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
        }

        LocalVolume = newLocalVol;
        aSource.volume = LocalVolume * masterVolume;
    }

    private void VolumeSliderAdjusted(float value)
    {
        if (activeFadeInRoutine != null)
        {
            StopCoroutine(activeFadeInRoutine);
        }

        if (activeFadeOutRoutine != null)
        {
            StopCoroutine(activeFadeOutRoutine);
        }

        activeFadeInRoutine = null;
        activeFadeOutRoutine = null;
        ChangeLocalVolume(value);
    }

    internal void Stop(bool fromStopAll = false)
    {
        if (fromStopAll && IgnorePlayAll) { }
        else
        {
            if (stream != null)
            {
                stream.Position = 0L;
            }

            if (vorbis != null)
            {
                vorbis.DecodedPosition = 0L;
            }

            if (LocalVolume > 0 && !stopped)
            {
                spc.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
            }

            buttonHasTrackImage.color = bgImage.color;
            aSource.Stop();
            stopped = true;
            ChangeButtonColor(MainAppController.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight);
        }
    }

    internal void Play(bool fromPlayAll = false)
    {
        if (fromPlayAll && IgnorePlayAll) { }
        else
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                string extension = Path.GetExtension(FileName);
                if (extension == ".mp3" || extension == ".ogg")
                {
                    PlayValidFile();
                }
                else
                {
                    mac.ShowErrorMessage(extension + " file type not supported", 1);
                }
            }
        }
    }

    private void PlayValidFile()
    {
        if (LocalVolume > 0)
        {
            spc.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources++;
        }

        bgImage.color = ResourceManager.green;
        buttonHasTrackImage.color = ResourceManager.green;
        Image rect = playBackBar.GetComponent<Image>();
        rect.color = ResourceManager.red;
        aSource.Play();
        stopped = false;
    }

    private void StreamMP3File()
    {
        if (stream != null)
        {
            stream.Dispose();
        }

        try
        {
            string filePath = FileName;
            print(filePath);
            stream = new MpegFile(filePath);

            long clipSize = stream.Length / (stream.Channels * 4);
            if (int.MaxValue < stream.Length)
            {
                clipSize = int.MaxValue;
            }

            int sampleRate = stream.SampleRate;
            AudioClip newClip = AudioClip.Create(FileName, (int)clipSize, stream.Channels, sampleRate, true, Mp3Callback);
            aSource.clip = newClip;
        }
        catch (FileNotFoundException)
        {
            mac.ShowErrorMessage("File " + FileName + " not found! Was it deleted?", 1);
        }
    }

    private void StreamOggFile()
    {
        if (vorbis != null)
        {
            vorbis.Dispose();
        }

        try
        {
            vorbis = new NVorbis.VorbisReader(FileName);
            long clipSize = vorbis.TotalSamples;
            int sampleRate = vorbis.SampleRate;
            AudioClip newClip = AudioClip.Create(FileName, (int)clipSize, 2, sampleRate, true, VorbisCallback);
            aSource.clip = newClip;
        }
        catch (FileNotFoundException)
        {
            mac.ShowErrorMessage("File " + FileName + " not found! Was it deleted?", 1);
        }
    }

    private void VorbisCallback(float[] data)
    {
        vorbis.ReadSamples(data, 0, data.Length);
    }

    private void Mp3Callback(float[] data)
    {
        stream.ReadSamples(data, 0, data.Length);
    }

    internal void ChangeMasterVolume(float newMasterVolume)
    {
        masterVolume = newMasterVolume;
        try
        {
            aSource.volume = LocalVolume * masterVolume;
        }
        catch (UnassignedReferenceException)
        {
            print(id);
            print(page);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && hasPointer == (page, id))
        {
            bec.StartEditing(id);
        }
        //this is gross.Fix pls
        if (!aSource.isPlaying && !stopped && LoopEnabled && !loopDelayRoutineActive && !string.IsNullOrEmpty(aSource.clip.name))
        {
            StartCoroutine("WaitForLoopDelay");
        }
        if (GetComponent<Image>().color == ResourceManager.green && !aSource.isPlaying && !loopDelayRoutineActive)
        {
            ChangeButtonColor(MainAppController.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight);
            Stop();
        }
        if (!stopped && !loopDelayRoutineActive)
        {
            float percentPlayed = (aSource.time / aSource.clip.length);
            playbackBarRect.sizeDelta = new Vector2(Mathf.Max(1, (percentPlayed * rectWidth)), playbackBarRect.rect.height);
            progressText.text = (aSource.time / 60f).ToString("N0") + ":" + Mathf.FloorToInt(aSource.time % 60f).ToString("D2") + "/" + (aSource.clip.length / 60f).ToString("N0") + ":" + Mathf.FloorToInt(aSource.clip.length % 60).ToString("D2");
        }
        framesSinceColorUpdate = Mathf.Max(framesSinceColorUpdate, framesSinceColorUpdate + 1);
    }

    private IEnumerator WaitForLoopDelay()
    {
        loopDelayRoutineActive = true;
        if (LocalVolume > 0)
        {
            spc.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
        }

        waitStartedTime = Time.realtimeSinceStartup;
        Image rect = playBackBar.GetComponent<Image>();
        rect.color = Color.yellow;

        timeToWait = RandomizeLoopDelay ? UnityEngine.Random.Range(MinLoopDelay, MaxLoopDelay) : MinLoopDelay;
        while (timeToWait + waitStartedTime > Time.realtimeSinceStartup)
        {
            float percentWaited = ((Time.realtimeSinceStartup - waitStartedTime) / timeToWait);
            playbackBarRect.sizeDelta = new Vector2((percentWaited * rectWidth), playbackBarRect.rect.height);
            if (stopped)
            {
                loopDelayRoutineActive = false;
                break;
            }
            yield return null;
        }

        if (!stopped)
        {
            if (stream != null)
            {
                stream.Position = 0L;
            }

            if (vorbis != null)
            {
                vorbis.DecodedPosition = 0L;
            }

            loopDelayRoutineActive = false;
            Play();
            rect.color = ResourceManager.red;
        }
        playbackBarRect.sizeDelta = new Vector2(0f, playbackBarRect.rect.height);
        loopDelayRoutineActive = false;

        yield return null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hasPointer = (page, id);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hasPointer = (-1, -1);
    }

    private void OnApplicationQuit()
    {
        if (stream != null)
        {
            stream.Dispose();
        }

        if (vorbis != null)
        {
            vorbis.Dispose();
        }
    }
}

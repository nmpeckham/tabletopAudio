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
    public string FileName
    {
        get { return fileName; }
        set
        {
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

    internal int page;
    internal int id;
    static private ButtonEditorController bec;
    static public FileSelectViewController vc;
    static private MainAppController mac;
    private Button thisButton;
    static (int page, int id) hasPointer = (-1, -1);    //page, id
    public AudioSource aSource;
    private Image bgImage;
    private GameObject playBackBar;
    private TMP_Text TMPLabel;
    public Image buttonHasTrackImage;

    public Image buttonEdgeImage;
    private Color buttonEdgeColor = Color.white;
    internal Color ButtonEdgeColor
    {
        get { return buttonEdgeColor; }
        set
        {
            buttonEdgeColor = value;
            buttonEdgeImage.color = buttonEdgeColor;
        }
    }

    MpegFile stream;
    NVorbis.VorbisReader vorbis;

    private float localVolume = 1;
    string label;

    private Slider volumeSlider;
    private float rectWidth;

    private float masterVolume = 1f;
    internal float minimumFadeVolume = 0f;
    internal float maximumFadeVolume = 1f;

    private bool isWaiting = false;
    internal bool isPlaying = false;
    private bool ignorePlayAll = false;

    private float waitStartedTime;
    private float timeToWait;

    RectTransform playbackBarRect;

    public Button fadeInButton;
    public Button fadeOutButton;

    Coroutine activeFadeInRoutine;
    Coroutine activeFadeOutRoutine;

    const float FADE_RATE = 0.004f;

    public TMP_Text ignorePlayAllIndicator;

    private float framesSinceColorUpdate;
    internal static bool fadeToBlack = false;

    public TMP_Text progressText;

    Color currentColor;
    Coroutine discoCR;

    internal bool IgnorePlayAll
    {
        get
        {
            return ignorePlayAll;
        }
        set
        {
            ignorePlayAll = value;
            ignorePlayAllIndicator.gameObject.SetActive(ignorePlayAll);
        }
    }


    internal string Label
    {
        get
        {
            return label;
        }
        set
        {
            label = value;
            TMPLabel.SetText(label);
        }
    }

    internal float LocalVolume
    {
        get
        {
            return localVolume.ToLog();
        }

        set
        {
            localVolume = value.ToActual();
            volumeSlider.SetValueWithoutNotify(value);
        }
    }

    public bool Loop { get; set; } = false;
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
    }


    internal void SetDiscoMode(bool active)
    {
        if (!active)
        {
            if (discoCR != null) StopCoroutine(discoCR);
            discoCR = null;
            GetComponent<Image>().color = mac.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight;
            currentColor = GetComponent<Image>().color;
        }
    }

    internal void ChangeButtonColor(Color newColor)
    {
        GetComponent<Image>().color = newColor;
        buttonHasTrackImage.color = newColor;
    }

    IEnumerator DiscoModeUpdate()
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
                if (UnityEngine.Random.Range(0, 2) == 1) yield return null;
                if (UnityEngine.Random.Range(0, 2) == 1) yield return null;
            }
        }
    }

    internal void ChangeColor()
    {
        if (discoCR != null) StopCoroutine(discoCR);
        Image btnImage = GetComponent<Image>();
        Color newwColor = GetNewColor();
        while (newwColor == btnImage.color)
        {
            GetNewColor();
        }
        btnImage.color = GetNewColor();
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
                if (activeFadeInRoutine == null) activeFadeInRoutine = StartCoroutine(FadeInRoutine());
                else
                {
                    StopCoroutine(activeFadeInRoutine);
                    activeFadeInRoutine = null;
                }
            }
            else if (type == "out")
            {
                if (activeFadeInRoutine != null)
                {
                    StopCoroutine(activeFadeInRoutine);
                    activeFadeInRoutine = null;
                }
                if (activeFadeOutRoutine == null) activeFadeOutRoutine = StartCoroutine(FadeOutRoutine());
                else
                {
                    StopCoroutine(activeFadeOutRoutine);
                    activeFadeOutRoutine = null;
                }
            }
        }
    }

    Color GetNewColor()
    {
        int colorIndex = UnityEngine.Random.Range(0, DiscoMode.colours.Count - 1);
        return DiscoMode.colours[colorIndex];
    }

    IEnumerator FadeInRoutine()
    {
        while (LocalVolume < maximumFadeVolume)
        {
            ChangeLocalVolume(LocalVolume + FADE_RATE);
            yield return null;
        }
        activeFadeInRoutine = null;
        yield break;
    }

    IEnumerator FadeOutRoutine()
    {
        while (LocalVolume > minimumFadeVolume)
        {
            ChangeLocalVolume(Mathf.Max(0, LocalVolume - FADE_RATE));
            yield return null;
        }
        activeFadeOutRoutine = null;
        yield break;
    }


    void Clicked()
    {
        if (aSource.isPlaying || isWaiting)
        {
            Stop();
        }
        else if (!aSource.isPlaying)
        {
            Play();
        }
    }

    private void ChangeLocalVolume(float newLocalVol)
    {
        if (newLocalVol > 0 && LocalVolume <= 0 && isPlaying && !isWaiting) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources++;
        else if (newLocalVol <= 0 && LocalVolume > 0 && isPlaying && !isWaiting) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
        LocalVolume = newLocalVol;
        aSource.volume = LocalVolume * masterVolume;
    }

    private void VolumeSliderAdjusted(float value)
    {
        if (activeFadeInRoutine != null) StopCoroutine(activeFadeInRoutine);
        if (activeFadeOutRoutine != null) StopCoroutine(activeFadeOutRoutine);
        activeFadeInRoutine = null;
        activeFadeOutRoutine = null;
        ChangeLocalVolume(value);
    }

    internal void Stop(bool fromStopAll = false)
    {
        if (fromStopAll && IgnorePlayAll) { }
        else
        {
            if (LocalVolume > 0 && isPlaying && !isWaiting) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
            isPlaying = false;
            bgImage.color = mac.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight;
            buttonHasTrackImage.color = bgImage.color;
            aSource.Stop();
            if (stream != null) stream.Position = 0L;
            if (vorbis != null) vorbis.DecodedPosition = 0L;
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
                //file pre-loading is done when FileName is set
                if (extension == ".mp3" || extension == ".ogg")
                {
                    PlayValidFile();
                }
                else
                {
                    mac.ShowErrorMessage(extension + " file type not supported");
                }
            }
        }
    }
    void PlayValidFile()
    {
        if (LocalVolume > 0) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources++;
        isPlaying = true;
        bgImage.color = ResourceManager.green;
        buttonHasTrackImage.color = ResourceManager.green;
        Image rect = playBackBar.GetComponent<Image>();
        rect.color = ResourceManager.red;
        aSource.Play();
    }

    void StreamMP3File()
    {
        if (stream != null) stream.Dispose();
        try
        {
            string filePath = System.IO.Path.Combine(mac.sfxDirectory, FileName);
            stream = new MpegFile(filePath);

            long clipSize = stream.Length / (stream.Channels * 4);
            if (int.MaxValue < stream.Length) clipSize = int.MaxValue;
            int sampleRate = stream.SampleRate;
            AudioClip newClip = AudioClip.Create(FileName, (int)clipSize, stream.Channels, sampleRate, true, Mp3Callback);
            aSource.clip = newClip;
        }
        catch (FileNotFoundException)
        {
            mac.ShowErrorMessage("File " + FileName + " not found! Was it deleted?");
        }
    }

    void StreamOggFile()
    {
        if (vorbis != null) vorbis.Dispose();
        try
        {
            vorbis = new NVorbis.VorbisReader(System.IO.Path.Combine(mac.sfxDirectory, FileName));
            long clipSize = vorbis.TotalSamples;
            int sampleRate = vorbis.SampleRate;
            AudioClip newClip = AudioClip.Create(FileName, (int)clipSize, 2, sampleRate, true, VorbisCallback);
            aSource.clip = newClip;
        }
        catch (FileNotFoundException)
        {
            mac.ShowErrorMessage("File " + FileName + " not found! Was it deleted?");
        }

    }

    void VorbisCallback(float[] data)
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

    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(1) && hasPointer == (page, id))
        {
            bec.StartEditing(id);
        }
        //this is gross.Fix pls
        if (!aSource.isPlaying && Loop && !isWaiting && isPlaying)
        {
            StartCoroutine("WaitForLoopDelay");
        }
        if (!aSource.isPlaying && !isWaiting)
        {
            ChangeButtonColor(mac.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight);
        }
        if (aSource.isPlaying)
        {
            float percentPlayed = (aSource.time / aSource.clip.length);
            playbackBarRect.sizeDelta = new Vector2(Mathf.Max(1, (percentPlayed * rectWidth)), playbackBarRect.rect.height);
            progressText.text = (aSource.time / 60f).ToString("N0") + ":" + Mathf.FloorToInt(aSource.time % 60f).ToString("D2") + "/" + (aSource.clip.length / 60f).ToString("N0") + ":" + Mathf.FloorToInt(aSource.clip.length % 60).ToString("D2");
        }
        if (isWaiting && isPlaying)
        {
            float percentWaited = ((Time.time - waitStartedTime) / timeToWait);
            playbackBarRect.sizeDelta = new Vector2((percentWaited * rectWidth), playbackBarRect.rect.height);
        }

        //prevent playback bar from showing after clip has been removed
        if (string.IsNullOrEmpty(FileName) && playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(false);
        else if (!string.IsNullOrEmpty(FileName) && !playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(true);

        framesSinceColorUpdate = Mathf.Max(framesSinceColorUpdate, framesSinceColorUpdate + 1);
    }

    IEnumerator WaitForLoopDelay()
    {
        if (LocalVolume > 0) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
        isWaiting = true;
        waitStartedTime = Time.time;
        Image rect = playBackBar.GetComponent<Image>();
        rect.color = Color.yellow;

        timeToWait = RandomizeLoopDelay ? UnityEngine.Random.Range(MinLoopDelay, MaxLoopDelay) : MinLoopDelay;
        while (timeToWait + waitStartedTime > Time.time)
        {
            if (!isPlaying) break;
            yield return null;
        }

        if (isPlaying)
        {
            if (stream != null) stream.Position = 0L;
            if (vorbis != null) vorbis.DecodedPosition = 0L;

            Play();
            rect.color = ResourceManager.red;
        }
        isWaiting = false;

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
}

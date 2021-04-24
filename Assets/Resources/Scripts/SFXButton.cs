using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System;
using NLayer;
using Extensions;

//Base class for sfx buttons
public class SFXButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Stores only clip path, without directory information. Ex: song.mp3
    public string FileName = null;

    internal int page;
    internal int id;
    private ButtonEditorController bec;
    public FileSelectViewController vc;
    private MainAppController mac;
    private Button thisButton;
    private bool hasPointer = false;
    public AudioSource aSource;
    private Image bgImage;
    private GameObject playBackBar;
    private TMP_Text TMPLabel;

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
    public Button sliderButton;

    Coroutine activeFadeInRoutine;
    Coroutine activeFadeOutRoutine;

    const float FADE_RATE = 0.004f;

    public TMP_Text ignorePlayAllIndicator;

    private float framesSinceColorUpdate;

    Color currentColor;

    internal bool IgnorePlayAll
    {
        get
        {
            return ignorePlayAll;
        }
        set
        {
            ignorePlayAll = value;
            ignorePlayAllIndicator.gameObject.SetActive(ignorePlayAll ? true : false);
        }
    }


    internal string Label { 
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
        volumeSlider.onValueChanged.AddListener(ChangeLocalVolume);
        sliderButton = volumeSlider.GetComponentInChildren<Button>();
        sliderButton.onClick.AddListener(SliderButtonClicked);
        currentColor = GetComponent<Image>().color;
    }

    void SliderButtonClicked()
    {
        print("Slider clicked");
        if (activeFadeOutRoutine != null) StopCoroutine(activeFadeOutRoutine);
        if (activeFadeInRoutine != null) StopCoroutine(activeFadeInRoutine);
    }


    internal void SetDiscoMode(bool active)
    {
        if (!active)
        {
            StopCoroutine("DiscoModeUpdate");
            GetComponent<Image>().color = mac.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight;
            currentColor = GetComponent<Image>().color;
        }
        else StartCoroutine("DiscoModeUpdate");
    }

    IEnumerator DiscoModeUpdate()
    {
        const int numSteps = 100;
        while (true)
        {
            Image btnImage = GetComponent<Image>();
            Color newColor = btnImage.color;
            while(btnImage.color == newColor)
            {
                int colorIndex = UnityEngine.Random.Range(0, ResourceManager.kellysMaxContrastSet.Count - 1);
                newColor = MainAppController.UIntToColor(ResourceManager.kellysMaxContrastSet[colorIndex]);
                yield return null;
            }
            currentColor = btnImage.color;
            for (int i = 0; i < numSteps; i++)
            {
                float fadeRatio = (float)i / numSteps;

                float newR = Mathf.Lerp(currentColor.r, newColor.r, fadeRatio);
                float newG = Mathf.Lerp(currentColor.g, newColor.g, fadeRatio);
                float newB = Mathf.Lerp(currentColor.b, newColor.b, fadeRatio);
                newR = newR - (framesSinceColorUpdate / 100f);
                newG = newG - (framesSinceColorUpdate / 100f);
                newB = newB - (framesSinceColorUpdate / 100f);
                btnImage.color = new Color(newR, newG, newB);

                yield return null;
                if (UnityEngine.Random.Range(0, 2) == 1) yield return null;
            }
        }
    }

    internal void ChangeColor()
    {
        StopAllCoroutines();
        Image btnImage = GetComponent<Image>();
        int colorIndex = UnityEngine.Random.Range(0, ResourceManager.kellysMaxContrastSet.Count - 1);
        Color newColor = MainAppController.UIntToColor(ResourceManager.kellysMaxContrastSet[colorIndex]);
        btnImage.color = newColor;
        StartCoroutine(DiscoModeUpdate());
        framesSinceColorUpdate = 0;
    }

    internal void FadeVolume(string type, bool fromFadeAll=false)
    {
        if (fromFadeAll && IgnorePlayAll) { }
        else
        {
            if(type == "in")
            {
                if (activeFadeOutRoutine != null)
                {
                    print("stopping");
                    StopCoroutine(activeFadeOutRoutine);
                    activeFadeOutRoutine = null;
                }
                if (activeFadeInRoutine == null) activeFadeInRoutine = StartCoroutine(FadeInRoutine());
                else
                {
                    print("stopping");
                    StopCoroutine(activeFadeInRoutine);
                    activeFadeInRoutine = null;
                }
            }
            else if(type == "out")
            {
                if (activeFadeInRoutine != null)
                {
                    print("stopping");
                    StopCoroutine(activeFadeInRoutine);
                    activeFadeInRoutine = null;
                }
                if (activeFadeOutRoutine == null) activeFadeOutRoutine = StartCoroutine(FadeOutRoutine());
                else
                {
                    print("stopping");
                    StopCoroutine(activeFadeOutRoutine);
                    activeFadeOutRoutine = null;
                }
            }

        }
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
    {;
        if (aSource.isPlaying || isWaiting)
        {
            Stop();
        }
        else if(!aSource.isPlaying)
        {
            Play();
        }
    }

    private void ChangeLocalVolume(float newLocalVol)
    {
        if(newLocalVol > 0 && LocalVolume <= 0 && isPlaying && !isWaiting) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources++;
        else if(newLocalVol <= 0 && LocalVolume > 0 && isPlaying && !isWaiting) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
        LocalVolume = newLocalVol;
        aSource.volume = LocalVolume * masterVolume;
    }

    internal void ClearActiveClip()
    {
        aSource.clip = null;
        if (stream != null) stream.Dispose();
        if (vorbis != null) vorbis.Dispose();
    }

    internal void Stop(bool fromStopAll=false)
    {
        if(fromStopAll && IgnorePlayAll) { }
        else
        {
            if (LocalVolume > 0 && isPlaying && !isWaiting) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources--;
            isPlaying = false;
            bgImage.color = mac.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight;
            aSource.Stop();
            aSource.clip = null;
            if (stream != null) stream.Dispose();
            if (vorbis != null) vorbis.Dispose();
        }
    }

    internal void Play(bool fromPlayAll=false)
    {
        if (fromPlayAll && IgnorePlayAll) { }
        else
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                string extension = Path.GetExtension(FileName);
                if (extension == ".mp3")
                {
                    StreamMP3File();
                    PlayValidFile();
                }
                else if (extension == ".ogg")
                {
                    StreamOggFile();
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
        if(LocalVolume > 0) mac.pageButtons[page].GetComponent<PageButton>().ActiveAudioSources++;
        isPlaying = true;
        bgImage.color = ResourceManager.green;
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
        catch(FileNotFoundException)
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
        catch(FileNotFoundException)
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
        catch(UnassignedReferenceException e)
        {
            print(id);
            print(page);
        }
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(1) && hasPointer)
        {
            bec.StartEditing(id);

        }
        if (!aSource.isPlaying && bgImage.color == ResourceManager.green && !Loop) Stop();
        if(!aSource.isPlaying && Loop && !isWaiting && isPlaying)
        {
            StartCoroutine("WaitForLoopDelay");
        }
        if(aSource.isPlaying)
        {
            float percentPlayed = (aSource.time / aSource.clip.length);
            //playbackBarRect.sizeDelta.Set((percentPlayed * rectWidth), playbackBarRect.rect.height);
            playbackBarRect.sizeDelta = new Vector2(Mathf.Max(1, (percentPlayed * rectWidth)), playbackBarRect.rect.height);
        }
        if(isWaiting && isPlaying)
        {
            float percentWaited = ((Time.time - waitStartedTime) / timeToWait);
            //playbackBarRect.sizeDelta.Set((percentWaited * rectWidth), playbackBarRect.rect.height);
            playbackBarRect.sizeDelta = new Vector2((percentWaited * rectWidth), playbackBarRect.rect.height);
        }

        //prevent playback bar from showing after clip has been removed
        if (string.IsNullOrEmpty(FileName) && playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(false);
        else if (!string.IsNullOrEmpty(FileName) && !playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(true);

        framesSinceColorUpdate = Mathf.Max(framesSinceColorUpdate, framesSinceColorUpdate + 1);
        //print(framesSinceColorUpdate);
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
            Play();
            rect.color = ResourceManager.red;
        }
        isWaiting = false;
        
        yield return null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        hasPointer = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {

        hasPointer = false;
    }
}

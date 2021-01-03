using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System;
using NLayer;

//Base class for sfx buttons
public class SFXButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Stores only clip path, without directory information. Ex: song.mp3
    internal string FileName { get; set; } = null;

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

    private bool discoModeActive = false;

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
            return localVolume;
        }

        set
        {
            if (Mathf.Abs(Mathf.Abs(value - localVolume) - FADE_RATE) > FADE_RATE)
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
            }
            localVolume = Mathf.Pow(value, 2f);
            volumeSlider.value = value;
        }
    }

    public bool Loop { get; set; } = false;
    public float MinLoopDelay { get; set; } = 0;
    public float MaxLoopDelay { get; set; } = 0;
    public bool RandomizeLoopDelay { get; set; } = false;
    void Start()
    {
        fadeInButton.onClick.AddListener(delegate { FadeIn(false); });
        fadeOutButton.onClick.AddListener(delegate { FadeOut(false); });
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

    }

    void SliderButtonClicked()
    {
        if (activeFadeOutRoutine != null) StopCoroutine(activeFadeOutRoutine);
        if (activeFadeInRoutine != null) StopCoroutine(activeFadeInRoutine);
    }

    internal void FadeIn(bool fromFadeInAll=false)
    {
        if (fromFadeInAll && IgnorePlayAll) { }
        else
        {
            //stop active fade out routines, if any
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
    }

    internal void SetDiscoMode(bool active)
    {
        if (!active)
        {
            StopCoroutine("DiscoModeUpdate");
            GetComponent<Image>().color = mac.darkModeEnabled ? ResourceManager.sfxButtonDark : ResourceManager.sfxButtonLight;
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
            }
            Color currentColor = btnImage.color;
            for (int i = 0; i < numSteps; i++)
            {
                float fadeRatio = (float)i / numSteps;

                float newR = Mathf.Lerp(currentColor.r, newColor.r, fadeRatio);
                float newG = Mathf.Lerp(currentColor.g, newColor.g, fadeRatio);
                float newB = Mathf.Lerp(currentColor.b, newColor.b, fadeRatio);
                btnImage.color = new Color(newR, newG, newB);

                yield return new WaitForEndOfFrame();
                if (UnityEngine.Random.Range(0, 1) == 1) yield return new WaitForEndOfFrame();
            }
        }
    }

    internal void FadeOut(bool fromFadeOutAll=false)
    {
        if (fromFadeOutAll && IgnorePlayAll) { }
        else
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

    IEnumerator FadeInRoutine()
    {        
        while (LocalVolume < maximumFadeVolume)
        {
            ChangeLocalVolume(LocalVolume + FADE_RATE);
            yield return new WaitForFixedUpdate();
        }

        yield return null;
    }

    IEnumerator FadeOutRoutine()
    {
        while (localVolume > minimumFadeVolume)
        {
            ChangeLocalVolume(LocalVolume - FADE_RATE);
            yield return new WaitForFixedUpdate();
        }
        yield return null;
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
        aSource.volume = LocalVolume * masterVolume;
    }

    void Update()
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
            playbackBarRect.sizeDelta = new Vector2((percentPlayed * rectWidth), playbackBarRect.rect.height);
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
            yield return new WaitForEndOfFrame();
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

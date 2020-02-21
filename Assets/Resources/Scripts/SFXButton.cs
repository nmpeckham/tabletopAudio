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
    internal string clipPath = null;
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
    MemoryStream audioData;
    byte[] buffer;
    byte[] convertedAudioData;


    float localVolume = 1;
    string label;

    private Slider volumeSlider;
    private float rectWidth;

    private float masterVolume = 1f;
    private bool waiting = false;
    private bool play = false;

    private float waitStartedTime;
    private float timeToWait;

    TimeSpan vorbisPosition;
    int vorbisCount;
    RectTransform playbackBarRect;

    public Button fadeInButton;
    public Button fadeOutButton;
    public Button sliderButton;

    Coroutine activeFadeInRoutine;
    Coroutine activeFadeOutRoutine;

    const float FADE_RATE = 0.005f;

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
            //Debug.Log(Mathf.Abs(value - localVolume));
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
            localVolume = value;
            volumeSlider.value = value;
        }
    }

    public bool Loop { get; set; } = false;
    public float MinLoopDelay { get; set; } = 0;
    public float MaxLoopDelay { get; set; } = 0;
    public bool RandomizeLoopDelay { get; set; } = false;
    void Start()
    {
        fadeInButton.onClick.AddListener(FadeIn);
        fadeOutButton.onClick.AddListener(FadeOut);
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
        Debug.Log("clicked");
        if (activeFadeOutRoutine != null) StopCoroutine(activeFadeOutRoutine);
        if (activeFadeInRoutine != null) StopCoroutine(activeFadeInRoutine);
    }

    void FadeIn()
    {
        if (activeFadeOutRoutine != null)
        {
            StopCoroutine(activeFadeOutRoutine);
            activeFadeOutRoutine = null;
        }
        activeFadeInRoutine = StartCoroutine(FadeInRoutine());
    }

    void FadeOut()
    {
        if (activeFadeInRoutine != null)
        {
            StopCoroutine(activeFadeInRoutine);
            activeFadeInRoutine = null;
        }
        activeFadeOutRoutine = StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        for(int i = 0; i < 100; i++)
        {
            while (localVolume < 1f)
            {
                ChangeLocalVolume(localVolume + FADE_RATE);
                yield return new WaitForSecondsRealtime(0.01f);
            }
        }
        yield return null;
    }

    IEnumerator FadeOutRoutine()
    {
        for (int i = 0; i < 100; i++)
        {
            while (localVolume > 0f)
            {
                ChangeLocalVolume(localVolume - FADE_RATE);
                yield return new WaitForSecondsRealtime(0.01f);
            }
        }
        yield return null;
    }


    void Clicked()
    {
        //Debug.Log("Clicked");
        if (aSource.isPlaying || waiting)
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
        //Debug.Log("Vol Changed");
        LocalVolume = newLocalVol;
        aSource.volume = LocalVolume * masterVolume;
    }

    internal void ClearActiveClip()
    {
        aSource.clip = null;
        if (stream != null) stream.Dispose();
        if (vorbis != null) vorbis.Dispose();
    }

    public void Stop()
    {
        //if(stream != null) stream.Dispose();
        play = false;
        bgImage.color = ResourceManager.transWhite;
        aSource.Stop();
        aSource.clip = null;
        if (stream != null) stream.Dispose();
        if (vorbis != null) vorbis.Dispose();
    }

    public void Play()
    {
        //if (stream != null) stream.Dispose();

        if(!string.IsNullOrEmpty(clipPath))
        {
            string extension = Path.GetExtension(clipPath);
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
    void PlayValidFile()
    {
        play = true;
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
            stream = new MpegFile(clipPath);
            long clipSize = stream.Length / (stream.Channels * 4);
            if (int.MaxValue < stream.Length) clipSize = int.MaxValue;
            int sampleRate = stream.SampleRate;
            AudioClip newClip = AudioClip.Create(clipPath, (int)clipSize, stream.Channels, sampleRate, true, Mp3Callback);
            aSource.clip = newClip;
        }
        catch(FileNotFoundException)
        {
            mac.ShowErrorMessage("File not found! Was it deleted?");
        }

    }

    void StreamOggFile()
    {
        if (vorbis != null) vorbis.Dispose();
        try
        {
            vorbis = new NVorbis.VorbisReader(clipPath);
            long clipSize = vorbis.TotalSamples;
            int sampleRate = vorbis.SampleRate;
            AudioClip newClip = AudioClip.Create(clipPath, (int)clipSize, 2, sampleRate, true, VorbisCallback);
            vorbisPosition = TimeSpan.Zero;
            aSource.clip = newClip;
        }
        catch(FileNotFoundException)
        {
            mac.ShowErrorMessage("File not found! Was it deleted?");
        }

    }

    void VorbisCallback(float[] data)
    {
        //Debug.Log(data.Length);
        vorbis.ReadSamples(data, 0, data.Length);
        vorbisCount += data.Length;
        vorbisPosition = vorbis.DecodedTime;
    }

    private void Mp3Callback(float[] data)
    {
        stream.ReadSamples(data, 0, data.Length);
    }

    public void ChangeMasterVolume(float newMasterVolume)
    {
        masterVolume = newMasterVolume;
        aSource.volume = LocalVolume * masterVolume;
    }

    void Update()
    {
        if(rectWidth < 0)
        {
            rectWidth = GetComponent<RectTransform>().sizeDelta.x;
        }
        if (Input.GetMouseButtonDown(1) && hasPointer)
        {
            bec.StartEditing(id);

        }
        if (!aSource.isPlaying && bgImage.color == ResourceManager.green && !Loop) Stop();
        if(!aSource.isPlaying && Loop && !waiting && play)
        {
            StartCoroutine("WaitForLoopDelay");
        }
        if(aSource.isPlaying)
        {
            float percentPlayed = (aSource.time / aSource.clip.length);
            playbackBarRect.sizeDelta = new Vector2((percentPlayed * rectWidth), playbackBarRect.rect.height);
        }
        if(waiting && play)
        {

            float percentWaited = ((Time.time - waitStartedTime) / timeToWait);
            playbackBarRect.sizeDelta = new Vector2((percentWaited * rectWidth), playbackBarRect.rect.height);
        }

        //prevent playback bar from showing after clip has been removed
        if (string.IsNullOrEmpty(clipPath) && playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(false);
        else if (!string.IsNullOrEmpty(clipPath) && !playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(true);
    }



    IEnumerator WaitForLoopDelay()
    {
        waiting = true;
        waitStartedTime = Time.time;
        Image rect = playBackBar.GetComponent<Image>();
        rect.color = ResourceManager.black;

        timeToWait = RandomizeLoopDelay ? UnityEngine.Random.Range(MinLoopDelay, MaxLoopDelay) : MinLoopDelay;
        while (timeToWait + waitStartedTime > Time.time)
        {
            if (!play) break;
            yield return new WaitForEndOfFrame();
        }

        if (play)
        {
            Play();
            rect.color = ResourceManager.red;
        }
        waiting = false;
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System;
using NLayer;

public class SFXButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    internal string clipID = null;
    internal int page;
    internal int id;
    private ButtonEditorController bec;
    public FileSelectViewController vc;
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

    internal string Label { 
        get
        {
            return label;
        }
        set 
        {
            label = value;
            //TMPLabel = GetComponentInChildren<TMP_Text>();
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
            localVolume = value;
            //volumeSlider = GetComponentInChildren<Slider>();
            volumeSlider.value = value;
        }
    }

    public bool Loop { get; set; } = false;
    public float MinLoopDelay { get; set; } = 0;
    public float MaxLoopDelay { get; set; } = 0;
    public bool RandomizeLoopDelay { get; set; } = false;
    void Start()
    {
        //Debug.Log("Start");

        TMPLabel = GetComponentInChildren<TMP_Text>();
        //if (id == 1 && page == 0)
        //{
        //    Debug.Log(TMPLabel.name);
        //    TMPLabel.SetText("hello");
        //}
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

        volumeSlider.onValueChanged.AddListener(ChangeLocalVolume);
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
        Debug.Log("Vol Changed");
        LocalVolume = newLocalVol;
        aSource.volume = LocalVolume * masterVolume;
    }

    public void Stop()
    {
        //if(stream != null) stream.Dispose();
        play = false;
        bgImage.color = ResourceManager.transWhite;
        aSource.Stop();
    }

    public void Play()
    {
        //if (stream != null) stream.Dispose();

        if(!string.IsNullOrEmpty(clipID))
        {
            if (clipID.Contains(".mp3"))
            {
                StreamMP3File();
                PlayValidFile();
            }
            else if (clipID.Contains(".ogg"))
            {
                StreamOggFile();
                PlayValidFile();
            }
            else
            {
                //TODO: Error message for unsupported file type;
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
        stream = new MpegFile(clipID);
        int clipSize = (int)(stream.Length * 2.77f); //Clip length does not equal an integer product of samples for some reason?
        int sampleRate = stream.SampleRate;
        AudioClip newClip = AudioClip.Create(clipID, clipSize, 2, sampleRate, true, Mp3Callback);
        aSource.clip = newClip;
    }

    void StreamOggFile()
    {
        if (vorbis != null) vorbis.Dispose();
        vorbis = new NVorbis.VorbisReader(clipID);
        int clipSize = (int)vorbis.TotalSamples;
        int sampleRate = vorbis.SampleRate;
        AudioClip newClip = AudioClip.Create(clipID, clipSize, 2, sampleRate, true, VorbisCallback);

        vorbisPosition = TimeSpan.Zero;
        aSource.clip = newClip;
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
        if (!aSource.isPlaying && bgImage.color == ResourceManager.green && !Loop) bgImage.color = ResourceManager.transWhite;
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
        if (string.IsNullOrEmpty(clipID) && playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(false);
        else if (!string.IsNullOrEmpty(clipID) && !playbackBarRect.gameObject.activeSelf) playbackBarRect.gameObject.SetActive(true);
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

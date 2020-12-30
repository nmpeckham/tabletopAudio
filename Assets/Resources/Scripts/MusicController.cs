using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;
using NLayer;
using NVorbis;
using System.Net.Http.Headers;
using UnityEngine.Rendering;
using UnityEngine.Video;

//Controls the playing of songs in the playlist
public class MusicController : MonoBehaviour
{

    private MainAppController mac;

    public GameObject playlistItemPrefab;
    public GameObject musicScrollView;
    public GameObject playlistRightClickMenuPrefab;

    public TMP_Text nowPlayingLabel;
    private Image prevButtonImage = null;
    private string songPath = "";
    private string songName = "";
    internal int nowPlayingButtonID = -1;
    private int toDeleteId = -1;

    private float musicVolume = 1f;
    private float masterVolume = 1f;

    public Button fadeInButton;
    public Button fadeOutButton;
    public Slider localVolumeSlider;
    public Slider playbackScrubber;

    internal bool isPaused = false;
    public TMP_Text localVolumeLabel;

    public List<GameObject> musicButtons;
    public GameObject musicButtonContentPanel;
    public Image musicStatusImage;
    public TMP_Text playbackTimerText;
    public Image shuffleImage;
    private bool shuffle;
    public Image pauseButton;
    public Image playButton;
    private bool crossfade = false;

    public Material crossfadeMaterial;

    private VolumeController vc;

    private bool autoCheckForNewFiles = false;

    internal AudioSource activeAudioSource;
    private AudioSource inactiveAudioSource;


    MpegFile activeMp3Stream;
    MpegFile inactiveMp3Stream;

    VorbisReader activeVorbisStream;
    VorbisReader inactiveVorbisStream;

    int buttonWithCursor;
    private GameObject activeRightClickMenu;

    public GameObject fftParent;
    public FftBar[] pieces;
    private Material[] fftBarMaterials;

    public GameObject TooltipParent;


    private const float fixedUpdateStep = 0.02f;
    private const float fixedUpdateTime = 1f / fixedUpdateStep;
    private float crossfadeTime = 5f;
    private float crossfadeValue;
    public PlaylistAudioSources plas;

    bool useInactiveMp3Callback = true;
    bool useInactiveOggCallback = true;

    bool shouldStop1 = false;
    bool shouldStop2 = false;

    bool usingInactiveAudioSource = true;

    double[] fadeTargets;

    bool fileTypeIsMp3 = false;
    OptionsMenuController omc;

    bool mono = false;

    int numCoroutines = 0;
    Coroutine coroutine = null;

    private int nextDelayTimer = 0;

    private float[] prevFFTmesurement = new float[8]; // values, frames ago
    private float[] oldScale = new float[8];
    private int fftReadingIndex = 0;

    //public VideoPlayer vp;
    // private AudioSource videoSource;
    //private AudioSource videoAudioSource;

    private long totalRead = 0;
    private bool fadeInMusicActive = false;
    private bool fadeOutMusicActive = false;
    public float CrossfadeTime
    {
        get { return (int)crossfadeTime; }
        set {
            crossfadeTime = value;
            crossfadeValue = 1 / (value * fixedUpdateTime);
        }
    }
    public int ButtonWithCursor
    {
        get
        {
            return buttonWithCursor;
        }
        set
        {
            if (buttonWithCursor != value)
            {
                buttonWithCursor = value;
            }
        }
    }
    public float MusicVolume
    {
        get
        {
            return musicVolume;
        }

        set
        {
            ChangeLocalVolume(value);
            musicVolume = value;
        }
    }
    public float MasterVolume
    {
        get
        {
            return masterVolume;
        }
        set
        {
            vc.VolumeChanged(value);
            masterVolume = value;
        }
    }
    public bool AutoCheckForNewFiles
    {
        get
        {
            return autoCheckForNewFiles;
        }

        set
        {
            LoadedFilesData.deletedMusicClips.Clear();
            autoCheckForNewFiles = value;
        }
    }
    public bool Shuffle {
        get
        {
            return shuffle;
        }
        set
        {
            shuffle = value;
            if (shuffle) shuffleImage.color = ResourceManager.green;
            else shuffleImage.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white;
        }
    }

    public bool Crossfade
    {
        get
        {
            return crossfade;
        }
        set
        {
            crossfade = value;
            if (crossfade) crossfadeMaterial.SetColor("ButtonColor", Color.green);
            else crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadedFilesData.deletedMusicClips.Clear();
        LoadedFilesData.musicClips.Clear();
        LoadedFilesData.sfxClips.Clear();
        plas = GetComponent<PlaylistAudioSources>();
        activeAudioSource = GetComponent<AudioSource>();
        crossfadeValue = 1 / (crossfadeTime * fixedUpdateTime);
        //activeAudioSource = plas.a1;
        inactiveAudioSource = plas.a2;
        omc = GetComponent<OptionsMenuController>();

        pieces = fftParent.GetComponentsInChildren<FftBar>();
        fftBarMaterials = new Material[pieces.Length];

        int i = 0;
        foreach (FftBar bar in pieces)
        {
            fftBarMaterials[i] = bar.gameObject.GetComponent<Image>().material;
            i++;
        }

        fadeTargets = new double[pieces.Length];
        mac = Camera.main.GetComponent<MainAppController>();
        buttonWithCursor = -1;
        vc = GetComponent<VolumeController>();
        musicButtons = new List<GameObject>();
        StartCoroutine("CheckForNewFiles");
        localVolumeSlider.onValueChanged.AddListener(LocalVolumeSliderChanged);
        playbackScrubber.onValueChanged.AddListener(PlaybackTimeValueChanged);
        fadeInButton.onClick.AddListener(StartFadeInMusicVolume);
        fadeOutButton.onClick.AddListener(StartFadeOutMusicVolume);
        StartCoroutine(AdjustScale());
        StartCoroutine(Fft());

        crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);
        GetComponent<GenerateMusicFFTBackgrounds>().Begin();

        //ItemSelected(0);
        mac.PlayVideo();
        StartCoroutine(StartAudioDelayed());
    }

    IEnumerator StartAudioDelayed()
    {
        ItemSelected(0);
        //ItemSelected(0);
        //for (int i = 0; i < 60; i++)
        //{
        //    yield return new WaitForEndOfFrame();
        //    ItemSelected(0);
        //}
        yield return null;
    }

    void StartFadeInMusicVolume()
    {
        StopCoroutine("FadeInMusicVolume");
        StopCoroutine("FadeOutMusicVolume");

        StartCoroutine("FadeInMusicVolume");
    }

    void StartFadeOutMusicVolume()
    {
        StopCoroutine("FadeInMusicVolume");
        StopCoroutine("FadeOutMusicVolume");

        StartCoroutine("FadeOutMusicVolume");
    }

    IEnumerator FadeInMusicVolume()
    {
        if (fadeInMusicActive)
        {
            fadeInMusicActive = false;
            StopCoroutine("FadeInMusicVolume");
        }
        else
        {
            fadeInMusicActive = true;
            fadeOutMusicActive = false;
            float fadeInValue = 1 / (crossfadeTime * fixedUpdateTime);
            while (MusicVolume < 1f)
            {
                ChangeLocalVolume(musicVolume + fadeInValue);
                yield return new WaitForFixedUpdate();
            }
        }
        fadeOutMusicActive = false;
        yield return null;
    }

    IEnumerator FadeOutMusicVolume()
    {
        if (fadeOutMusicActive)
        {
            fadeOutMusicActive = false;
            StopCoroutine("FadeOutMusicVolume");
        }
        else
        {
            fadeOutMusicActive = true;
            fadeInMusicActive = false;
            float fadeOutValue = 1 / (crossfadeTime * fixedUpdateTime);
            while (MusicVolume > 0f)
            {
                ChangeLocalVolume(musicVolume - fadeOutValue);
                yield return new WaitForFixedUpdate();
            }
        }
        fadeOutMusicActive = false;
        yield return null;
    }

    IEnumerator Fft()
    {
        print(activeAudioSource.gameObject.name);
        // Much of this is based on making the fft display _look_ nice, rather than to be mathematically correct
        int fftSize = 4096;
        float[] data0 = new float[fftSize];
        float[] data1 = new float[fftSize];
        int[] segments = new int[9] {0, 24, 64, 128, 256, 512, 1024, 2048, 4096}; // Don't ask...
        while (true)
        {
            activeAudioSource.GetSpectrumData(data0, 0, FFTWindow.BlackmanHarris);
            if(!mono) { activeAudioSource.GetSpectrumData(data1, 1, FFTWindow.BlackmanHarris); }
            //videoAudioSource.GetSpectrumData(data0, 0, FFTWindow.BlackmanHarris);
            //videoAudioSource.GetSpectrumData(data1, 1, FFTWindow.BlackmanHarris);
            // mono audio is determined in ItemSelected() once for each clip

            for (int i = 0; i < 8; i++)
            {
                double sum = 0;
                for (int j = segments[i]; j < segments[i + 1]; j++)
                {
                    sum += data0[j];
                    if (!mono) sum += data1[j]; // only add from second track if stereo

                }
                //if (i == 0) sum *= 0.75; // correct for first band being larger than it "should" be
                //if (i == 7) sum *= 1.4; // correct for last band being smaller than it "should" be
                sum *= mono ? 1f : 0.5f;    // mono vs. stereo amplitude correction
                sum = Mathf.Pow((float)sum, 0.75f); // make low sounds show as a little louder
                //sum *= 1.5;

                fadeTargets[i] = sum;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator AdjustScale()
    {
        print(pieces.Length);
        float newScale;
        while (true)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                Transform obj = pieces[i].transform;

                prevFFTmesurement[i] = oldScale[i];
                oldScale[i] = obj.localScale.y;

                if(fadeTargets[i] > oldScale[i])
                {
                    newScale = ((float)fadeTargets[i] * 0.8f) + (oldScale[i] * 0.2f);
                }
                else
                {
                    newScale = (float)(fadeTargets[i] * 0.6f) + (oldScale[i] * 0.25f) + (prevFFTmesurement[i] * 0.15f); //average slightly over 3 frames
                }
                obj.localScale = new Vector3(1, Mathf.Min(newScale, 1), 1);

                //set var in shader to adjust texture height
                fftBarMaterials[i].SetFloat("Height", Mathf.Min(newScale, 1));

            }
            yield return new WaitForEndOfFrame();
        }
    }

    internal void InitLoadFiles(List<string> files = null)
    {
        if (files == null)
        {
            files = new List<string>();
            int attempts = 0;
            while (true)
            {
                if (System.IO.Directory.Exists(mac.musicDirectory))
                {
                    foreach (string s in System.IO.Directory.GetFiles(mac.musicDirectory))
                    {
                        if ((Path.GetExtension(s) == ".mp3" || Path.GetExtension(s) == ".ogg"))
                        {
                            files.Add(s);
                        }
                    }
                    break;
                }
                attempts++;
                if (attempts > 100)
                {
                    mac.ShowErrorMessage("Directory setup failed. Please inform the developer.");
                    break;
                }
            }
        }

        foreach (string s in files)
        {
            if (!LoadedFilesData.musicClips.Contains(s))
            {
                LoadedFilesData.musicClips.Add(s);
                GameObject listItem = Instantiate(playlistItemPrefab, musicScrollView.transform);
                listItem.GetComponentInChildren<TMP_Text>().text = s.Replace(mac.musicDirectory + mac.sep, "");
                listItem.GetComponent<MusicButton>().id = LoadedFilesData.musicClips.Count - 1;
                listItem.GetComponent<MusicButton>().FileName = s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "");
                musicButtons.Add(listItem);
            }
        }
    }
    IEnumerator CheckForNewFiles()
    {
        List<GameObject> toDelete = new List<GameObject>();
        while (true)
        {
            string[] files = System.IO.Directory.GetFiles(mac.musicDirectory);
            if (autoCheckForNewFiles)
            {
                foreach (string s in LoadedFilesData.musicClips)
                {
                    if (!files.Contains(Path.Combine(mac.musicDirectory, s)))
                    {
                        toDelete.Add(musicButtons[LoadedFilesData.musicClips.IndexOf(s)]);
                    }
                }
                foreach (GameObject g in toDelete)
                {
                    LoadedFilesData.musicClips.Remove(g.GetComponent<MusicButton>().FileName);
                    musicButtons.Remove(g);
                    Destroy(g);
                }
                toDelete.Clear();
                foreach (string s in files)
                {
                    
                    if (!LoadedFilesData.musicClips.Contains(s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "")) && (Path.GetExtension(s) == ".mp3" || Path.GetExtension(s) == ".ogg") && !LoadedFilesData.deletedMusicClips.Contains(s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "")))
                    {
                        LoadedFilesData.musicClips.Add(s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, ""));
                        GameObject listItem = Instantiate(playlistItemPrefab, musicScrollView.transform);
                        listItem.GetComponentInChildren<TMP_Text>().text = s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "");
                        listItem.GetComponent<MusicButton>().id = LoadedFilesData.musicClips.Count - 1;
                        listItem.GetComponent<MusicButton>().FileName = s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "");
                        musicButtons.Add(listItem);
                    }
                }
                int id = 0;
                foreach (GameObject g in musicButtons)
                {
                    g.GetComponent<MusicButton>().id = id;
                    id++;
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

    public void ItemSelected(int id)
    {
        print("itemSelected");
        if (nowPlayingButtonID == id && activeAudioSource.isPlaying)
        {
            if(inactiveVorbisStream != null) inactiveVorbisStream.DecodedTime = TimeSpan.FromSeconds(0);
            else if(inactiveMp3Stream != null) inactiveMp3Stream.Time = TimeSpan.FromSeconds(0);
            else if (activeMp3Stream != null) activeMp3Stream.Time = TimeSpan.FromSeconds(0);
            else if (activeVorbisStream != null) activeVorbisStream.DecodedTime = TimeSpan.FromSeconds(0);
            activeAudioSource.time = 0;
        }
        else
        {
            if (musicButtons.Count > 0)
            {
                try
                {
                    nowPlayingButtonID = id;
                    MusicButton button = musicButtons[nowPlayingButtonID].GetComponent<MusicButton>();
                    songPath = System.IO.Path.Combine(mac.musicDirectory, button.FileName);
                    songName = button.FileName;
                    AudioClip clip = null;
                    long totalLength = 0;
                    if (crossfade)
                    {
                        if (Path.GetExtension(songPath) == ".mp3")
                        {
                            fileTypeIsMp3 = true;
                            if (useInactiveMp3Callback)
                            {
                                inactiveMp3Stream = new MpegFile(songPath);
                                totalLength = inactiveMp3Stream.Length / (inactiveMp3Stream.Channels * 4);
                                if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                                clip = AudioClip.Create(songPath, (int)totalLength, inactiveMp3Stream.Channels, inactiveMp3Stream.SampleRate, true, InactiveMP3Callback);

                            }
                            else
                            {
                                activeMp3Stream = new MpegFile(songPath);
                                totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                                if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                                clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                            }
                            useInactiveMp3Callback = !useInactiveMp3Callback;
                            SetupInterfaceForPlay(inactiveAudioSource, clip);
                            usingInactiveAudioSource = !usingInactiveAudioSource;
                        }
                        else if (Path.GetExtension(songPath) == ".ogg")
                        {
                            fileTypeIsMp3 = false;
                            if (useInactiveOggCallback)
                            {
                                inactiveVorbisStream = new NVorbis.VorbisReader(songPath);
                                totalLength = inactiveVorbisStream.TotalSamples;
                                if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                                clip = AudioClip.Create(songPath, (int)totalLength, inactiveVorbisStream.Channels, inactiveVorbisStream.SampleRate, true, InactiveVorbisCallback);

                            }
                            else
                            {
                                activeVorbisStream = new NVorbis.VorbisReader(songPath);
                                totalLength = activeVorbisStream.TotalSamples;
                                if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                                clip = AudioClip.Create(songPath, (int)totalLength, activeVorbisStream.Channels, activeVorbisStream.SampleRate, true, ActiveVorbisCallback);
                            }
                            useInactiveOggCallback = !useInactiveOggCallback;
                            SetupInterfaceForPlay(inactiveAudioSource, clip);
                            usingInactiveAudioSource = !usingInactiveAudioSource;
                        }
                        else
                        {
                            activeAudioSource.clip = null;
                        }
                    }
                    else
                    {
                        if (Path.GetExtension(songPath) == ".mp3")
                        {
                            fileTypeIsMp3 = true;
                            activeMp3Stream = new MpegFile(songPath);
                            totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                            SetupInterfaceForPlay(activeAudioSource, clip);
                        }
                        else if (Path.GetExtension(songPath) == ".ogg")
                        {
                            fileTypeIsMp3 = false;
                            activeVorbisStream = new NVorbis.VorbisReader(songPath);
                            totalLength = activeVorbisStream.TotalSamples;
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, activeVorbisStream.Channels, activeVorbisStream.SampleRate, true, ActiveVorbisCallback);
                            SetupInterfaceForPlay(activeAudioSource, clip);
                        }
                        else
                        {
                            activeAudioSource.clip = null;
                        }
                    }
                    playButton.color = ResourceManager.green;
                    pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

                    // Determine if audio is mono or stereo
                    // In testing this doesn't seem to work though :/
                    mono = false;
                    float[] data = new float[64];

                    try
                    {
                        activeAudioSource.GetSpectrumData(data, 1, FFTWindow.Rectangular);
                    }
                    catch (ArgumentException) { mono = true; }

                }
                catch (IndexOutOfRangeException e)
                {
                    mac.ShowErrorMessage("Encoding Type Invalid: 0. " + e.Message);
                }
                catch (ArgumentException e)
                {
                    mac.ShowErrorMessage("Encoding Type Invalid: 1. " + e.Message);
                }
            }
        }
    }

    internal void ClearPlaylist()
    {
        foreach (GameObject mb in musicButtons)
        {
            Destroy(mb);
        }
    }

    void SetupInterfaceForPlay(AudioSource aSource, AudioClip clip = null)
    {
        if (crossfade)
        {
            AudioSource temp = inactiveAudioSource;
            inactiveAudioSource = activeAudioSource;
            activeAudioSource = temp;
        }
        aSource.clip = clip;
        aSource.time = 0;
        aSource.Play();
        if (crossfade)
        {
            // Just using StopCoroutine(CrossfadeAudioSources()) here simply _does not_ work. Don't know why. This works.
            try
            {
                if(coroutine != null) StopCoroutine(coroutine);
            }
            catch (Exception e) {
                print(e);
            }

            coroutine = StartCoroutine(CrossfadeAudioSources());
        }

        playbackScrubber.SetValueWithoutNotify(0);
        Image buttonImage = musicButtons[nowPlayingButtonID].GetComponent<Image>();
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.musicButtonGrey;
        buttonImage.color = ResourceManager.red;

        prevButtonImage = buttonImage;
        nowPlayingLabel.text = songName;
        musicStatusImage.sprite = mac.playImage;
    }

    IEnumerator CrossfadeAudioSources()
    {
        int counter = 0;
        float maxVolume = MusicVolume * MasterVolume;
        activeAudioSource.volume = 0;
        float changeVolumeAmount = Mathf.Lerp(0, maxVolume, crossfadeValue);
        crossfadeMaterial.SetFloat("Progress", 0);
        numCoroutines++;
        while (true)
        {
            if (numCoroutines > 1)
            {
                // Will be encountered if a current crossfade coroutine is already running. Exit if true
                print("coroutines greater than 1");
                numCoroutines--;
                yield return null;
            }
            else
            {
                if (counter % 20 == 0)
                {
                    if (counter % 40 == 0) crossfadeMaterial.SetColor("ButtonColor", Color.green);
                    else crossfadeMaterial.SetColor("ButtonColor", Color.red);
                }
                activeAudioSource.volume += changeVolumeAmount;
                inactiveAudioSource.volume -= changeVolumeAmount;

                // can become NaN if master volume is 0
                float newXScale = activeAudioSource.volume / maxVolume;
                if (float.IsNaN(newXScale))
                {
                    newXScale = 0;
                }

                float currentScale = crossfadeMaterial.GetFloat("Progress");
                crossfadeMaterial.SetFloat("Progress", newXScale);

                // If volume is adjusted during fade, exit early
                if (activeAudioSource.volume >= MusicVolume * MasterVolume) break;
                counter++;
                // Shit's broke
                if (counter > 5000)
                {
                    Debug.Log("Breaking");
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
        }
        crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);
        activeAudioSource.volume = MusicVolume * MasterVolume;
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;

        yield return null;
    }

    void ActiveMP3Callback(float[] data)
    {
        try
        {
            //print(activeMp3Stream.Channels);
            //print("Buffer size: " + data.Length);
            int samples = activeMp3Stream.ReadSamples(data, 0, data.Length);
            totalRead += samples;
            //print("total Read: " + totalRead);
            //print("Samples read this iter: " + samples);
            //print("Total length: " + activeMp3Stream.Length);
        }
        catch (NullReferenceException e)
        {
            shouldStop1 = true;
            mac.ShowErrorMessage("Error decoding audio from active MP3 stream, stopping playback");
            print(e);
        }
        catch (IndexOutOfRangeException e)
        {
            StartCoroutine(DelayAndPlayNext());
            print(e);
        }
    }

    void InactiveMP3Callback(float[] data)
    {
        try
        {
            inactiveMp3Stream.ReadSamples(data, 0, data.Length);
        }
        catch (NullReferenceException e)
        {
            shouldStop2 = true;
            mac.ShowErrorMessage("Error decoding audio from inactive MP3 stream, stopping playback");
            print(e);
        }
        catch (IndexOutOfRangeException e)
        {
            StartCoroutine(DelayAndPlayNext());
            print(e);
        }
    }

    void ActiveVorbisCallback(float[] data)
    {
        activeVorbisStream.ReadSamples(data, 0, data.Length);
    }

    void InactiveVorbisCallback(float[] data)
    {
        inactiveVorbisStream.ReadSamples(data, 0, data.Length);
    }

    public void Next()
    {
        StopCoroutine(CrossfadeAudioSources());
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;
        if (shuffle)
        {
            int newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
            while(nowPlayingButtonID == newButtonID)
            {
                newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
            }
            ItemSelected(newButtonID);
        }
        else
        {
            if (nowPlayingButtonID == musicButtons.Count - 1)
            {
                ItemSelected(0);
            }
            else
            {
                ItemSelected(nowPlayingButtonID+1);
            }
        }
        playbackScrubber.SetValueWithoutNotify(0);
        pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

    }

    public void Previous()
    {
        StopCoroutine(CrossfadeAudioSources());
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;

        if (musicButtons.Count > 0)
        {
            if (shuffle)
            {
                int newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
                while (nowPlayingButtonID == newButtonID)
                {
                    newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
                }
                ItemSelected(newButtonID);
            }

            else
            {
                if (nowPlayingButtonID > 0)
                {
                    ItemSelected(nowPlayingButtonID - 1);
                }
                else
                {
                    ItemSelected(musicButtons.Count - 1);
                }
            }
            playbackScrubber.SetValueWithoutNotify(0);
            pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

        }
    }

    public void Stop()
    {
        activeAudioSource.Stop();
        inactiveAudioSource.Stop();
        StopCoroutine(CrossfadeAudioSources());
        isPaused = false;
        activeAudioSource.clip = null;
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.musicButtonGrey;
        musicStatusImage.sprite = mac.stopImage;
        nowPlayingLabel.text = "";
        playButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
        pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
    }

    public void Pause()
    {
        if (isPaused)
        {
            Play();
        }
        else if(activeAudioSource.isPlaying)
        {
            pauseButton.color = ResourceManager.orange;
            activeAudioSource.Pause();
            isPaused = true;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.orange;
            musicStatusImage.sprite = mac.pauseImage;
        }
    }

    public void SpacebarPressed()
    {
        if (isPaused) Play();
        else if (activeAudioSource.clip == null) ItemSelected(0);
        else Pause();
    }

    public void Play()
    {
        if (activeAudioSource.clip != null)
        {
            activeAudioSource.UnPause();
            isPaused = false;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.red;
            musicStatusImage.sprite = mac.playImage;
            pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

        }
        else if (shuffle)
        {
            nowPlayingButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count - 1);
            ItemSelected(nowPlayingButtonID);
        }
        else
        {
            ItemSelected(0);
        }
    }

    public void RefreshSongOrder(int oldID, int newID)
    {
        musicButtons[oldID].GetComponent<MusicButton>().id = newID;
        musicButtons[newID].GetComponent<MusicButton>().id = oldID;
        GameObject item = musicButtons[oldID];
        if(oldID == nowPlayingButtonID) nowPlayingButtonID = newID;
        musicButtons.Remove(item);
        musicButtons.Insert(newID, item);
    }

    public void ChangeMasterVolume(float newMasterVolume)
    {
        masterVolume = newMasterVolume;
        activeAudioSource.volume = masterVolume * musicVolume;
    }

    private void LocalVolumeSliderChanged(float value)
    {
        StopCoroutine("FadeInMusicVolume");
        StopCoroutine("FadeOutMusicVolume");
        ChangeLocalVolume(value);
    }

    private void ChangeLocalVolume(float newLocalVolume)
    {
        if (localVolumeSlider.value != newLocalVolume) localVolumeSlider.SetValueWithoutNotify(newLocalVolume);
        musicVolume = newLocalVolume;
        activeAudioSource.volume = masterVolume * musicVolume;
        localVolumeLabel.text = (musicVolume * 100).ToString("N0") + "%";
    }

    private void PlaybackTimeValueChanged(float val)
    {
        //print(val);
        float value = Mathf.Clamp(val, 0f, 1f);
        if (Math.Abs(value) > .995f)
        {
            StartCoroutine(DelayAndPlayNext());
        }
        else try
            {
                TryChangePlaybackTime(value);
            }
            catch (IndexOutOfRangeException e)
            {
                print(e);
            }
        //nextDelayTimer = 1;

    }

    private void TryChangePlaybackTime(float val)
    {
        bool error = false;
        if (nextDelayTimer == 0 && val > 0)
        {
            nextDelayTimer = 1;
            if (fileTypeIsMp3)
            {

                MpegFile streamToUse = useInactiveMp3Callback ? activeMp3Stream : inactiveMp3Stream;
                try
                {
                    long position = Convert.ToInt64(streamToUse.Length * val);
                    if (streamToUse != null && position > 0) streamToUse.Position = position -520000;
                    mac.ChangeVideoTime(val);
                }
                catch (NullReferenceException e)
                {
                    print(e);
                    error = true;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    print(e);
                    error = true;
                }
                if (!error)
                {
                    try
                    {
                        if (activeAudioSource)
                        {
                            activeAudioSource.time = Mathf.Min(val, 0.999f) * activeAudioSource.clip.length;
                        }
                    }
                    catch (Exception)
                    {
                        print("Magical exception. Be afraid");
                    }
                }
            }
            else
            {
                NVorbis.VorbisReader streamToUse = useInactiveOggCallback ? activeVorbisStream : inactiveVorbisStream;
                try
                {
                    if (streamToUse != null) streamToUse.DecodedPosition = Convert.ToInt64(streamToUse.TotalSamples * val);
                }
                catch (NullReferenceException e)
                {
                    error = true;
                    print(e);
                }
                if (!error) activeAudioSource.time = val * activeAudioSource.clip.length;

            }
        }
        if(error)
        {
            DelayAndPlayNext();
        }
    }

    IEnumerator CheckMousePos(Vector3 mousePos)
    {
        while (activeRightClickMenu)
        {
            float yDelta = Input.mousePosition.y - mousePos.y;
            float xDelta = Input.mousePosition.x - mousePos.x;
            if (yDelta < -10 || yDelta > 40 || xDelta < -10 || xDelta > 80)
            {
                Destroy(activeRightClickMenu);
                mac.currentMenuState = MainAppController.MenuState.mainAppView;
                break;
            }
            else if (mac.currentMenuState == MainAppController.MenuState.mainAppView && Input.GetKey(KeyCode.Escape))
            {
                Destroy(activeRightClickMenu);
                mac.currentMenuState = MainAppController.MenuState.mainAppView;
                break;
            }

            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    // My code is _so_ good that it only needs two frames to catch up and not crash because of nLayer
    IEnumerator DelayAndPlayNext()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Next();
        nextDelayTimer = 1;
    }

    public void CloseDeleteMusicItemTooltip()
    {
        Destroy(activeRightClickMenu);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    public void DeleteItem()
    {
        if (nowPlayingButtonID == toDeleteId)
        {
            Stop();
            nowPlayingButtonID = -1;
        }
        LoadedFilesData.deletedMusicClips.Add(LoadedFilesData.musicClips[toDeleteId]);
        LoadedFilesData.musicClips.Remove(LoadedFilesData.musicClips[toDeleteId]);
        Destroy(musicButtons[toDeleteId]);
        musicButtons.RemoveAt(toDeleteId);
        int currentID = 0;
        foreach (GameObject mbObj in musicButtons)
        {
            MusicButton mb = mbObj.GetComponent<MusicButton>();
            mb.id = currentID;
            currentID++;
        }
        Destroy(activeRightClickMenu);
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldStop1)
        {
            Stop();
            shouldStop1 = false;
        }
        if (shouldStop2)
        {
            Stop();
            shouldStop2 = false;
        }
        
        if(activeAudioSource.clip != null)
        {
            playbackScrubber.SetValueWithoutNotify(activeAudioSource.time / activeAudioSource.clip.length);
            playbackTimerText.text = Mathf.Floor(activeAudioSource.time / 60).ToString() + ":" + (Mathf.FloorToInt(activeAudioSource.time % 60)).ToString("D2") + "/" + Mathf.FloorToInt(activeAudioSource.clip.length / 60) + ":" + Mathf.FloorToInt(activeAudioSource.clip.length % 60).ToString("D2");
        }
    }

    private void LateUpdate()
    {
        bool shouldStartCrossfade = false;
        try
        {
            if (activeAudioSource.clip)
            {
                shouldStartCrossfade = activeAudioSource.time > activeAudioSource.clip.length - crossfadeTime;
            }
            if (activeAudioSource.clip)
            {
                if (((!activeAudioSource.isPlaying && !isPaused) || (crossfade && shouldStartCrossfade)))
                {
                    prevButtonImage.color = ResourceManager.musicButtonGrey;
                    if (musicButtons.Count > 1) //edge case if only one track is present in playlist
                    {
                        if (nowPlayingButtonID < musicButtons.Count - 1)
                        {
                            int newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : nowPlayingButtonID + 1;
                            while (newButtonID == nowPlayingButtonID)
                            {
                                newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : nowPlayingButtonID + 1;
                            }
                            ItemSelected(newButtonID);
                        }
                        else
                        {
                            int newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : 0;
                            while (newButtonID == nowPlayingButtonID)
                            {
                                newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : 0;
                            }
                            ItemSelected(newButtonID);
                        }
                    }
                    else
                    {
                        Stop();
                    }
                }
                if (nextDelayTimer > 0)
                {
                    nextDelayTimer -= 1;
                }
            }
        }
        catch (Exception) { };
        
    }
    public void ShowRightClickMenu(int id)
    {
        mac.currentMenuState = MainAppController.MenuState.deleteMusicFile;
        toDeleteId = id;
        if(activeRightClickMenu) Destroy(activeRightClickMenu);
        activeRightClickMenu = Instantiate(playlistRightClickMenuPrefab, Input.mousePosition, Quaternion.identity, TooltipParent.transform);
        StartCoroutine(CheckMousePos(Input.mousePosition));
    }

    private void OnApplicationQuit()
    {
        activeMp3Stream.Dispose();
        inactiveMp3Stream.Dispose();
        activeVorbisStream.Dispose();
        inactiveVorbisStream.Dispose();
    }
}

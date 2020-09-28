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

    public Slider localVolumeSlider;
    public Slider playbackScrubber;

    private bool isPaused = false;
    public TMP_Text localVolumeLabel;

    public List<GameObject> musicButtons;
    public GameObject musicButtonContentPanel;
    public Image musicStatusImage;
    public TMP_Text playbackTimerText;
    public Image shuffleImage;
    private bool shuffle;
    private Button crossfadeButton;
    private bool crossfade = false;
    public Image crossfadeImage;

    private VolumeController vc;

    private bool autoCheckForNewFiles = false;

    private AudioSource activeAudioSource;
    private AudioSource inactiveAudioSource;


    MpegFile activeMp3Stream;
    MpegFile inactiveMp3Stream;

    VorbisReader activeVorbisStream;
    VorbisReader inactiveVorbisStream;

    int buttonWithCursor;
    private GameObject activeRightClickMenu;

    public GameObject fftParent;
    public FftBar[] pieces;

    public GameObject TooltipParent;


    private const float fixedUpdateTime = 1f / .02f;
    private float crossfadeTime = 5f;
    private float crossfadeValue;
    public PlaylistAudioSources plas;

    bool useInactiveMp3Callback = true;
    bool useInactiveOggCallback = true;

    bool shouldStop1 = false;
    bool shouldStop2 = false;

    bool usingInactiveAudioSource = false;

    double[] fadeTargets;

    bool fileTypeIsMp3 = false;

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
            else shuffleImage.color = Color.white;
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
            if (crossfade) crossfadeImage.color = ResourceManager.green;
            else crossfadeImage.color = Color.white;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        crossfadeValue = 1 / (crossfadeTime * fixedUpdateTime);
        activeAudioSource = plas.a1;
        inactiveAudioSource = plas.a2;

        pieces = fftParent.GetComponentsInChildren<FftBar>();
        fadeTargets = new double[pieces.Length];
        mac = Camera.main.GetComponent<MainAppController>();
        buttonWithCursor = -1;
        vc = GetComponent<VolumeController>();
        musicButtons = new List<GameObject>();
        StartCoroutine("CheckForNewFiles");
        localVolumeSlider.onValueChanged.AddListener(ChangeLocalVolume);
        playbackScrubber.onValueChanged.AddListener(PlaybackTimeValueChanged);
        StartCoroutine(AdjustScale());
        StartCoroutine(Fft());
    }

    IEnumerator Fft()
    {
        int fftSize = 1024;
        float[] data0 = new float[fftSize];
        float[] data1 = new float[fftSize];
        int[] segments = new int[9] {0, 4, 16, 32, 64, 128, 256, 512, 1024};
        bool mono = false;
        while (true)
        {
            activeAudioSource.GetSpectrumData(data0, 0, FFTWindow.BlackmanHarris);
            try { activeAudioSource.GetSpectrumData(data1, 1, FFTWindow.BlackmanHarris); }
            catch (ArgumentException) { mono = true; }

            double sum;
            for (int i = 0; i < 8; i++)
            {
                sum = 0;
                for (int j = segments[i]; j < segments[i + 1]; j++)
                {
                    sum += data0[j];
                    if (!mono) sum += data1[j]; // only add from second track if stereo
                }
                sum *= mono ? 1f : 0.5f;    // mono vs. stereo amplitude correction
                 sum *= 0.9f; // scale down to reduce overall amplitude
                fadeTargets[i] = sum;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator AdjustScale()
    {
        while (true)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                Transform obj = pieces[i].transform;

                float oldScale = obj.localScale.y;
                float newScale = (float)(fadeTargets[i] * 0.75f) + (oldScale * 0.25f); //average slightly
                obj.localScale = new Vector3(1, Mathf.Min(newScale, 1), 1);

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
            if (autoCheckForNewFiles)
            {
                foreach (string s in LoadedFilesData.musicClips)
                {
                    string[] files = System.IO.Directory.GetFiles(mac.musicDirectory);
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
                foreach (string s in System.IO.Directory.GetFiles(mac.musicDirectory))
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
            nowPlayingButtonID = id;
            if (musicButtons.Count > 0)
            {
                try
                {
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
        if (crossfade) StartCoroutine(CrossfadeAudioSources());

        playbackScrubber.value = 0;
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
        while (true)
        {
            activeAudioSource.volume += crossfadeValue;
            inactiveAudioSource.volume -= crossfadeValue;


            if (activeAudioSource.volume >= MusicVolume) break;
            counter++;
            if (counter > 1500)
            {
                break;
            }
            yield return new WaitForFixedUpdate();

        }
        activeAudioSource.volume = MusicVolume;
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;
        inactiveAudioSource.Stop();
        yield return null;
    }

    void ActiveMP3Callback(float[] data)
    {
        try
        {
            activeMp3Stream.ReadSamples(data, 0, data.Length);
        }
        catch (NullReferenceException)
        {
            shouldStop1 = true;
            Debug.Log("NRE! Mp3 1");
            mac.ShowErrorMessage("Error decoding audio from active MP3 stream, stopping playback");
        }
    }

    void InactiveMP3Callback(float[] data)
    {
        try
        {
            inactiveMp3Stream.ReadSamples(data, 0, data.Length);
        }
        catch (NullReferenceException)
        {
            shouldStop2 = true;
            Debug.Log("NRE! Mp3 2");
            mac.ShowErrorMessage("Error decoding audio from inactive MP3 stream, stopping playback");
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
        inactiveAudioSource.Stop();
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
        playbackScrubber.value = 0;
    }

    public void Previous()
    {
        StopCoroutine(CrossfadeAudioSources());
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;
        inactiveAudioSource.Stop();

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
            playbackScrubber.value = 0;
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
    }

    public void Pause()
    {
        if (isPaused)
        {
            Play();
        }
        else
        {
            activeAudioSource.Pause();
            isPaused = true;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.orange;
            musicStatusImage.sprite = mac.pauseImage;
        }
    }

    public void SpacebarPressed()
    {
        if (isPaused)
        {
            Play();
        }
        else if (activeAudioSource.clip == null)
        {
            ItemSelected(0);
        }
        else
        {
            activeAudioSource.Pause();
            isPaused = true;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.orange;
            musicStatusImage.sprite = mac.pauseImage;
        }
    }

    public void Play()
    {
        if (activeAudioSource.clip != null)
        {
            activeAudioSource.UnPause();
            isPaused = false;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.red;
            musicStatusImage.sprite = mac.playImage;
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
        activeAudioSource.volume = MusicVolume;
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

    private void ChangeLocalVolume(float newLocalVolume)
    {
        if (localVolumeSlider.value != newLocalVolume) localVolumeSlider.value = newLocalVolume;
        musicVolume = newLocalVolume;
        activeAudioSource.volume = masterVolume * musicVolume;
        localVolumeLabel.text = (musicVolume * 100).ToString("N0");
    }

    private void PlaybackTimeValueChanged(float val)
    {

        if(fileTypeIsMp3)
        {
            try
            {
                MpegFile streamToUse = usingInactiveAudioSource ? inactiveMp3Stream : activeMp3Stream;
                if (Mathf.Abs(val - (activeAudioSource.time / activeAudioSource.clip.length)) > 0.005)
                {
                    if (streamToUse != null) streamToUse.Position = Convert.ToInt64(streamToUse.Length * val);
                    activeAudioSource.time = val * activeAudioSource.clip.length;
                }
            }
        catch (NullReferenceException)
            {
                Debug.Log(activeMp3Stream.Length);
                Debug.Log(inactiveMp3Stream.Length);
                Debug.Log("NRE Playback Time Changed");
                activeAudioSource.Stop();
            }
        }
        else
        {
            try
            {
                NVorbis.VorbisReader streamToUse = usingInactiveAudioSource ? inactiveVorbisStream : activeVorbisStream;
                if (Mathf.Abs(val - (activeAudioSource.time / activeAudioSource.clip.length)) > 0.01)
                {
                    if (streamToUse != null) streamToUse.DecodedPosition = Convert.ToInt64(streamToUse.TotalSamples * val);
                    activeAudioSource.time = val * activeAudioSource.clip.length;
                }
            }
            catch (NullReferenceException)
            {
                Debug.Log(activeMp3Stream.Length);
                Debug.Log(inactiveMp3Stream.Length);
                Debug.Log("NRE Playback Time Changed");
                activeAudioSource.Stop();

            }
        }

    }
    IEnumerator CheckMousePos(Vector3 mousePos)
    {
        while (true)
        {
            if (Vector3.Distance(mousePos, Input.mousePosition) > 80)
            {
                Destroy(activeRightClickMenu);
                break;
            }
            if (mac.currentMenuState == MainAppController.MenuState.none && Input.GetKey(KeyCode.Escape))
            {
                Destroy(activeRightClickMenu);
                break;
            }

            yield return new WaitForEndOfFrame();
        }
        yield return null;
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
            Debug.Log("stoping 1");
        }
        if (shouldStop2)
        {
            Stop();
            shouldStop2 = false;
            Debug.Log("stopping 2");
        }
        
        if(activeAudioSource.clip != null)
        {
            playbackScrubber.SetValueWithoutNotify(activeAudioSource.time / activeAudioSource.clip.length);
            playbackTimerText.text = Mathf.Floor(activeAudioSource.time / 60).ToString() + ":" + (Mathf.FloorToInt(activeAudioSource.time % 60)).ToString("D2") + "/" + Mathf.FloorToInt(activeAudioSource.clip.length / 60) + ":" + Mathf.FloorToInt(activeAudioSource.clip.length % 60).ToString("D2");
        }
    }

    private void FixedUpdate()
    {
        bool shouldStartCrossfade = false;
        if (activeAudioSource.clip)
        {
            shouldStartCrossfade = activeAudioSource.time > activeAudioSource.clip.length - crossfadeTime;
        }
        if ((!activeAudioSource.isPlaying && activeAudioSource.clip != null && !isPaused) || (crossfade && shouldStartCrossfade))
        {
            prevButtonImage.color = ResourceManager.musicButtonGrey;
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
    }
    public void ShowRightClickMenu(int id)
    {
        toDeleteId = id;
        if(activeRightClickMenu) Destroy(activeRightClickMenu);
        activeRightClickMenu = Instantiate(playlistRightClickMenuPrefab, Input.mousePosition, Quaternion.identity, TooltipParent.transform);
        StartCoroutine(CheckMousePos(Input.mousePosition));
    }
}

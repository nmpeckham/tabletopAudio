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
using System.Text.RegularExpressions;

//Controls the playing of songs in the playlist
public class MusicController : MonoBehaviour
{

    private MainAppController mac;

    public GameObject musicScrollView;

    public TMP_Text nowPlayingLabel;
    private Image prevButtonImage = null;
    private string songPath = "";
    private string songName = "";
    internal int nowPlayingButtonID = -1;

    private float musicVolume = 1f;
    private float masterVolume = 1f;
    private float actualVolume = 1f;

    public Button fadeInButton;
    public Button fadeOutButton;
    public Slider localVolumeSlider;
    public Slider playbackScrubber;

    internal bool isPaused = false;
    public TMP_Text localVolumeLabel;

    public List<GameObject> musicButtons;
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

    private AudioSource activeAudioSource;
    private AudioSource inactiveAudioSource;

    MpegFile activeMp3Stream;
    MpegFile inactiveMp3Stream;

    VorbisReader activeVorbisStream;
    VorbisReader inactiveVorbisStream;

    public GameObject fftParent;
    public FftBar[] pieces;
    private Material[] fftBarMaterials;

    //private const float fixedUpdateStep = 1 / 60f;
    private const float fixedUpdateTime = 60f;
    private float crossfadeTime = 0.01f;
    private float crossfadeValue;
    public PlaylistAudioSources plas;

    bool useInactiveMp3Callback = true;
    bool useInactiveOggCallback = true;

    bool shouldStop1 = false;
    bool shouldStop2 = false;

    bool usingInactiveAudioSource = true;

    double[] fadeTargets;

    bool fileTypeIsMp3 = false;
    bool mono = false;

    private int nextDelayTimer = 0;

    private float[] prevFFTmesurement = new float[8]; // values, frames ago
    private float[] oldScale = new float[8];

    private bool fadeInMusicActive = false;
    private bool fadeOutMusicActive = false;

    public TMP_InputField searchField;
    public Button clearPlaylistSearchButton;

    internal DiscoMode discoModeController;
    internal float discoModeMinSum = 0.45f;
    internal float discoModeNumFreq = 3;

    private List<Coroutine> crossfadeAudioCoroutines = new List<Coroutine>();

    private List<string> mostRecentSongs = new List<string>();

    bool shouldDelayPlayNext = false;

    private List<PlayNextItem> playNextList = new List<PlayNextItem>();

    private PlaylistTabs pt;

    internal PlaylistTab tabCurrentlyPlaying;

    private MusicButton[] searchButtons = null;

    public float CrossfadeTime
    {
        get { return (int)crossfadeTime; }
        set {
            crossfadeTime = value;
            crossfadeValue = 1 / (value * fixedUpdateTime);
        }
    }
    public float MusicVolume
    {
        get
        {
            return Mathf.Log(2, musicVolume) + 2;
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

            crossfadeAudioCoroutines.Clear();
            crossfade = value;
            if (crossfade)
            {
                crossfadeMaterial.SetColor("ButtonColor", Color.green);
            }
            else
            {
                crossfadeMaterial.SetFloat("Progress", 0);
                crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white);
            }

            activeAudioSource.volume = masterVolume * musicVolume;
            inactiveAudioSource.volume = masterVolume * musicVolume;
        }

    }

    // Start is called before the first frame update
    internal void Init()
    {
        LoadedFilesData.deletedMusicClips.Clear();
        LoadedFilesData.songs.Clear();
        LoadedFilesData.sfxClips.Clear();
        crossfadeValue = 1 / (crossfadeTime * fixedUpdateTime);
        activeAudioSource = plas.a1;
        inactiveAudioSource = plas.a2;

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
        vc = GetComponent<VolumeController>();
        musicButtons = new List<GameObject>();
        pt = GetComponent<PlaylistTabs>();
        StartCoroutine("CheckForNewFiles");
        localVolumeSlider.onValueChanged.AddListener(LocalVolumeSliderChanged);
        playbackScrubber.onValueChanged.AddListener(PlaybackTimeValueChanged);
        fadeInButton.onClick.AddListener(StartFadeInMusicVolume);
        fadeOutButton.onClick.AddListener(StartFadeOutMusicVolume);
        StartCoroutine(AdjustScale());
        StartCoroutine(Fft());

        crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);

        searchField.onValueChanged.AddListener(SearchTextEntered);
        searchField.onSelect.AddListener(SearchFieldHasFocus);
        searchField.onTextSelection.AddListener(SearchFieldHasFocus);
        searchField.onDeselect.AddListener(SearchFieldLostFocus);
        searchField.restoreOriginalTextOnEscape = false;
        clearPlaylistSearchButton.onClick.AddListener(ClearPlaylistSearch);

        discoModeController = GetComponent<DiscoMode>();
        tabCurrentlyPlaying = pt.selectedTab;
    }

    public void ChangeMasterVolume(float newMasterVolume)
    {
        masterVolume = (float)Math.Pow(newMasterVolume, 2f);
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
        //print("volume");
        actualVolume = newLocalVolume;
        musicVolume = (float)Math.Pow(newLocalVolume, 2f);
        activeAudioSource.volume = masterVolume * musicVolume;
        localVolumeLabel.text = (newLocalVolume * 100f).ToString("N0") + "%";
        localVolumeSlider.SetValueWithoutNotify(newLocalVolume);
    }

    internal void ClearPlaylistSearch()
    {
        searchField.text = "";
        searchField.Select();
        searchButtons = null;
    }

    internal void TabChanged()
    {
        searchField.SetTextWithoutNotify("");
        searchField.Select();
        searchButtons = null;
    }

    private void SearchFieldHasFocus(string entry = null, int start = 0, int end = 0)
    {

        mac.currentMenuState = MainAppController.MenuState.playlistSearch;
    }

    internal void SearchFieldLostFocus(string entry = null)
    {

        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    private void SearchFieldHasFocus(string entry = null)
    {

        mac.currentMenuState = MainAppController.MenuState.playlistSearch;
    }

    private void SearchTextEntered(string text)
    {
        SearchFieldHasFocus("");
        
        print(searchField.text.Length);
        if (searchButtons == null)
        {
            searchButtons = pt.selectedTab.musicContentView.GetComponentsInChildren<MusicButton>();
        }
        print(searchButtons.Length);
        ClearPlaylist();
        foreach (MusicButton mb in searchButtons)
        {
            string name = mb.gameObject.GetComponent<MusicButton>().Song.FileName;
            if (name.ToLower().Contains(text.ToLower())) mb.gameObject.SetActive(true);
        }
    }

    private void ClearPlaylist()
    {
        foreach(GameObject go in GameObject.FindGameObjectsWithTag("playlistItem"))
        {
            go.SetActive(false);
        }
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
            float fadeOutValue = 1f / (crossfadeTime * fixedUpdateTime);
            print("fadeOut val: " + MusicVolume);
            float newMusicVol = actualVolume;
            while (newMusicVol < 1f)
            {
                //print(MusicVolume);
                newMusicVol += fadeOutValue;
                //print("changing music volume");
                ChangeLocalVolume(newMusicVol);

                yield return new WaitForFixedUpdate();
            }
            ChangeLocalVolume(1f);
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
            float fadeOutValue = 1f / (crossfadeTime * fixedUpdateTime);
            print("fadeOut val: " + MusicVolume);
            float newMusicVol = actualVolume;
            while (newMusicVol > 0f)
            {
                //print(MusicVolume);
                newMusicVol -= fadeOutValue;
                //print("changing music volume");
                ChangeLocalVolume(newMusicVol);

                yield return new WaitForFixedUpdate();
            }
            ChangeLocalVolume(0f);
        }
        fadeOutMusicActive = false;
        yield return null;
    }

    IEnumerator Fft()
    {
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
                sum *= 0.5;

                fadeTargets[i] = sum;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator AdjustScale()
    {
        float newScale;
        while (true)
        {
            float totalSum = 0;
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
                newScale = Mathf.Min(newScale, 1);
                obj.localScale = new Vector3(1, newScale, 1);
                if(i < discoModeNumFreq) totalSum += newScale;

                //set var in shader to adjust texture height
                fftBarMaterials[i].SetFloat("Height", Mathf.Min(newScale, 1));
            }
            if (totalSum >= discoModeMinSum)
            {
                discoModeController.ChangeColors();
            }
            yield return new WaitForEndOfFrame();
        }
    }

    internal void InitLoadFiles(List<string> files = null)
    {
        print(files.Count);
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
                    // weird linux bug. Couldn't replicate
                    mac.ShowErrorMessage("Directory setup failed. Please inform the developer.");
                    break;
                }
            }
        }
        print(files.Count);
        foreach (string s in files)
        {
            if (LoadedFilesData.songs.FindIndex(f => f.FileName == s) == -1)
            {
                Song newSong = new Song(s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, ""));
                LoadedFilesData.songs.Add(newSong);
                GameObject listItem = Instantiate(Prefabs.musicButtonPrefab, musicScrollView.transform);
                
                listItem.GetComponentInChildren<TMP_Text>().text = s.Replace(mac.musicDirectory + mac.sep, "");
                listItem.GetComponent<MusicButton>().buttonId = musicButtons.Count - 1;
                listItem.GetComponent<MusicButton>().Song = newSong;
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
                foreach (Song s in LoadedFilesData.songs)
                {
                    if(!files.Contains(Path.Combine(mac.musicDirectory, s.FileName)))
                    {
                        toDelete.Add(musicButtons[LoadedFilesData.songs.IndexOf(s)]);
                    }
                }
                foreach (GameObject g in toDelete)
                {
                    LoadedFilesData.songs.Remove(g.GetComponent<MusicButton>().Song);
                    musicButtons.Remove(g);
                    Destroy(g);
                }
                toDelete.Clear();
                foreach (string s in files)
                {
                    string newFileName = "";
                    if ((LoadedFilesData.songs.FindIndex(f => f.FileName == s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "")) == -1) && (Path.GetExtension(s) == ".mp3" || Path.GetExtension(s) == ".ogg") && LoadedFilesData.deletedMusicClips.FindIndex(f => f.FileName == s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "")) == -1)
                    {
                        newFileName = s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "");
                        Song newSong = new Song(newFileName);
                        LoadedFilesData.songs.Add(newSong);


                        List<Song> songList = pt.tabs[0].Playlist;
                        songList.Add(newSong);
                        if(pt.selectedTab == pt.mainTab)
                        {
                            GameObject listItem = Instantiate(Prefabs.musicButtonPrefab, musicScrollView.transform);
                            MusicButton mb = listItem.GetComponent<MusicButton>();
                            mb.Init();
                            mb.buttonId = songList.Count;
                            mb.Song = newSong;
                            musicButtons.Add(listItem);
                        }


                        //regex to remove starting song numbers
                        //Match match = Regex.Match(s, @"\d{1,} *\.*-* *");
                        //if (String.IsNullOrEmpty(match.ToString())) listItem.GetComponentInChildren<TMP_Text>().text = newFileName;
                        //else listItem.GetComponentInChildren<TMP_Text>().text = newFileName.Replace(match.ToString(), "");



                        //musicButtons.Add(listItem);
                        //if (!String.IsNullOrEmpty(match.ToString()))
                        //{
                        //    newFileName = newFileName.Replace(match.ToString(), "");
                        //    listItem.GetComponentInChildren<MusicButton>().Song.sortName = newFileName;
                        //}
                    }
                }
                int id = 0;
                

                // Set indices for each button
                foreach (GameObject g in musicButtons)
                {
                    g.GetComponent<MusicButton>().buttonId = id;
                    id++;
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

    internal void PlaylistItemSelected(int id)
    {
        print(id);
        if (tabCurrentlyPlaying != pt.selectedTab)
        {
            ChangeCurrentlyPlayingTab(pt.selectedTab.tabId);
        }
        ItemSelected(id);
    }

    internal void ChangeCurrentlyPlayingTab(int id)
    {
        print("newTab: " + id);
        musicButtons.Clear();
        tabCurrentlyPlaying = pt.tabs[id];
        foreach (MusicButton mb in tabCurrentlyPlaying.musicContentView.GetComponentsInChildren<MusicButton>())
        {
            musicButtons.Add(mb.gameObject);
        }
        

    }

    public void ItemSelected(int id)
    {
        //print("is id: " + id);
        //print("is tcp: " + tabCurrentlyPlaying.tabId);
        //print("musicButtons length: " + musicButtons.Count);

        //TODO: fix this to let a song be restarted
        //if (nowPlayingButtonID == id && activeAudioSource.isPlaying)
        //{
        //    if (inactiveVorbisStream != null) inactiveVorbisStream.DecodedTime = TimeSpan.FromSeconds(0);
        //    else if (inactiveMp3Stream != null) inactiveMp3Stream.Time = TimeSpan.FromSeconds(0);
        //    else if (activeMp3Stream != null) activeMp3Stream.Time = TimeSpan.FromSeconds(0);
        //    else if (activeVorbisStream != null) activeVorbisStream.DecodedTime = TimeSpan.FromSeconds(0);
        //    activeAudioSource.time = 0;
        //}
        if (true)
        {
            if (musicButtons.Count > 0)
            {
                try
                {
                    nowPlayingButtonID = id;
                    MusicButton button = musicButtons[nowPlayingButtonID].GetComponent<MusicButton>();
                    songPath = System.IO.Path.Combine(mac.musicDirectory, button.Song.FileName);
                    songName = button.Song.FileName;
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
                                //if (!crossfade) activeAudioSource.Stop();
                            }
                            else
                            {
                                activeMp3Stream = new MpegFile(songPath);
                                totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                                if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                                clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                                //if (!crossfade) inactiveAudioSource.Stop();
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
                        UpdateMostRecentlyPlayed(songName);

                    }
                    else
                    {
                        activeAudioSource.Stop();
                        if (Path.GetExtension(songPath) == ".mp3")
                        {
                            fileTypeIsMp3 = true;
                            activeMp3Stream = new MpegFile(songPath);
                            //print(activeMp3Stream.Length);
                            totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                            //print(totalLength);
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

    bool TryPlayRequestedNext()
    {
        if(playNextList.Count > 0)
        {
            print("CurrentPage: " + tabCurrentlyPlaying.tabId);
            print("newPage: " + playNextList[0].playlistTabId);
            if(playNextList[0].playlistTabId != tabCurrentlyPlaying.tabId)
            {
                print("Current and next tab don't match, cahnging");
                ChangeCurrentlyPlayingTab(playNextList[0].playlistTabId);
            }
            ItemSelected(playNextList[0].id);
            print("before: " + playNextList.Count);
            playNextList.RemoveAt(0);
            print("after: " + playNextList.Count);

            return true;
        }
        return false;
    }

    internal void AddToPlayNext(PlayNextItem item)
    {
        //print(item.id);
        //print(item.playlistTabId);
        playNextList.Add(item);
    }

    void SetupInterfaceForPlay(AudioSource aSource, AudioClip clip = null)
    {
        //print("sifp");
        if (crossfade)
        {
            AudioSource temp = inactiveAudioSource;
            inactiveAudioSource = activeAudioSource;
            activeAudioSource = temp;
        }
        aSource.clip = clip;
        aSource.time = 0;
        aSource.Play();
        isPaused = false;
        if (crossfade)
        {
            // Just using StopCoroutine(CrossfadeAudioSources()) here simply _does not_ work. Don't know why. This works.
            foreach(Coroutine routine in crossfadeAudioCoroutines)
            {
                StopCoroutine(routine);
            }

            crossfadeAudioCoroutines.Add(StartCoroutine(CrossfadeAudioSources()));
        }

        playbackScrubber.SetValueWithoutNotify(0);
        print("buttonImage count: " + musicButtons.Count);
        print("current Page: " + tabCurrentlyPlaying.tabId);
        Image buttonImage = musicButtons[nowPlayingButtonID].GetComponent<Image>();
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.musicButtonGrey;
        buttonImage.color = ResourceManager.red;

        prevButtonImage = buttonImage;
        nowPlayingLabel.text = songName;
        musicStatusImage.sprite = mac.playImage;
        GetComponent<ScrollNowPlayingTitle>().SongChanged();
    }

    IEnumerator CrossfadeAudioSources()
    {
        int counter = 0;
        float maxVolume = MusicVolume * MasterVolume;
        activeAudioSource.volume = 0;
        float changeVolumeAmount = Mathf.Lerp(0, maxVolume, crossfadeValue);
        crossfadeMaterial.SetFloat("Progress", 0);

        while (true)
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
            if (counter > 1000)
            {
                Debug.Log("Breaking");
                break;
            }
            yield return new WaitForEndOfFrame();
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
        //data = new float[4096];
        try
        {
            activeMp3Stream.ReadSamples(data, 0, data.Length);
        }
        catch (NullReferenceException e)
        {
            shouldStop1 = true;
            mac.ShowErrorMessage("Error decoding audio from active MP3 stream, stopping playback");
            print(e);
        }
        catch (IndexOutOfRangeException e)
        {
            shouldDelayPlayNext = true;
            print(e);
        }
        catch (ObjectDisposedException e)
        {
            print("disposed 1");
            shouldStop1 = true;
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
            shouldDelayPlayNext = true;
            print(e);
        }
        catch(ObjectDisposedException e)
        {
            print("disposed 2");
            shouldStop2 = true;
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

        if(!TryPlayRequestedNext())
        {
            if (shuffle)
            {
                StartCoroutine(ShuffleSelectNewSong());
            }

            else
            {
                if (nowPlayingButtonID == musicButtons.Count - 1)
                {
                    ItemSelected(0);
                }
                else
                {
                    ItemSelected(nowPlayingButtonID + 1);
                }
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
                StartCoroutine(ShuffleSelectNewSong());
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

    IEnumerator ShuffleSelectNewSong()
    {
        int newButtonID = UnityEngine.Random.Range(0, musicButtons.Count);
        CheckMostRecentlyPlayed();
        while (nowPlayingButtonID == newButtonID || mostRecentSongs.Contains(musicButtons[newButtonID].GetComponent<MusicButton>().Song.FileName))
        {
            newButtonID = UnityEngine.Random.Range(0, musicButtons.Count);
            //print("same button");
            //yield return new WaitForEndOfFrame();
        }
        ItemSelected(newButtonID);
        yield return null;
    }

    // Remove first two items if mostRecentSongs length has met or exceeded # of musicButtons
    // Removing two adds some randomness. Removing just one results in playing songs in the same order
    private void CheckMostRecentlyPlayed()
    {
        if (mostRecentSongs.Count >= musicButtons.Count)
        {
            mostRecentSongs.RemoveRange(0, 2);
        }
    }

    private void UpdateMostRecentlyPlayed(string fileName)
    {
        if(!mostRecentSongs.Contains(fileName))
        {
            mostRecentSongs.Add(fileName);
        }
    }

    public void Stop(bool? main = null)
    {
        if(main == true)
        {
            activeAudioSource.Stop();
        }
        if(main == false)
        {
            inactiveAudioSource.Stop();
        }
        else
        {
            activeAudioSource.Stop();
            inactiveAudioSource.Stop();
        }
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
            print("case A");
            activeAudioSource.UnPause();
            isPaused = false;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.red;
            musicStatusImage.sprite = mac.playImage;
            pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

        }
        else if (TryPlayRequestedNext()) return;
        else if (shuffle)
        {
            nowPlayingButtonID = UnityEngine.Random.Range(0, musicButtons.Count - 1);
            ItemSelected(nowPlayingButtonID);
        }
        else
        {
            ItemSelected(0);
        }
    }

    public void RefreshSongOrder(int oldID, int newID)
    {
        print((oldID, newID));
        musicButtons[oldID].GetComponent<MusicButton>().buttonId = newID;
        musicButtons[newID].GetComponent<MusicButton>().buttonId = oldID;
        GameObject item = musicButtons[oldID];
        if(oldID == nowPlayingButtonID) nowPlayingButtonID = newID;
        musicButtons.Remove(item);
        musicButtons.Insert(newID, item);
    }



    private void PlaybackTimeValueChanged(float val)
    {
        //print(val);
        float value = Mathf.Clamp(val, 0f, 1f);
        if (Mathf.Abs(value) > .995f)
        {
            shouldDelayPlayNext = true;
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
        print("trying");
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
                    if (streamToUse != null && position > 0) streamToUse.Position = position;
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
            shouldDelayPlayNext = true;
        }
    }



    // My code is _so_ good that it only needs two frames to catch up and not crash because of nLayer
    IEnumerator DelayAndPlayNext()
    {
        shouldDelayPlayNext = false;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Next();
        nextDelayTimer = 1;
    }



    internal void DeleteItem(int selectedId)
    {
        if (nowPlayingButtonID == selectedId)
        {
            Stop();
            nowPlayingButtonID = -1;
        }
        LoadedFilesData.deletedMusicClips.Add(musicButtons[selectedId].GetComponent<MusicButton>().Song);
        playNextList.RemoveAll(a => pt.tabs[a.playlistTabId].Playlist[a.id] == musicButtons[selectedId].GetComponent<MusicButton>().Song);
        Destroy(musicButtons[selectedId]);
        musicButtons.RemoveAt(selectedId);
        int currentID = 0;
        foreach (GameObject mbObj in musicButtons)
        {
            MusicButton mb = mbObj.GetComponent<MusicButton>();
            mb.buttonId = currentID;
            currentID++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldStop1)
        {
            Stop(true);
            shouldStop1 = false;
        }
        if (shouldStop2)
        {
            Stop(false);
            shouldStop2 = false;
        }
        
        if(activeAudioSource.clip != null)
        {
            playbackScrubber.SetValueWithoutNotify(activeAudioSource.time / activeAudioSource.clip.length);
            playbackTimerText.text = Mathf.Floor(activeAudioSource.time / 60).ToString() + ":" + (Mathf.FloorToInt(activeAudioSource.time % 60)).ToString("D2") + "/" + Mathf.FloorToInt(activeAudioSource.clip.length / 60) + ":" + Mathf.FloorToInt(activeAudioSource.clip.length % 60).ToString("D2");
        }
        if (shouldDelayPlayNext)
        {
            StartCoroutine(DelayAndPlayNext());
        }
    }

    private void LateUpdate()
    {
        bool shouldStartCrossfade = false;
        if (activeAudioSource.clip)
        {
            shouldStartCrossfade = activeAudioSource.time > activeAudioSource.clip.length - crossfadeTime;
        }
        if(activeAudioSource.clip)
        {
            if (((!activeAudioSource.isPlaying && !isPaused) || (crossfade && shouldStartCrossfade))) 
            {
                prevButtonImage.color = ResourceManager.musicButtonGrey;
                if (musicButtons.Count > 1) //edge case if only one track is present in playlist
                {
                    if(!TryPlayRequestedNext())
                    {
                        if (nowPlayingButtonID < musicButtons.Count - 1)
                        {
                            int newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : nowPlayingButtonID + 1;
                            while (newButtonID == nowPlayingButtonID)
                            {
                                newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : nowPlayingButtonID + 1;
                                print("same new Button");
                            }
                            ItemSelected(newButtonID);
                        }
                        else
                        {
                            int newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : 0;
                            while (newButtonID == nowPlayingButtonID)
                            {
                                newButtonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : 0;
                                print("same newButton");
                            }
                            ItemSelected(newButtonID);
                        }
                    }
                }
                else
                {
                    Stop();
                }
            }
            if(nextDelayTimer > 0)
            {
                nextDelayTimer -= 1;
            }
        }
        
    }
}

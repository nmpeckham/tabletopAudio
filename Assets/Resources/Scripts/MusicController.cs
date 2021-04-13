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
using Id3;
using Extensions;

//Controls the playing of songs in the playlist
public class MusicController : MonoBehaviour
{
    [Range(0.0f, 2.0f)]
    public float multVal = 0.7f;

    //[Range(0.0f, 2.0f)]
    const float audioExp = 0.7f;    //higher = more contrast, lower = less (quiet sounds show as louder)

    private static MainAppController mac;

    public GameObject musicScrollView;

    public TMP_Text nowPlayingLabel;
    private static Image prevButtonImage = null;
    private static string songPath = "";
    private static string songName = "";
    internal static int nowPlayingButtonID = -1;

    private static float musicVolume = 1f;
    private static float masterVolume = 1f;
    //private float actualVolume = 1f;

    public Button fadeInButton;
    public Button fadeOutButton;
    public Slider localVolumeSlider;
    public Slider playbackScrubber;

    internal static bool isPaused = false;
    public TMP_Text localVolumeLabel;

    //public List<GameObject> musicButtons;
    public Image musicStatusImage;
    public TMP_Text playbackTimerText;
    public Image shuffleImage;
    private static bool shuffle;
    public Image pauseButton;
    public Image playButton;
    private static bool crossfade = false;

    public Material crossfadeMaterial;

    private static VolumeController vc;

    private static bool autoCheckForNewFiles = false;

    private static AudioSource activeAudioSource;
    private static AudioSource inactiveAudioSource;

    static MpegFile activeMp3Stream;
    static MpegFile inactiveMp3Stream;

    static VorbisReader activeVorbisStream;
    static VorbisReader inactiveVorbisStream;

    public GameObject fftParent;
    public FftBar[] pieces;
    private static Material[] fftBarMaterials;

    //private const float fixedUpdateStep = 1 / 60f;
    private const float fixedUpdateTime = 60f;
    private static float crossfadeTime = 0.01f;
    private static float crossfadeValue;
    public PlaylistAudioSources plas;

    static bool useInactiveMp3Callback = true;
    static bool useInactiveOggCallback = true;

    static bool shouldStop1 = false;
    static bool shouldStop2 = false;

    static bool usingInactiveAudioSource = true;



    static bool fileTypeIsMp3 = false;
    static bool mono = false;

    private static int nextDelayTimer = 0;

    private static float[] fadeTargets = new float[10];
    private static float[] prevFFTmesurement = new float[10]; // values one frame ago
    private static float[] oldScale = new float[10];

    private static bool fadeInMusicActive = false;
    private static bool fadeOutMusicActive = false;

    public TMP_InputField searchField;
    public Button clearPlaylistSearchButton;

    internal static DiscoMode discoModeController;
    internal static float discoModeMinSum = 0.45f;
    internal static float discoModeNumFreq = 3;

    private static List<Coroutine> crossfadeAudioCoroutines = new List<Coroutine>();

    private static List<string> mostRecentSongs = new List<string>();

    static bool shouldDelayPlayNext = false;

    private static List<PlayNextItem> playNextList = new List<PlayNextItem>();

    private static PlaylistTabs pt;

    internal static PlaylistTab nowPlayingTab;

    private static List<MusicButton> searchButtons = null;

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
            return musicVolume.ToLog();
        }

        set
        {
            musicVolume = value.ToActual();
            activeAudioSource.volume = masterVolume * musicVolume;
            localVolumeLabel.text = (value * 100f).ToString("N0") + "%";
            localVolumeSlider.SetValueWithoutNotify(value);
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
            print(value);
            crossfade = value;
            if (crossfade)
            {
                crossfadeMaterial.SetColor("ButtonColor", Color.green);
            }
            else
            {
                print("Stopping routines");
                foreach (Coroutine routine in crossfadeAudioCoroutines)
                {
                    StopCoroutine(routine);
                }
                crossfadeAudioCoroutines.Clear();
                crossfadeMaterial.SetFloat("Progress", 0);
                crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white);
            }

            activeAudioSource.volume = masterVolume * musicVolume;
            inactiveAudioSource.volume = 0;
            inactiveAudioSource.Stop();
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

        mac = Camera.main.GetComponent<MainAppController>();
        vc = GetComponent<VolumeController>();

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
        nowPlayingTab = PlaylistTabs.selectedTab;
    }

    //Called when a save is loaded
    internal void InitLoadFiles(List<string> files, int tabId)
    {
        //print("loading " + files.Count + " files");
        files.ForEach(a => AddNewSong(Path.Combine(mac.musicDirectory, a), tabId));
    }

    internal void ClearPlayNextList()
    {
        playNextList.Clear();
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
        MusicVolume = value;
    }

    //private void ChangeLocalVolume(float newLocalVolume)
    //{

    //}

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
        
        //print(searchField.text.Length);
        if (searchButtons == null)
        {
            searchButtons = PlaylistTabs.selectedTab.musicContentView.GetComponentsInChildren<MusicButton>().ToList();
        }
        //print(searchButtons.Length);
        ClearPlaylist();
        foreach (MusicButton mb in searchButtons)
        {
            if(mb.Song.artist != null)
            {
                if (mb.Song.artist.ToLower().Contains(text.ToLower()) || mb.Song.title.ToLower().Contains(text.ToLower())) mb.gameObject.SetActive(true);
            }
            else
            {
                if (mb.Song.title.ToLower().Contains(text.ToLower())) mb.gameObject.SetActive(true);
            }
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
        fadeOutButton.GetComponent<Image>().color = Color.white;
        if (fadeInMusicActive)
        {
            fadeInMusicActive = false;
            fadeInButton.GetComponent<Image>().color = Color.white;
            StopCoroutine("FadeInMusicVolume");
        }
        else
        {
            int counter = 0;
            fadeInMusicActive = true;
            fadeOutMusicActive = false;
            float fadeOutValue = 1f / (crossfadeTime * fixedUpdateTime);
            while (MusicVolume < 1f)
            {
                if (counter % 20 == 0)
                {
                    if (counter % 40 == 0) fadeInButton.GetComponent<Image>().color = Color.green;
                    else fadeInButton.GetComponent<Image>().color = Color.white;
                }
                MusicVolume = MusicVolume + fadeOutValue;

                counter++;
                yield return null;
            }
            MusicVolume = 1f;
        }
        fadeOutMusicActive = false;
        fadeInButton.GetComponent<Image>().color = Color.white;
        yield break;
    }

    IEnumerator FadeOutMusicVolume()
    {
        fadeInButton.GetComponent<Image>().color = Color.white;
        int counter = 0;
        if (fadeOutMusicActive)
        {
            fadeOutMusicActive = false;
            fadeOutButton.GetComponent<Image>().color = Color.white;
            StopCoroutine("FadeOutMusicVolume");
        }
        else
        {
            fadeOutMusicActive = true;
            fadeInMusicActive = false;
            float fadeOutValue = 1f / (crossfadeTime * fixedUpdateTime);
            //float fadeOutVal = 1f;

            while (MusicVolume > 0)
            {
                if (counter % 20 == 0)
                {
                    if(counter % 40 == 0) fadeOutButton.GetComponent<Image>().color = Color.green;
                    else fadeOutButton.GetComponent<Image>().color = Color.white;
                }
                MusicVolume = Mathf.Max(0, MusicVolume - fadeOutValue);

                counter++;
                yield return null;
            }
            MusicVolume = 0f;
        }
        fadeOutMusicActive = false;
        fadeOutButton.GetComponent<Image>().color = Color.white;
        yield break;
    }

    IEnumerator Fft()
    {
        // Much of this is based on making the fft display _look_ nice, rather than to be mathematically correct
        int fftSize = 4096;
        float[] data0 = new float[fftSize];
        float[] data1 = new float[fftSize];
        int[] segments = new int[11] {4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096}; // Don't ask...
        //4096, 2058, 1024, 512, 256, 128, 64, 32, 16, 8
        double sum;
        int maxVal;
        int startVal;
        while (true)
        {
            activeAudioSource.GetSpectrumData(data0, 0, FFTWindow.BlackmanHarris);
            if(!mono) { activeAudioSource.GetSpectrumData(data1, 1, FFTWindow.BlackmanHarris); }
            for (int i = 0; i < pieces.Length; i++)
            {
                sum = 0f;
                startVal = segments[i];
                maxVal = segments[i + 1];
                for (int j = startVal; j < maxVal; j++)
                {
                    sum += data0[j];
                    sum += data1[j];
                }
                sum *= (mono ? 0.5f : .25f);
                sum *=  Mathf.Cos((float)sum * 2.5f) + 1; //math
                sum = Mathf.Pow((float)sum, audioExp); // make low sounds show as a little louder

                fadeTargets[i] = (float)sum;//> audioCutoff ? sum : 0f;
            }
            yield return null;
        }
    }

    IEnumerator AdjustScale()
    {
        
        while (true)
        {
            float totalSum = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                
                Transform obj = pieces[i].transform;
                float temp = fadeTargets[i];
                float newScale;

                if (temp > oldScale[i])
                {
                    newScale = (temp * 0.8f) + (oldScale[i] * 0.2f);
                }
                else
                {
                    newScale = (temp * 0.6f) + (oldScale[i] * 0.25f) + (prevFFTmesurement[i] * 0.15f); //average slightly over 3 frames
                }
                newScale = Mathf.Min(newScale, 1);
                obj.localScale = new Vector3(1, newScale, 1);
                if(i < discoModeNumFreq) totalSum += newScale;

                //set var in shader to adjust texture height
                fftBarMaterials[i].SetFloat("Height", newScale);
                prevFFTmesurement[i] = oldScale[i];
                oldScale[i] = temp;
            }
            if (totalSum >= discoModeMinSum)
            {
                discoModeController.ChangeColors();
            }
            yield return null;
        }
    }

    bool FileIsValid(string s)
    {
        return ((LoadedFilesData.songs.FindIndex(f => f.FileName == s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "")) == -1) && (Path.GetExtension(s) == ".mp3" || Path.GetExtension(s) == ".ogg") && LoadedFilesData.deletedMusicClips.FindIndex(f => f.FileName == s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "")) == -1); 
    }
    void AddNewSong(String s, int tabId = 0)
    {
        string artist = null;
        string title = null;
        TimeSpan duration = TimeSpan.Zero;
        if (Path.GetExtension(s) == ".mp3")
        {
            //Try both ID3 families
            Id3Tag newTag = new Mp3(s).GetTag(Id3TagFamily.Version2X);
            if (newTag == null)
            {
                newTag = new Mp3(s).GetTag(Id3TagFamily.Version1X);
            }
            if (newTag != null)
            {
                artist = newTag.Artists;
                title = newTag.Title;
                duration = newTag.Length;
            }
            //If duration not present in tag, get from temp mpeg stream
            if (duration == TimeSpan.Zero)
            {
                MpegFile temp = new MpegFile(s);
                duration = temp.Duration;
            }
        }
        //ID3 info not supported for ogg vorbis, get duration from temp vorbis stream
        else if(Path.GetExtension(s) == ".ogg")
        {
            VorbisReader temp = new VorbisReader(s);
            duration = temp.TotalTime;
        }
        if(title == null)
        {
            title = s.Replace(Path.GetExtension(s), "").Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "");    //Remove file extension
        }

        string newFileName = s.Replace(mac.musicDirectory + Path.DirectorySeparatorChar, "");
        Song newSong = new Song(newFileName, title, duration, artist);
        LoadedFilesData.songs.Add(newSong);

        pt.AddSongToPlaylist(tabId, newSong);
    }
    IEnumerator CheckForNewFiles()
    {
        List<string> toDelete = new List<string>();
        while (true)
        {
            string[] files = System.IO.Directory.GetFiles(mac.musicDirectory);
            if (autoCheckForNewFiles)
            {
                foreach (Song s in LoadedFilesData.songs)
                {
                    if(!files.Contains(Path.Combine(mac.musicDirectory, s.FileName)))
                    {
                        toDelete.Add(s.FileName);
                    }
                }
                foreach (string s in toDelete)
                {
                    LoadedFilesData.songs.RemoveAll(song => song.FileName == s);
                    foreach (PlaylistTab t in PlaylistTabs.tabs)
                    {
                        List<MusicButton> buttons = t.MusicButtons.FindAll(mb => mb.Song.FileName == s);
                        buttons.ForEach(mb => {
                            t.MusicButtons.Remove(mb);
                            Destroy(mb.gameObject);
                        });
                    }
                }
                toDelete.Clear();
                yield return null;
                foreach (string s in files)
                {
                    if (FileIsValid(s))
                    {
                        AddNewSong(s);

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
            }
            yield return new WaitForSeconds(1);
        }
    }

    internal void PlaylistItemSelected(int id)
    {
        if (nowPlayingTab != PlaylistTabs.selectedTab)
        {
            ChangeCurrentlyPlayingTab(PlaylistTabs.selectedTab.tabId);
        }
        ItemSelected(id);
    }

    internal void ChangeCurrentlyPlayingTab(int id)
    {
        nowPlayingTab = PlaylistTabs.tabs[id];
    }

    public void ItemSelected(int id)
    {
        //print("item Selected " + id);
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
            if (nowPlayingTab.MusicButtons.Count > 0)
            {
                try
                {
                    nowPlayingButtonID = id;
                    MusicButton button = nowPlayingTab.MusicButtons[nowPlayingButtonID];
                    songPath = System.IO.Path.Combine(mac.musicDirectory, button.Song.FileName);
                    songName = button.Song.FileName;
                    AudioClip clip = null;
                    long totalLength = 0;
                    if (crossfade)
                    {
                        if (Path.GetExtension(songPath) == ".mp3")
                        {
                            print("is mp3");
                            fileTypeIsMp3 = true;
                            if (useInactiveMp3Callback)
                            {
                                inactiveMp3Stream = new MpegFile(songPath);
                                totalLength = inactiveMp3Stream.Length / (inactiveMp3Stream.Channels * 4);
                                if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                                clip = AudioClip.Create(songPath, (int)totalLength, inactiveMp3Stream.Channels, inactiveMp3Stream.SampleRate, true, InactiveMP3Callback);
                                print("clipName " + clip.name);
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
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                        }
                        else if (Path.GetExtension(songPath) == ".ogg")
                        {
                            fileTypeIsMp3 = false;
                            activeVorbisStream = new NVorbis.VorbisReader(songPath);
                            totalLength = activeVorbisStream.TotalSamples;
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, activeVorbisStream.Channels, activeVorbisStream.SampleRate, true, ActiveVorbisCallback);
                        }
                        else
                        {
                            activeAudioSource.clip = null;
                        }
                        SetupInterfaceForPlay(activeAudioSource, clip);
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
                    NowPlayingWebpage.SongChanged(button.Song);
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
        //print("TryingToPlayNext");
        if(playNextList.Count > 0)
        {
            //print("CurrentPage: " + nowPlayingTab.tabId);
            //print("newPage: " + playNextList[0].playlistTabId);
            if(playNextList[0].playlistTabId != nowPlayingTab.tabId)
            {
                //print("Current and next tab don't match, changing");
                ChangeCurrentlyPlayingTab(playNextList[0].playlistTabId);
            }
            ItemSelected(playNextList[0].id);
            //print("before: " + playNextList.Count);
            playNextList.RemoveAt(0);
            //print("after: " + playNextList.Count);

            return true;
        }
        return false;
    }

    internal void AddToPlayNext(PlayNextItem item)
    {
        //print(item.id);
        //print(item.playlistTabId);
        playNextList.Add(item);
        print(playNextList.Count);
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
        isPaused = false;
        if (crossfade)
        {
            // Just using StopCoroutine(CrossfadeAudioSources()) here simply _does not_ work. Don't know why. This works.
            foreach(Coroutine routine in crossfadeAudioCoroutines)
            {
                StopCoroutine(routine);
            }

            crossfadeAudioCoroutines.Add(StartCoroutine(CrossfadeAudioSources()));
            crossfadeAudioCoroutines.Add(StartCoroutine(SetCrossfadeProgressBar()));
        }

        playbackScrubber.SetValueWithoutNotify(0);
        //print("buttonImage count: " + musicButtons.Count);
        //print("current Page: " + tabCurrentlyPlaying.tabId);
        Image buttonImage = nowPlayingTab.MusicButtons[nowPlayingButtonID].GetComponent<Image>();
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.musicButtonGrey;
        buttonImage.color = ResourceManager.red;

        prevButtonImage = buttonImage;
        nowPlayingLabel.text = songName;
        musicStatusImage.sprite = mac.playImage;
        GetComponent<ScrollNowPlayingTitle>().SongChanged();
    }

    IEnumerator SetCrossfadeProgressBar()
    {
        //for(int i = 0; i <= 1f / crossfadeValue; i++)
        //{
        //    crossfadeMaterial.SetFloat("Progress", i / crossfadeValue);
        //    yield return new WaitForFixedUpdate();
        //}
        //crossfadeMaterial.SetFloat("Progress", 0f);
        yield return null;
    }

    IEnumerator CrossfadeAudioSources()
    {
        int initNumFrames = (int) (1 / crossfadeValue);
        int numFrames = initNumFrames;
        int counter = 0;
        float maxVolume = (MusicVolume * MasterVolume).ToActual();
        activeAudioSource.volume = 0;
        float changeVolumeAmount = Mathf.Lerp(0, maxVolume, crossfadeValue);
        print(changeVolumeAmount);
        crossfadeMaterial.SetFloat("Progress", 0);

        for(int i = 0; i < numFrames; i++)
        {
            if (counter % 20 == 0)
            {
                if (counter % 40 == 0) crossfadeMaterial.SetColor("ButtonColor", Color.green);
                else crossfadeMaterial.SetColor("ButtonColor", Color.red);
            }
            activeAudioSource.volume += changeVolumeAmount;
            inactiveAudioSource.volume -= changeVolumeAmount;

            crossfadeMaterial.SetFloat("Progress", (i / (float)numFrames));

            counter++;
            // Shit's broke
            if (counter > 1000)
            {
                Debug.Log("Breaking");
                break;
            }
            numFrames = (int)(1 / crossfadeValue);
            yield return null;
        }
        crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);
        activeAudioSource.volume = (MusicVolume * MasterVolume).ToActual();
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;
        crossfadeAudioCoroutines.RemoveAt(crossfadeAudioCoroutines.Count - 1);  //remove self on completion
        yield return null;
    }

    void ActiveMP3Callback(float[] data)
    {
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
        //print("Next");
        StopCoroutine(CrossfadeAudioSources());
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;

        if (!TryPlayRequestedNext())
        {
            print(nowPlayingTab.tabId);
            if (nowPlayingTab.MusicButtons.Count > 1)
            {

                if (shuffle)
                {
                    StartCoroutine(ShuffleSelectNewSong());
                }

                else
                {
                    if (nowPlayingButtonID == nowPlayingTab.MusicButtons.Count - 1)
                    {
                        ItemSelected(0);
                    }
                    else
                    {
                        ItemSelected(nowPlayingButtonID + 1);
                    }
                }
            }

            else
            {
                activeAudioSource.Stop();
                activeAudioSource.clip = null;
                playButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
                playbackScrubber.SetValueWithoutNotify(0);
                pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
                nowPlayingLabel.text = "";
                playbackTimerText.text = "0:00/0:00";
                prevButtonImage.color = ResourceManager.musicButtonGrey;
            }
        }

    }

    public void Previous()
    {
        foreach (Coroutine routine in crossfadeAudioCoroutines)
        {
            StopCoroutine(routine);
        }
        inactiveAudioSource.volume = 0f;
        inactiveAudioSource.clip = null;

        if (nowPlayingTab.MusicButtons.Count > 0)
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
                    ItemSelected(nowPlayingTab.MusicButtons.Count - 1);
                }
            }
            playbackScrubber.SetValueWithoutNotify(0);
            pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

        }
    }

    IEnumerator ShuffleSelectNewSong()
    {
        int newButtonID = UnityEngine.Random.Range(0, nowPlayingTab.MusicButtons.Count);
        CheckMostRecentlyPlayed();
        int i = 0;
        while (nowPlayingButtonID == newButtonID || mostRecentSongs.Contains(nowPlayingTab.MusicButtons[newButtonID].GetComponent<MusicButton>().Song.FileName))
        {
            newButtonID = UnityEngine.Random.Range(0, nowPlayingTab.MusicButtons.Count);
            i++;
            if (i > 500)
            {
                print("oof");
                break;
            }
        }
        ItemSelected(newButtonID);
        print("shuffle select new song");
        yield return null;
    }

    // Remove first two items if mostRecentSongs length has met or exceeded # of musicButtons
    // Removing two adds some randomness. Removing just one results in playing songs in the same order
    private void CheckMostRecentlyPlayed()
    {
        if (mostRecentSongs.Count >= nowPlayingTab.MusicButtons.Count)
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
        foreach (Coroutine routine in crossfadeAudioCoroutines)
        {
            StopCoroutine(routine);
        }
        isPaused = false;
        activeAudioSource.clip = null;
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.musicButtonGrey;
        musicStatusImage.sprite = mac.stopImage;
        nowPlayingLabel.text = "";
        playButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
        pauseButton.color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
        NowPlayingWebpage.SongStopped();
    }

    public void Pause()
    {
        if (isPaused)
        {
            Play();
            NowPlayingWebpage.SongUnpaused(activeAudioSource.time);
        }
        else if(activeAudioSource.isPlaying)
        {
            pauseButton.color = ResourceManager.orange;
            activeAudioSource.Pause();
            isPaused = true;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.orange;
            musicStatusImage.sprite = mac.pauseImage;
            NowPlayingWebpage.SongPaused(activeAudioSource.time);
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
            nowPlayingButtonID = UnityEngine.Random.Range(0, PlaylistTabs.selectedTab.MusicButtons.Count);
            ChangeCurrentlyPlayingTab(PlaylistTabs.selectedTab.tabId);
            ItemSelected(nowPlayingButtonID);
        }
        else
        {
            PlaylistItemSelected(0);
        }
    }

    public void RefreshSongOrder(int oldID, int newID)
    {
        PlaylistTabs.selectedTab.MusicButtons[oldID].buttonId = newID;
        PlaylistTabs.selectedTab.MusicButtons[newID].buttonId = oldID;

        //Put button in correct position in list
        MusicButton item = nowPlayingTab.MusicButtons[oldID];
        if (newID == nowPlayingButtonID)
        {
            nowPlayingButtonID = oldID;
        }
        else if (oldID == nowPlayingButtonID)
        {
            nowPlayingButtonID = newID;
        }

        nowPlayingTab.MusicButtons.Remove(item);
        nowPlayingTab.MusicButtons.Insert(newID, item);
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
        if (nextDelayTimer == 0 && val >= 0)
        {
            nextDelayTimer = 1;
            if (fileTypeIsMp3)
            {

                MpegFile streamToUse = useInactiveMp3Callback ? activeMp3Stream : inactiveMp3Stream;
                try
                {
                    long position = Convert.ToInt64(streamToUse.Length * val);
                    print(position);
                    if (streamToUse != null && position >= 0) streamToUse.Position = position;
                    activeAudioSource.time = Mathf.Min(val, 0.999f) * activeAudioSource.clip.length;    //0.999f beacuse going to exact last samples produces weird results
                }
                catch (NullReferenceException e)
                {
                    print(e);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    print(e);
                }
            }
            else
            {
                NVorbis.VorbisReader streamToUse = useInactiveOggCallback ? activeVorbisStream : inactiveVorbisStream;
                try
                {
                    if (streamToUse != null) streamToUse.DecodedPosition = Convert.ToInt64(streamToUse.TotalSamples * val);
                    activeAudioSource.time = val * activeAudioSource.clip.length;
                }
                catch (NullReferenceException e)
                {
                    print(e);
                }
            }
        }
    }


    // My code is _so_ good that it only needs one frames to catch up and not crash because of nLayer
    IEnumerator DelayAndPlayNext()
    {
        print("Delaying and playing next");
        shouldDelayPlayNext = false;
        yield return null;
        Next();
        nextDelayTimer = 1;
    }


    internal void DeleteItem(int selectedId)
    {
        print("deleting");
        print(PlaylistTabs.selectedTab.MusicButtons[selectedId].Song.title);
        print(PlaylistTabs.selectedTab.tabId);
        if (nowPlayingTab == PlaylistTabs.selectedTab && nowPlayingButtonID == selectedId)
        {
            Stop();
            nowPlayingButtonID = -1;
        }
        //LoadedFilesData.deletedMusicClips.Add(nowPlayingTab.MusicButtons[selectedId].GetComponent<MusicButton>().Song);
        playNextList.RemoveAll(a => a.id == PlaylistTabs.tabs[a.playlistTabId].MusicButtons[a.id].buttonId);
        Destroy(PlaylistTabs.selectedTab.MusicButtons[selectedId].gameObject);
        print(PlaylistTabs.selectedTab.MusicButtons[selectedId].Song.title);
        PlaylistTabs.selectedTab.MusicButtons.RemoveAt(selectedId);
        //Re-number tabs
        int currentID = 0;
        foreach (MusicButton mb in PlaylistTabs.selectedTab.MusicButtons)
        {
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
        if (activeAudioSource.clip)
        {
            if (((!activeAudioSource.isPlaying && !isPaused) || (crossfade && shouldStartCrossfade)))
            {
                Next();
            }
            if (nextDelayTimer > 0)
            {
                nextDelayTimer -= 1;
            }
        }
    }
}

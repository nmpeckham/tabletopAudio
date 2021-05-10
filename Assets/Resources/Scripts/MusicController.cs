﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;
using NLayer;
using NVorbis;
using Id3;
using Extensions;
using UnityEngine.Audio;

//Controls the playing of songs in the playlist
public class MusicController : MonoBehaviour
{
    private static MainAppController mac;

    public GameObject musicScrollView;

    public TMP_Text nowPlayingLabel;
    private static Image prevButtonImage = null;
    private static string songPath = "";
    public static string SongPath
    {
        get
        {
            return songPath;
        }
    }
    private static string songName = "";
    public static string SongName
    {
        get
        {
            return songName;
        }
    }
    internal static int nowPlayingButtonID = -1;

    public Button fadeInButton;
    public Button fadeOutButton;
    public Slider localVolumeSlider;
    public Slider playbackScrubber;

    public static bool isPaused = false;
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

    private static bool autoCheckForNewFiles = true;

    private static AudioSource activeAudioSource;
    private static AudioSource inactiveAudioSource;

    static MpegFile activeMp3Stream;
    static MpegFile inactiveMp3Stream;

    static VorbisReader activeVorbisStream;
    static VorbisReader inactiveVorbisStream;

    //private const float fixedUpdateStep = 1 / 60f;
    private const float fixedUpdateTime = 60f;
    private static float crossfadeTime = 0.01f;
    private static float crossfadeValue;
    public PlaylistAudioSources plas;

    static bool usingInactiveAudioSource = false;

    static bool shouldStop1 = false;
    static bool shouldStop2 = false;

    static bool fileTypeIsMp3 = false;
    static bool mono = false;

    private static int nextDelayTimer = 0;

    private static bool fadeInMusicActive = false;
    private static bool fadeOutMusicActive = false;

    public TMP_InputField searchField;
    public Button clearPlaylistSearchButton;



    private static Coroutine crossfadeAudioCoroutine = null;

    private static List<string> mostRecentSongs = new List<string>();

    static bool shouldDelayPlayNext = false;

    private static List<PlayNextItem> playNextList = new List<PlayNextItem>();

    private static PlaylistTabs pt;

    internal static PlaylistTab nowPlayingTab;

    public AudioMixerGroup musicAMG;
    public AudioMixerGroup masterAMG;

    public static PlaylistTab NowPlayingTab
    {
        get
        {
            return nowPlayingTab;
        }
    }

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
            float outVal;
            musicAMG.audioMixer.GetFloat("MusicVolume", out outVal);
            return outVal.ToZeroOne();
        }

        set
        {
            if(value > 1f)
            {
                throw new ArgumentOutOfRangeException("Volume too loud! Must be between 0 and 1.");
            }
            musicAMG.audioMixer.SetFloat("MusicVolume", value.ToDB());
            localVolumeLabel.text = (value * 100f).ToString("N0") + "%";
            localVolumeSlider.SetValueWithoutNotify(value);
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
            if (crossfade)
            {
                crossfadeMaterial.SetColor("ButtonColor", Color.green);
            }
            else
            {
                //foreach (Coroutine routine in crossfadeAudioCoroutines)
                //{
                //    StopCoroutine(routine);
                //}
                //crossfadeAudioCoroutines.Clear();
                if (crossfadeAudioCoroutine != null) StopCoroutine(crossfadeAudioCoroutine);
                crossfadeMaterial.SetFloat("Progress", 0);
                crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white);
                if (usingInactiveAudioSource)
                {
                    activeAudioSource.volume = 0f;
                    inactiveAudioSource.volume = 1f;
                }
                else
                {
                    inactiveAudioSource.volume = 0;
                    activeAudioSource.volume = 1f;
                }

            }
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

        mac = Camera.main.GetComponent<MainAppController>();
        vc = GetComponent<VolumeController>();

        pt = GetComponent<PlaylistTabs>();
        StartCoroutine("CheckForNewFiles");
        localVolumeSlider.onValueChanged.AddListener(LocalVolumeSliderChanged);
        playbackScrubber.onValueChanged.AddListener(PlaybackTimeValueChanged);
        fadeInButton.onClick.AddListener(StartFadeInMusicVolume);
        fadeOutButton.onClick.AddListener(StartFadeOutMusicVolume);

        StartCoroutine(Fft());

        crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);

        searchField.onValueChanged.AddListener(SearchTextEntered);
        searchField.onSelect.AddListener(SearchFieldHasFocus);
        searchField.onTextSelection.AddListener(SearchFieldHasFocus);
        searchField.onDeselect.AddListener(SearchFieldLostFocus);
        searchField.restoreOriginalTextOnEscape = false;
        clearPlaylistSearchButton.onClick.AddListener(ClearPlaylistSearch);

        nowPlayingTab = PlaylistTabs.selectedTab;
    }

    //Called when a save is loaded
    internal void InitLoadFiles(List<string> files, int tabId)
    {
        files.ForEach(a =>
        {
            try
            {
                AddNewSong(Path.Combine(mac.musicDirectory, a), tabId);
            }
            catch(FileNotFoundException)
            {
                mac.ShowErrorMessage("Could not find file " + a);
            }
        });
    }

    internal void ClearPlayNextList()
    {
        playNextList.Clear();
    }

    private void LocalVolumeSliderChanged(float value)
    {
        StopCoroutine("FadeInMusicVolume");
        StopCoroutine("FadeOutMusicVolume");
        MusicVolume = value;
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

        if (searchButtons == null)
        {
            searchButtons = PlaylistTabs.selectedTab.musicContentView.GetComponentsInChildren<MusicButton>().ToList();
        }
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
                MusicVolume += fadeOutValue;

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
        int[] segments = new int[41] { 2, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 384, 448, 512, 640, 768, 896, 1024, 1280, 1536, 1792, 2048, 2560, 3072, 3584, 4096 }; // Don't ask...
        double sum;
        int maxVal;
        int startVal;
        while (true)
        {
            if(usingInactiveAudioSource)
            {
                inactiveAudioSource.GetSpectrumData(data0, 0, FFTWindow.BlackmanHarris);
                inactiveAudioSource.GetSpectrumData(data1, 1, FFTWindow.BlackmanHarris);
            }
            else
            {
                activeAudioSource.GetSpectrumData(data0, 0, FFTWindow.BlackmanHarris);
                activeAudioSource.GetSpectrumData(data1, 1, FFTWindow.BlackmanHarris);
            }

            for (int i = 0; i < segments.Length - 1; i++)
            {
                sum = 0f;
                startVal = segments[i];
                maxVal = segments[i + 1];
                for (int j = startVal; j < maxVal; j++)
                {
                    sum += data0[j];
                    sum += data1[j];
                }

                FftController.fadeTargets[i] = (float)sum;
            }
            yield return null;
        }
    }

    bool FileIsValid(string s)
    {
        return LoadedFilesData.songs.All(f => f.FileName != s) && (Path.GetExtension(s) == ".mp3" || Path.GetExtension(s) == ".ogg") && LoadedFilesData.deletedMusicClips.All(f => f.FileName != s) && !(Application.isEditor && s.Contains("testRunner")); 
    }
    void AddNewSong(String s, int tabId = 0)
    {
        string artist = null;
        string title = null;
        TimeSpan duration = TimeSpan.Zero;
        if (Path.GetExtension(s) == ".mp3")
        {
            //Try both ID3 families
            Id3Tag newTag = null;
            try
            {
                newTag = new Mp3(s).GetTag(Id3TagFamily.Version2X);
            }
            catch (IndexOutOfRangeException) { }

            var altTag = TagLib.File.Create(s);

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
            if(altTag.Tag.Performers.Length > 0) artist = altTag.Tag.Performers[0];
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
            title = Path.GetFileName(s);
        }
        Song newSong = new Song(s, title, duration, artist);
        LoadedFilesData.songs.Add(newSong);

        pt.AddSongToPlaylist(tabId, newSong);
    }
    IEnumerator CheckForNewFiles()
    {
        List<string> toDelete = new List<string>();
        while (true)
        {
            string[] files = System.IO.Directory.GetFiles(mac.musicDirectory, "*", SearchOption.AllDirectories);
            if (autoCheckForNewFiles)
            {
                foreach (Song s in LoadedFilesData.songs)
                {
                    if (!files.Any(file => file == s.FileName))
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
                            DeleteItem(mb.buttonId);
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
                    }
                }
            }
            PlaylistTabs.tabs[0].MusicButtons.Sort();
            int i = 0;
            PlaylistTabs.tabs[0].MusicButtons.ForEach(mb =>
            {
                mb.buttonId = i;
                mb.gameObject.transform.SetSiblingIndex(i);
                i++;
            });
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
        if (nowPlayingTab.MusicButtons.Count > 0)
        {
            try
            {
                nowPlayingButtonID = id;
                MusicButton button = nowPlayingTab.MusicButtons[nowPlayingButtonID];
                songPath = System.IO.Path.Combine(mac.musicDirectory, button.Song.FileName);
                songName = button.Song.SortName;
                AudioClip clip = null;
                long totalLength = 0;
                if (crossfade)
                {
                    if (Path.GetExtension(songPath) == ".mp3")
                    {
                        fileTypeIsMp3 = true;
                        if (usingInactiveAudioSource)
                        {
                            try
                            {
                                activeMp3Stream.Dispose();
                            }
                            catch (NullReferenceException) { }
                            activeMp3Stream = new MpegFile(songPath);
                            totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                            SetupInterfaceForPlay(activeAudioSource, clip);

                        }
                        else
                        {
                            try
                            {
                                inactiveMp3Stream.Dispose();
                            }
                            catch (NullReferenceException) { }
                            inactiveMp3Stream = new MpegFile(songPath);
                            totalLength = inactiveMp3Stream.Length / (inactiveMp3Stream.Channels * 4);
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, inactiveMp3Stream.Channels, inactiveMp3Stream.SampleRate, true, InactiveMP3Callback);
                            SetupInterfaceForPlay(inactiveAudioSource, clip);

                        }

                        usingInactiveAudioSource = !usingInactiveAudioSource;
                    }
                    else if (Path.GetExtension(songPath) == ".ogg")
                    {
                        fileTypeIsMp3 = false;
                        if (usingInactiveAudioSource)
                        {
                            try
                            {
                                activeVorbisStream.Dispose();
                            }
                            catch (NullReferenceException) { }
                            activeVorbisStream = new NVorbis.VorbisReader(songPath);
                            totalLength = activeVorbisStream.TotalSamples;
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, activeVorbisStream.Channels, activeVorbisStream.SampleRate, true, ActiveVorbisCallback);

                        }
                        else
                        {
                            try
                            {
                                inactiveVorbisStream.Dispose();
                            }
                            catch (NullReferenceException) { }
                            inactiveVorbisStream = new NVorbis.VorbisReader(songPath);
                            totalLength = inactiveVorbisStream.TotalSamples;
                            if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                            clip = AudioClip.Create(songPath, (int)totalLength, inactiveVorbisStream.Channels, inactiveVorbisStream.SampleRate, true, InactiveVorbisCallback);
                        }
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
                    inactiveAudioSource.Stop();
                    if (Path.GetExtension(songPath) == ".mp3")
                    {
                        fileTypeIsMp3 = true;
                        try
                        {
                            activeMp3Stream.Dispose();
                        }
                        catch (NullReferenceException) { }
                        activeMp3Stream = new MpegFile(songPath);
                        totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                        if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                        clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                    }
                    else if (Path.GetExtension(songPath) == ".ogg")
                    {
                        fileTypeIsMp3 = false;
                        try
                        {
                            activeVorbisStream.Dispose();
                        }
                        catch (NullReferenceException) { }
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

    bool TryPlayRequestedNext()
    {
        if(playNextList.Count > 0)
        {
            if(playNextList[0].playlistTabId != nowPlayingTab.tabId)
            {
                ChangeCurrentlyPlayingTab(playNextList[0].playlistTabId);
            }
            ItemSelected(playNextList[0].id);
            playNextList.RemoveAt(0);

            return true;
        }
        return false;
    }

    internal void AddToPlayNext(PlayNextItem item)
    {
        playNextList.Add(item);
    }

    void SetupInterfaceForPlay(AudioSource aSource, AudioClip clip = null)
    {
        if (crossfade)
        {
            //AudioSource temp = inactiveAudioSource;
            //inactiveAudioSource = activeAudioSource;
            //activeAudioSource = temp;
            if (inactiveAudioSource == activeAudioSource) print("Fucked");
            if (crossfadeAudioCoroutine != null) StopCoroutine(crossfadeAudioCoroutine);
            crossfadeAudioCoroutine = StartCoroutine(CrossfadeAudioSources());
        }
        aSource.clip = clip;
        aSource.time = 0;
        aSource.Play();
        isPaused = false;
        //if (crossfade)
        //{
        //    // Just using StopCoroutine(CrossfadeAudioSources()) here simply _does not_ work. Don't know why. This works.
        //    foreach(Coroutine routine in crossfadeAudioCoroutines)
        //    {
        //        StopCoroutine(routine);
        //    }
        //    crossfadeAudioCoroutines.Add();
        //}
        
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

    IEnumerator CrossfadeAudioSources()
    {
        print("starting fade");
        AudioSource fadeOut = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
        AudioSource fadeIn = usingInactiveAudioSource ? activeAudioSource : inactiveAudioSource;
        int initNumFrames = (int) (1 / crossfadeValue);
        int numFrames = initNumFrames;
        int counter = 0;
        fadeIn.volume = 0;
        float changeVolumeAmount = Mathf.Lerp(0, 1f, crossfadeValue);
        crossfadeMaterial.SetFloat("Progress", 0);

        for(int i = 0; i < numFrames; i++)
        {
            //Flash button green/red
            if (counter % 20 == 0)
            {
                if (counter % 40 == 0) crossfadeMaterial.SetColor("ButtonColor", Color.green);
                else crossfadeMaterial.SetColor("ButtonColor", Color.red);
            }
            fadeIn.volume += changeVolumeAmount;
            fadeOut.volume -= changeVolumeAmount;

            crossfadeMaterial.SetFloat("Progress", (i / (float)numFrames));

            counter++;
            // Shit's broke
            if (counter > 10000)
            {
                Debug.Log("Breaking");
                break;
            }
            numFrames = (int)(1 / crossfadeValue);
            yield return null;
        }
        crossfadeMaterial.SetColor("ButtonColor", mac.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);
        fadeIn.volume = 1f;
        fadeOut.volume = 0f;
        //Stop(false);
        //inactiveAudioSource.clip = null;
        //crossfadeAudioCoroutines.Remove(crossfadeAudioCoroutines.Last());  //remove self on completion
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
        if(crossfadeAudioCoroutine != null) StopCoroutine(CrossfadeAudioSources());

        if (!TryPlayRequestedNext())
        {
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
        if (crossfadeAudioCoroutine != null) StopCoroutine(crossfadeAudioCoroutine);

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
        while (nowPlayingButtonID == newButtonID || mostRecentSongs.Contains(nowPlayingTab.MusicButtons[newButtonID].GetComponent<MusicButton>().Song.SortName))
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
            activeMp3Stream.Dispose();
            activeAudioSource.clip = null;
        }
        if(main == false)
        {
            inactiveAudioSource.Stop();
            inactiveMp3Stream.Dispose();
            inactiveAudioSource.clip = null;
        }
        else
        {
            activeAudioSource.Stop();
            inactiveAudioSource.Stop();
            try
            {
                activeMp3Stream.Dispose();
            }
            catch (NullReferenceException) { }
            try
            {
                inactiveMp3Stream.Dispose();
            }
            catch (NullReferenceException) { }
            activeAudioSource.clip = null;
            inactiveAudioSource.clip = null;
        }
        if (crossfadeAudioCoroutine != null) StopCoroutine(crossfadeAudioCoroutine);
        crossfadeMaterial.SetFloat("Progress", 0);
        songPath = "";
        songName = "";
        isPaused = false;
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
        print("playback time changed");
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
    }

    private void TryChangePlaybackTime(float val)
    {
        if (nextDelayTimer == 0 && val >= 0)
        {
            nextDelayTimer = 1;
            if (fileTypeIsMp3)
            {
                MpegFile streamToUse = usingInactiveAudioSource ? inactiveMp3Stream : activeMp3Stream;
                try
                {
                    long position = Convert.ToInt64(streamToUse.Length * (val / (2f / streamToUse.Channels)));
                    print(streamToUse.Channels);
                    if (streamToUse != null && position >= 0) streamToUse.Position = position;
                    activeAudioSource.time = Mathf.Min(val, 0.999f) * activeAudioSource.clip.length;    //0.999f beacuse going to exact last samples produces weird results
                    NowPlayingWebpage.SongPlaybackChanged(activeAudioSource.time);
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
                //TODO: broken
                NVorbis.VorbisReader streamToUse = usingInactiveAudioSource ? inactiveVorbisStream : activeVorbisStream;
                try
                {
                    if (streamToUse != null) streamToUse.DecodedPosition = Convert.ToInt64(streamToUse.TotalSamples * val);
                    activeAudioSource.time = val * activeAudioSource.clip.length;
                    NowPlayingWebpage.SongPlaybackChanged(activeAudioSource.time);
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
        if (nowPlayingTab == PlaylistTabs.selectedTab && nowPlayingButtonID == selectedId)
        {
            Stop();
            nowPlayingButtonID = -1;
        }
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
        //if (activeMp3Stream == inactiveMp3Stream) print("fucked 1");
        //if (activeAudioSource.clip == inactiveAudioSource.clip) print("fucked 2");
        //if (activeMp3Stream.ToString() == inactiveMp3Stream.ToString()) print("fucked 3");

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
        AudioSource aSourceToCheckCrossfade = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
        bool shouldStartCrossfade = false;
        if (aSourceToCheckCrossfade.clip)
        {

            shouldStartCrossfade = aSourceToCheckCrossfade.time > (aSourceToCheckCrossfade.clip.length - crossfadeTime);

            if (((!aSourceToCheckCrossfade.isPlaying && !isPaused) || (crossfade && shouldStartCrossfade)))
            {
                Next();
            }
            if (nextDelayTimer > 0)
            {
                nextDelayTimer -= 1;
            }
        }
    }

    private void OnApplicationQuit()
    {
        Stop();
        try
        {
            activeMp3Stream.Dispose();
        }
        catch(Exception) { }
        try
        {
            inactiveMp3Stream.Dispose();
        }
        catch (Exception) { }
        try
        {
            activeVorbisStream.Dispose();
        }
        catch (Exception) { }
        try
        {
            inactiveVorbisStream.Dispose();
        }
        catch (Exception) { }
    }
}

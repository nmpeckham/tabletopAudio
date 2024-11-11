﻿using Extensions;
using Id3;
using NLayer;
using NVorbis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

//Controls the playing of songs in the playlist
public class MusicController : MonoBehaviour
{
    private static MainAppController mac;

    public GameObject musicScrollView;

    public TMP_Text nowPlayingLabel;
    private static Image prevButtonImage = null;
    private static string songPath = "";
    public static string SongPath => songPath;
    private static string songName = "";
    public static string SongName => songName;
    internal static MusicButton nowPlayingButton = null;

    public Button fadeInButton;
    public Button fadeOutButton;
    public Slider localVolumeSlider;
    public Slider playbackScrubber;

    public static bool isPaused = false;
    public TMP_Text localVolumeLabel;

    //public List<GameObject> musicButtons;
    public Image musicStatusImage;
    public TMP_Text playbackTimerText;
    private static readonly string defaultPlaybackTimerText = "0:00/0:00";
    public Image shuffleImage;
    private static bool shuffle;
    public Image pauseButton;
    public Image playButton;
    private static bool crossfade = false;

    public Material crossfadeMaterial;

    private static AudioSource activeAudioSource;
    private static AudioSource inactiveAudioSource;
    private static MpegFile activeMp3Stream;
    private static MpegFile inactiveMp3Stream;
    private static VorbisReader activeVorbisStream;
    private static VorbisReader inactiveVorbisStream;

    //private const float fixedUpdateStep = 1 / 60f;
    private const float fixedUpdateTime = 50f;
    public PlaylistAudioSources plas;
    private static bool usingInactiveAudioSource = true;
    private static bool shouldStop1 = false;
    private static bool shouldStop2 = false;

    internal enum FileTypes
    {
        mp3,
        ogg,
        none
    }

    internal static FileTypes fileType = FileTypes.none;

    private static int nextDelayTimer = 0;

    private static bool fadeInMusicActive = false;
    private static bool fadeOutMusicActive = false;

    public TMP_InputField searchField;
    public Button clearPlaylistSearchButton;

    private static Coroutine crossfadeAudioCoroutine = null;

    public static List<PlayNextItem> mostRecentSongs = new();
    private static bool shouldDelayPlayNext = false;

    private static readonly List<PlayNextItem> playNextList = new();

    private static PlaylistTabs pt;

    private static PlaylistTab nowPlayingTab;

    public AudioMixerGroup musicAMG;
    public AudioMixerGroup masterAMG;

    private SearchFoldersController sfc;

    public Button AddMusicButton;


    public static PlaylistTab NowPlayingTab
    {
        get => nowPlayingTab;
        set
        {
            mostRecentSongs.Clear();
            nowPlayingTab = value;
        }
    }

    internal static List<MusicButton> searchButtons = null;

    private static float crossfadeTime = 0.01f;
    public float CrossfadeTime
    {
        get => (int)crossfadeTime;
        set => crossfadeTime = value;
    }

    public float MusicVolume
    {
        get
        {
            //throw new Exception();
            musicAMG.audioMixer.GetFloat("MusicVolume", out float outVal);
            return outVal.ToZeroOne();
        }

        set
        {
            if (value > 1f)
            {
                value = 1f;
            }
            if (value < 0f)
            {
                value = 0f;
            }
            musicAMG.audioMixer.SetFloat("MusicVolume", value.ToDB());
            localVolumeLabel.text = (value * 100f).ToString("N0") + "%";
            localVolumeSlider.SetValueWithoutNotify(value);
        }
    }

    private Coroutine checkForNewFilesRoutine = null;
    private static bool autoCheckForNewFiles = true;
    public bool AutoCheckForNewFiles
    {
        get => autoCheckForNewFiles;

        set
        {
            if (mac == null)
            {
                mac = GetComponent<MainAppController>();
            }

            LoadedFilesData.DeletedMusicClips.Clear();
            autoCheckForNewFiles = value;
            if (checkForNewFilesRoutine != null)
            {
                StopCoroutine(checkForNewFilesRoutine);
            }

            if (autoCheckForNewFiles && checkForNewFilesRoutine == null)
            {
                checkForNewFilesRoutine = StartCoroutine(CheckForNewFiles());
            }
        }
    }
    public bool Shuffle
    {
        get => shuffle;
        set
        {
            shuffle = value;
            if (shuffle)
            {
                Repeat = false;
                shuffleImage.color = ResourceManager.green;
            }
            else
            {
                shuffleImage.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white;
            }
        }
    }

    public bool Crossfade
    {
        get => crossfade;
        set
        {
            crossfade = value;
            if (crossfade)
            {
                crossfadeMaterial.SetColor("ButtonColor", Color.green);
            }
            else
            {
                if (crossfadeAudioCoroutine != null)
                {
                    StopCoroutine(crossfadeAudioCoroutine);
                }

                crossfadeMaterial.SetFloat("Progress", 0);
                crossfadeMaterial.SetColor("ButtonColor", MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white);
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

    public Image stopImage;

    private bool repeat = false;
    public bool Repeat
    {
        get => repeat;
        set
        {
            repeat = value;
            if (repeat)
            {
                stopImage.color = Color.green;
                Shuffle = false;
            }
            else
            {
                stopImage.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white;
            }
        }
    }

    public GameObject searchImage;
    public GameObject addNewTabLabelText;
    private float newSeekTime = -1;

    // Start is called before the first frame update
    internal void Init()
    {

        LoadedFilesData.DeletedMusicClips.Clear();
        LoadedFilesData.songs.Clear();
        LoadedFilesData.sfxClips.Clear();

        activeAudioSource = plas.a1;
        inactiveAudioSource = plas.a2;

        mac = GetComponent<MainAppController>();
        sfc = GetComponent<SearchFoldersController>();

        pt = GetComponent<PlaylistTabs>();
        checkForNewFilesRoutine = StartCoroutine(CheckForNewFiles());
        localVolumeSlider.onValueChanged.AddListener(LocalVolumeSliderChanged);
        playbackScrubber.onValueChanged.AddListener(PlaybackTimePercentChanged);
        fadeInButton.onClick.AddListener(StartFadeInMusicVolume);
        fadeOutButton.onClick.AddListener(StartFadeOutMusicVolume);

        StartCoroutine(Fft());
        StartCoroutine(UpdateStreamPosition());


        crossfadeMaterial.SetColor("ButtonColor", MainAppController.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);

        searchField.onValueChanged.AddListener(SearchTextEntered);
        searchField.onSelect.AddListener(SearchFieldHasFocus);
        searchField.onTextSelection.AddListener(SearchFieldHasFocus);
        searchField.onDeselect.AddListener(SearchFieldLostFocus);
        searchField.restoreOriginalTextOnEscape = false;
        clearPlaylistSearchButton.onClick.AddListener(ClearPlaylistSearch);
        playbackTimerText.text = defaultPlaybackTimerText;

        NowPlayingTab = PlaylistTabs.selectedTab;

        StartCoroutine(SkipAheadDueToEncodingError());
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
        pt.EnableSortButton();
        ActivateAllMusicButtons();
    }

    internal void TabChanged()
    {
        searchField.SetTextWithoutNotify("");
        searchField.Select();
        searchButtons = null;
    }
    internal void GiveSearchFieldFocus()
    {
        searchField.ActivateInputField();
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

    internal void SearchTextEntered(string text)
    {
        pt.EnableSortButton();
        ActivateAllMusicButtons();
        SearchFieldHasFocus("");
        pt.DisableSortButton();

        searchButtons = PlaylistTabs.selectedTab.musicContentView.GetComponentsInChildren<MusicButton>().ToList();

        ClearPlaylist();
        foreach (MusicButton mb in searchButtons)
        {
            if (mb.Song.artist != null)
            {
                if (mb.Song.artist.ToLower().Contains(text.ToLower()) || mb.Song.title.ToLower().Contains(text.ToLower()))
                {
                    mb.gameObject.SetActive(true);
                }
            }
            else
            {
                if (mb.Song.title.ToLower().Contains(text.ToLower()))
                {
                    mb.gameObject.SetActive(true);
                }
            }
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            pt.EnableSortButton();
            ActivateAllMusicButtons();
        }
    }

    private void ActivateAllMusicButtons()
    {
        foreach (MusicButton mb in PlaylistTabs.selectedTab.MusicButtons)
        {
            mb.gameObject.SetActive(true);
        }
    }

    private void ClearPlaylist()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("playlistItem"))
        {
            go.SetActive(false);
        }
    }

    private void StartFadeInMusicVolume()
    {
        StopCoroutine("FadeInMusicVolume");
        StopCoroutine("FadeOutMusicVolume");

        StartCoroutine("FadeInMusicVolume");
    }

    private void StartFadeOutMusicVolume()
    {
        StopCoroutine("FadeInMusicVolume");
        StopCoroutine("FadeOutMusicVolume");

        StartCoroutine("FadeOutMusicVolume");
    }

    private IEnumerator FadeInMusicVolume()
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
                    if (counter % 40 == 0)
                    {
                        fadeInButton.GetComponent<Image>().color = Color.green;
                    }
                    else
                    {
                        fadeInButton.GetComponent<Image>().color = Color.white;
                    }
                }
                MusicVolume += fadeOutValue;

                counter++;
                yield return new WaitForFixedUpdate();
            }
            MusicVolume = 1f;
        }
        fadeOutMusicActive = false;
        fadeInButton.GetComponent<Image>().color = Color.white;
        yield break;
    }

    private IEnumerator FadeOutMusicVolume()
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
            while (MusicVolume > 0.0001f)
            {
                if (counter % 20 == 0)
                {
                    if (counter % 40 == 0)
                    {
                        Debug.Log(MusicVolume);
                        fadeOutButton.GetComponent<Image>().color = Color.green;
                    }
                    else
                    {
                        fadeOutButton.GetComponent<Image>().color = Color.white;
                    }
                }
                MusicVolume = MusicVolume - fadeOutValue;

                counter++;
                yield return new WaitForFixedUpdate();
            }
            MusicVolume = 0f;
        }
        fadeOutMusicActive = false;
        fadeOutButton.GetComponent<Image>().color = Color.white;
        yield break;
    }

    private IEnumerator Fft()
    {
        // Much of this is based on making the fft display _look_ nice, rather than to be mathmatically correct
        int fftSize = 4096;
        float[] data0 = new float[fftSize];
        float[] data1 = new float[fftSize];
        //TODO re-adjust (last band shows nothing...) :/
        int[] segments = new int[] { 0, 3, 8, 13, 17, 21, 25, 29, 35, 42, 50, 59, 70, 84, 100, 119, 142, 170, 203, 242, 289, 344, 411, 490, 585, 698, 833, 995, 1187, 1417, 1691, 2019, 2409, 2826, 3132, 4096 };// }, 4889}; // Don't ask...
        double sum;
        int maxVal;
        int startVal;
        while (true)
        {
            //Sacrifice fft for framerate;
            if (mac.currentFPS < 30)
            {
                yield return null;
            }

            if (usingInactiveAudioSource)
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
                //print("segment: " + i);
                sum = 0f;
                startVal = segments[i];
                maxVal = segments[i + 1] - 1;
                //print("start val: " + startVal + ". end val: " + maxVal);
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

    private bool FileIsValid(string s, bool skipDupeCheck = false)
    {
        if(!LoadedFilesData.DeletedMusicClips.Any(f => f == s))
        {
            if ((Path.GetExtension(s) == ".mp3" || Path.GetExtension(s) == ".ogg") && !(s.Split(Path.DirectorySeparatorChar).ToList().Any(pathComponent => pathComponent[0].ToString() == ("~"))) && !(Application.isEditor && s.Contains("testRunner")))    //Ensure file ends with mp3 or ogg extension, doesn't have a path component that starts with ~, and isn't in a test mode
            {
                if ((!skipDupeCheck && !LoadedFilesData.songs.Any(song => song.FileName == s)) || skipDupeCheck)
                {
                    if (File.Exists(s))
                    {
                        return true;
                    }
                    else
                    {
                        mac.ShowErrorMessage("Couldn't find file " + s + ". Was it deleted?", 1);
                    }
                }
            }
        }

        return false;
    }

    internal void HideAddMusicButton()
    {
        AddMusicButton.gameObject.SetActive(false);
    }

    internal bool AddNewSong(String s, int tabId = 0, bool skipDupeCheck = false)
    {
        //print(s);
        if (FileIsValid(s, skipDupeCheck))
        {
            string artist = null;
            string title = null;
            TimeSpan duration = TimeSpan.Zero;
            if (Path.GetExtension(s) == ".mp3")
            {
                //Try both ID3 families
                Mp3 mp3File = new(s);
                Id3Tag newTag = null;
                try
                {
                    newTag = mp3File.GetTag(Id3TagFamily.Version2X);
                }
                catch (IndexOutOfRangeException) {}
                if (newTag == null)
                {
                    newTag = mp3File.GetTag(Id3TagFamily.Version1X);
                }
                else
                {
                    artist = newTag.Artists;
                    title = newTag.Title;
                    duration = newTag.Length;
                }

                TagLib.File altTag = null;
                try
                {
                    altTag = TagLib.File.Create(s);
                }
                catch(TagLib.CorruptFileException)
                { 
                    mac.ShowErrorMessage("couldn't add song " + s + ". The file may be corrupt", 1);
                    LoadedFilesData.DeletedMusicClips.Add(s);
                    return false; 
                }


                if(altTag != null)
                {
                    if(duration == TimeSpan.Zero)
                    {
                        duration = altTag.Properties.Duration;
                    }
                    if(altTag.Tag.Performers.Length > 0 && string.IsNullOrEmpty(artist)) {
                        artist = altTag.Tag.Performers[0];
                        //If duration not present in tag, get from temp mpeg stream
                        if (duration == TimeSpan.Zero)
                        {
                            MpegFile temp = new(s);
                            duration = temp.Duration;
                            temp.Dispose();
                        }
                    }
                }
                mp3File.Dispose();
            }
            //ID3 info not supported for ogg vorbis, get duration from temp vorbis stream
            else if (Path.GetExtension(s) == ".ogg")
            {
                try
                {
                    VorbisReader temp = new(s);
                    duration = temp.TotalTime;
                    temp.Dispose();
                }
                catch(InvalidDataException) 
                { 
                    mac.ShowErrorMessage("couldn't add song " + s + ". The file may be corrupt", 1);
                    LoadedFilesData.DeletedMusicClips.Add(s);
                    return false; 
                }
            }
            if (title == null || string.IsNullOrWhiteSpace(title))
            {
                title = Path.GetFileName(s);
            }
            //Check for characters not currently in loaded character set (except space (32) or null (0))

            if (artist != null && artist.ToList().Any(item =>
            {
                if (item != 32 || item == 0)
                {
                    return false;
                }

                if (!ResourceManager.charTable.Contains(item))
                {
                    print(item);
                    return true;

                }
                return false;
            }))
            {
                ShowUnsupportedCharactersError(s);
            }
            if (title != null && title.ToList().Any(item =>
            {
                if (item == 32 || item == 0)
                {
                    return false;
                }

                if (!ResourceManager.charTable.Contains(item))
                {
                    return true;
                }
                return false;
            }))
            {
                ShowUnsupportedCharactersError(s);
            }
            else if (title == null && s.ToList().Any(item =>
            {
                if (item == 32 || item == 0)
                {
                    return false;
                }

                if (!ResourceManager.charTable.Contains(item))
                {
                    print(item);

                    return true;
                }
                return false;
            }))
            {
                ShowUnsupportedCharactersError(s); 
            }
            Song newSong = new(s, title, duration, null, artist);
            LoadedFilesData.songs.Add(newSong);

            pt.AddSongToPlaylist(tabId, newSong);
            return true;    //song was added
        }
        return false;       //song was not added
    }

    private void ShowUnsupportedCharactersError(string s)
    {
        mac.ShowErrorMessage("Song " + s + " contains characters not currently supported. They will be displayed as rectangles", 1);
    }

    private void DeleteSong(string s)
    {
        LoadedFilesData.songs.RemoveAll(song => song.FileName == s);
        foreach (PlaylistTab t in PlaylistTabs.tabs)
        {
            List<MusicButton> buttons = t.MusicButtons.FindAll(mb => mb.Song.FileName == s);
            buttons.ForEach(mb =>
            {
                DeleteItem(mb);
            });
        }
    }

    internal void StopActiveSongCheck()
    {
        if(checkForNewFilesRoutine != null)
        {
            StopCoroutine(checkForNewFilesRoutine);
        }
    }

    internal void StartActiveSongCheck()
    {
        if(autoCheckForNewFiles)
        {
            checkForNewFilesRoutine = StartCoroutine(CheckForNewFiles());
        }
    }
    private IEnumerator CheckForNewFiles()
    {
        List<string> toDelete = new();
        EnumerationOptions ignoreCaseOptions = new EnumerationOptions();
        ignoreCaseOptions.MatchCasing = MatchCasing.CaseInsensitive;
        while (true)
        {

            if (autoCheckForNewFiles)
            {
                bool spinnerShown = false;
                int i = 0;
                foreach (Song s in LoadedFilesData.songs)
                {
                    //File was deleted
                    if (!File.Exists(s.FileName))
                    {
                        toDelete.Add(s.FileName);
                        continue;
                    }

                    if (!sfc.searchFolders.Contains(Path.GetDirectoryName(s.FileName)))
                    {
                        toDelete.Add(s.FileName);
                    }
                    i++;
                    if (i % 25 == 0) yield return null;
                }

                foreach (string d in toDelete)
                {
                    DeleteSong(d);
                }
                toDelete.Clear();

                yield return null;

                int numberOfSongsAdded = 0;
                float startTime = Time.realtimeSinceStartup;
                string[] searchFolders = new string[sfc.searchFolders.Count];
                Array.Copy(sfc.searchFolders.ToArray(), searchFolders, searchFolders.Length);
                i = 0;
                yield return null;

                GameObject checkForSongsSpinner = null;
                foreach (string folder in searchFolders)
                {
                    string[] files1 = Directory.GetFiles(folder, "*.ogg", ignoreCaseOptions);
                    string[] files2 = Directory.GetFiles(folder, "*.mp3", ignoreCaseOptions);
                    IEnumerable allFiles = files1.Concat(files2);
                    foreach (string file in allFiles)
                    {
                        //print(file);
                        if (!LoadedFilesData.DeletedMusicClips.Contains(file))
                        {
                            if (!LoadedFilesData.songs.Any(song => song.FileName == file))
                            {
                                if (AddNewSong(file, 0, false))
                                {
                                    if (!spinnerShown)
                                    {
                                        checkForSongsSpinner = mac.ShowErrorMessage("Loading Tracks", 2, 1000, true);
                                        spinnerShown = true;
                                    }
                                    numberOfSongsAdded++;
                                    yield return null;
                                }
                                if (!AutoCheckForNewFiles)
                                {
                                    break;
                                }
                            }
                        }
                        i++;
                        //print(i);
                        //print(i % 20 == 0);
                        //wait for next frame every 25 songs, framerate help
                    }
                }
                if (numberOfSongsAdded > 0)
                {
                    mac.ShowErrorMessage("Added " + numberOfSongsAdded + " songs in " + (Time.realtimeSinceStartup - startTime).ToString("N2") + " seconds", 2, 5);
                }
                if (checkForSongsSpinner != null)
                {
                    checkForSongsSpinner.GetComponent<ErrorMessage>().StartFadeOutNow();
                }
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    internal void PlaylistItemSelected(MusicButton selectedSong)
    {
        if (nowPlayingTab != PlaylistTabs.selectedTab)
        {
            ChangeCurrentlyPlayingTab(PlaylistTabs.selectedTab.tabId);
        }
        ItemSelected(selectedSong);
    }

    internal void ChangeCurrentlyPlayingTab(int id)
    {
        NowPlayingTab = PlaylistTabs.tabs[id];
    }

    public void ItemSelected(MusicButton selectedButton)
    {



        if (nowPlayingTab.MusicButtons.Count > 0)
        {
            ChangePlaybackScrubberActive(true);
            try
            {
                nowPlayingButton = selectedButton;
                Song selectedSong = selectedButton.Song;
                songPath = System.IO.Path.Combine(MainAppController.workingDirectories["musicDirectory"], selectedButton.Song.FileName);
                songName = selectedButton.Song.SortName;

                if (selectedSong != null)
                {
                    songPath = System.IO.Path.Combine(MainAppController.workingDirectories["musicDirectory"], selectedSong.FileName);
                    songName = selectedSong.SortName;
                }
                else
                {
                    selectedSong = selectedButton.Song;
                }

                AudioClip clip = null;
                long totalLength = 0;
                if (crossfade)
                {
                    if (Path.GetExtension(songPath) == ".mp3")
                    {
                        fileType = FileTypes.mp3;
                        if (usingInactiveAudioSource)
                        {
                            activeMp3Stream = new(songPath);
                            totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                            if (totalLength > int.MaxValue)
                            {
                                totalLength = int.MaxValue;
                            }

                            clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                            SetupInterfaceForPlay(activeAudioSource, clip);
                        }
                        else
                        {
                            inactiveMp3Stream = new(songPath);
                            totalLength = inactiveMp3Stream.Length / (inactiveMp3Stream.Channels * 4);
                            if (totalLength > int.MaxValue)
                            {
                                totalLength = int.MaxValue;
                            }

                            clip = AudioClip.Create(songPath, (int)totalLength, inactiveMp3Stream.Channels, inactiveMp3Stream.SampleRate, true, InactiveMP3Callback);
                            SetupInterfaceForPlay(inactiveAudioSource, clip);
                        }

                        usingInactiveAudioSource = !usingInactiveAudioSource;
                    }
                    else if (Path.GetExtension(songPath) == ".ogg")
                    {
                        fileType = FileTypes.ogg;
                        if (usingInactiveAudioSource)
                        {
                            activeVorbisStream = new NVorbis.VorbisReader(songPath);
                            totalLength = activeVorbisStream.TotalSamples;
                            if (totalLength > int.MaxValue)
                            {
                                totalLength = int.MaxValue;
                            }

                            clip = AudioClip.Create(songPath, (int)totalLength, activeVorbisStream.Channels, activeVorbisStream.SampleRate, true, ActiveVorbisCallback);
                            SetupInterfaceForPlay(activeAudioSource, clip);
                        }
                        else
                        {
                            inactiveVorbisStream = new NVorbis.VorbisReader(songPath);
                            totalLength = inactiveVorbisStream.TotalSamples;
                            if (totalLength > int.MaxValue)
                            {
                                totalLength = int.MaxValue;
                            }

                            clip = AudioClip.Create(songPath, (int)totalLength, inactiveVorbisStream.Channels, inactiveVorbisStream.SampleRate, true, InactiveVorbisCallback);
                            SetupInterfaceForPlay(inactiveAudioSource, clip);
                        }

                        usingInactiveAudioSource = !usingInactiveAudioSource;
                    }
                    else
                    {
                        activeAudioSource.clip = null;
                    }
                    UpdateMostRecentlyPlayed(selectedButton);
                }
                else
                {
                    inactiveAudioSource.Stop();
                    if (Path.GetExtension(songPath) == ".mp3")
                    {
                        fileType = FileTypes.mp3;
                        try
                        {
                            print(crossfade);
                            print("Disposing inactive stream 1");
                            inactiveMp3Stream.Dispose();
                        }
                        catch (NullReferenceException) { }
                        activeMp3Stream = new(songPath);
                        totalLength = activeMp3Stream.Length / (activeMp3Stream.Channels * 4);
                        if (totalLength > int.MaxValue)
                        {
                            totalLength = int.MaxValue;
                        }

                        clip = AudioClip.Create(songPath, (int)totalLength, activeMp3Stream.Channels, activeMp3Stream.SampleRate, true, ActiveMP3Callback);
                    }
                    else if (Path.GetExtension(songPath) == ".ogg")
                    {
                        fileType = FileTypes.ogg;
                        try
                        {
                            print("Disposing inactive stream 2");
                            inactiveVorbisStream.Dispose();
                        }
                        catch (NullReferenceException) { }
                        activeVorbisStream = new NVorbis.VorbisReader(songPath);
                        totalLength = activeVorbisStream.TotalSamples;
                        if (totalLength > int.MaxValue)
                        {
                            totalLength = int.MaxValue;
                        }

                        clip = AudioClip.Create(songPath, (int)totalLength, activeVorbisStream.Channels, activeVorbisStream.SampleRate, true, ActiveVorbisCallback);
                    }
                    else
                    {
                        activeAudioSource.clip = null;
                    }
                    activeAudioSource.volume = 1f;
                    SetupInterfaceForPlay(activeAudioSource, clip);
                    usingInactiveAudioSource = false;

                    UpdateMostRecentlyPlayed(selectedButton);
                }

                playButton.color = ResourceManager.green;
                pauseButton.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

                //TODO: Correct for mono/stereo audio?
                NowPlayingWebpage.SongChanged(selectedSong);
            }
            catch (IndexOutOfRangeException e)
            {
                mac.ShowErrorMessage("Encoding Type Invalid: 0. " + e.Message, 0);
            }
            catch (ArgumentException e)
            {
                mac.ShowErrorMessage("Encoding Type Invalid: 1. " + e.Message, 0);
            }
        }
    }

    private bool TryPlayRequestedNext()
    {
        if (playNextList.Count > 0)
        {
            if (playNextList[0].playlistTabId != nowPlayingTab.tabId)
            {
                ChangeCurrentlyPlayingTab(playNextList[0].playlistTabId);
            }
            ItemSelected(playNextList[0].mb);
            playNextList.RemoveAt(0);

            return true;
        }
        return false;
    }

    internal void AddToPlayNext(PlayNextItem item)
    {
        playNextList.Insert(0, item);
    }

    private void SetupInterfaceForPlay(AudioSource aSource, AudioClip clip = null)
    {
        if (crossfade)
        {
            if (inactiveAudioSource == activeAudioSource)
            {
                print("Fucked");
            }

            if (crossfadeAudioCoroutine != null)
            {
                StopCoroutine(crossfadeAudioCoroutine);
            }

            crossfadeAudioCoroutine = StartCoroutine(CrossfadeAudioSources());
        }
        aSource.clip = clip;
        aSource.time = 0;

        playbackScrubber.SetValueWithoutNotify(0f);
        aSource.Play();
        isPaused = false;


        Image buttonImage = nowPlayingButton.gameObject.GetComponent<Image>();
        if (prevButtonImage != null)
        {
            prevButtonImage.color = ResourceManager.musicButtonGrey;
        }

        buttonImage.color = ResourceManager.red;

        prevButtonImage = buttonImage;
        nowPlayingLabel.text = songName;
        musicStatusImage.sprite = mac.playImage;
        GetComponent<ScrollNowPlayingTitle>().SongChanged();
    }

    private IEnumerator CrossfadeAudioSources()
    {
        AudioSource fadeOut = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
        AudioSource fadeIn = usingInactiveAudioSource ? activeAudioSource : inactiveAudioSource;
        int counter = 0;
        fadeIn.volume = 0;
        crossfadeMaterial.SetFloat("Progress", 0);
        float startTime = Time.unscaledTime;
        float initialFadeOutVolume = fadeOut.volume;

        while (fadeIn.volume < 1)
        {
            if (isPaused)
            {
                break;
            }
            float progress = (Time.unscaledTime - startTime) / crossfadeTime;
            //Flash button green/red
            if (counter % 20 == 0)
            {
                if (counter % 40 == 0)
                {
                    crossfadeMaterial.SetColor("ButtonColor", Color.green);
                }
                else
                {
                    crossfadeMaterial.SetColor("ButtonColor", Color.red);
                }
            }
            fadeIn.volume = progress;
            fadeOut.volume = Mathf.Max(0f, (1 - progress) * initialFadeOutVolume);  //This right here? This is the magic of TableTopManager!

            crossfadeMaterial.SetFloat("Progress", progress);

            counter++;
            yield return null;
        }
        //Required to release file lock ("... File is currently open in TableTopManager)"
        //Causes bugs tho :/
        //if (!usingInactiveAudioSource)
        //{
        //    inactiveAudioSource.Stop();
        //    try
        //    {
        //        print("Disposing inactive stream 3");
        //        //inactiveMp3Stream.Dispose();
        //    }
        //    catch (ObjectDisposedException) { print("disposed"); }
        //    catch (NullReferenceException) { print("null"); }
        try
        {
            print("Disposing inactive stream 4");
            inactiveVorbisStream.Dispose();
        }
        catch (ObjectDisposedException) { print("disposed"); }
        catch (NullReferenceException) { print("null"); }
        //}
        //else
        //{
        //    activeAudioSource.Stop();
        //    try
        //    {
        //        print("Disposing inactive stream 3");

        //        //activeMp3Stream.Dispose();
        //    }
        //    catch (ObjectDisposedException) { print("disposed"); }
        //    catch (NullReferenceException) { print("null"); }
        try
        {
            print("Disposing inactive stream 3");

            activeVorbisStream.Dispose();
        }
        catch (ObjectDisposedException) { print("disposed"); }
        catch (NullReferenceException) { print("null"); }
        //}
        crossfadeMaterial.SetColor("ButtonColor", MainAppController.darkModeEnabled ? Crossfade ? Color.green : ResourceManager.darkModeGrey : ResourceManager.lightModeGrey);
        crossfadeMaterial.SetFloat("Progress", 0);

        fadeIn.volume = 1f;
        fadeOut.volume = 0f;
        fadeOut.Stop();
    }

    int hasEncodingError = 0;
    IEnumerator SkipAheadDueToEncodingError()
    {
        while(true)
        {
            if(hasEncodingError > 3)
            {
                float currentPlaybackPercent = GetPercentPlayed();
                print("skipping ahead");
                SkipAheadTenthOfSecond();
                mac.ShowErrorMessage("This track contains encoding errors! Skipping ahead");
                hasEncodingError = 0;
            }
            yield return null;
        }

    }

    int NoSamplesReadFrameCount = 0;
    private void ActiveMP3Callback(float[] data)
    {
        try
        {
            int samplesRead = activeMp3Stream.ReadSamples(data, 0, data.Length);
            //if(samplesRead == 0)
            //{
            //    hasEncodingError++;
            //}
        }
        catch (NullReferenceException e)
        {
            shouldStop1 = true;
            mac.ShowErrorMessage("Error decoding audio from active MP3 stream, stopping playback", 0);
            print("error 1");
            print(data.Length);
            throw e;
        }
        catch (IndexOutOfRangeException e)
        {
            AudioSource sourceToUse = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
            //shouldStop1 = true;
            print("error 1");
            print(data.Length);
        }
        catch (ObjectDisposedException e)
        {
            print("error 1");
            print(data.Length);
            shouldStop1 = true;
        }
    }

    private void InactiveMP3Callback(float[] data)
    {
        try
        {
            int samplesRead = inactiveMp3Stream.ReadSamples(data, 0, data.Length);
            //if (samplesRead == 0)
            //{
            //    hasEncodingError++;
            //}
        }
        catch (NullReferenceException e)
        {
            shouldStop2 = true;
            mac.ShowErrorMessage("Error decoding audio from inactive MP3 stream, stopping playback", 0);
            print("error 1");
            print(data.Length);
            throw e;
        }
        catch (IndexOutOfRangeException e)
        {
            AudioSource sourceToUse = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
            sourceToUse.Stop();
            sourceToUse.Play();
            print("error 2");
            print(data.Length);
            throw e;
        }
        catch (ObjectDisposedException e)
        {
            print("error 2");
            print(data.Length);
            shouldStop2 = true;
            print(e.StackTrace);
            throw e;
        }
    }

    private void ActiveVorbisCallback(float[] data)
    {
        activeVorbisStream.ReadSamples(data, 0, data.Length);
    }

    private void InactiveVorbisCallback(float[] data)
    {
        inactiveVorbisStream.ReadSamples(data, 0, data.Length);
    }

    public void Next()
    {
        if (crossfadeAudioCoroutine != null)
        {
            StopCoroutine(CrossfadeAudioSources());
        }

        if (repeat)
        {
            ItemSelected(nowPlayingButton);
        }

        else if (!TryPlayRequestedNext())
        {
            if (nowPlayingTab.MusicButtons.Count > 1)
            {

                if (shuffle)
                {
                    StartCoroutine(ShuffleSelectNewSong());
                }

                else
                {
                    if (nowPlayingButton.buttonId == nowPlayingTab.MusicButtons.Count - 1)
                    {
                        ItemSelected(nowPlayingTab.MusicButtons[0]);
                    }
                    else
                    {
                        ItemSelected(nowPlayingTab.MusicButtons[nowPlayingButton.buttonId + 1]);
                    }
                }
            }

            else
            {
                playButton.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
                playbackScrubber.SetValueWithoutNotify(0);
                pauseButton.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
                nowPlayingLabel.text = "";
                playbackTimerText.text = defaultPlaybackTimerText;
                if(prevButtonImage != null)
                {
                    prevButtonImage.color = ResourceManager.musicButtonGrey;
                }
            }
        }
    }

    public void Previous()
    {
        if (crossfadeAudioCoroutine != null)
        {
            StopCoroutine(crossfadeAudioCoroutine);
        }

        if (nowPlayingTab.MusicButtons.Count > 0)
        {
            if (shuffle)
            {
                if (mostRecentSongs.Count > 1 && mostRecentSongs[^2].mb.Song.SortName != SongName)
                {
                    ItemSelected(mostRecentSongs[^2].mb);
                    mostRecentSongs.RemoveAt(mostRecentSongs.Count - 2);
                }
                else
                {
                    StartCoroutine(ShuffleSelectNewSong());
                }
            }

            else
            {
                if (nowPlayingButton != null)
                {
                    ItemSelected(nowPlayingTab.MusicButtons[nowPlayingButton.buttonId - 1]);
                }
                else
                {
                    ItemSelected(nowPlayingTab.MusicButtons.Last());
                }
            }
            playbackScrubber.SetValueWithoutNotify(0);
            pauseButton.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

        }
    }

    private IEnumerator ShuffleSelectNewSong()
    {
        if(nowPlayingTab.MusicButtons.Count > 0)
        {
            MusicButton newButton = nowPlayingTab.MusicButtons[UnityEngine.Random.Range(0, nowPlayingTab.MusicButtons.Count)];
            CheckMostRecentlyPlayed();
            int i = 0;
            while (nowPlayingButton == newButton || mostRecentSongs.Any(item => item.mb == newButton))
            {
                newButton = nowPlayingTab.MusicButtons[UnityEngine.Random.Range(0, nowPlayingTab.MusicButtons.Count)];
                i++;
                if (i > 50)
                {
                    print("oof");
                    break;
                }
            }
            ItemSelected(newButton);
            yield return null;
        }
        yield return null;
    }

    // Remove first two items if mostRecentSongs length has met or exceeded # of musicButtons
    // Removing two adds some randomness. Removing just one results in playing songs in the same order forever
    // Also trim to store only 500 most recently played.
    private void CheckMostRecentlyPlayed()
    {
        if (mostRecentSongs.Count >= nowPlayingTab.MusicButtons.Count)
        {
            mostRecentSongs.RemoveRange(0, 2);
        }
        if (mostRecentSongs.Count > 500)
        {
            mostRecentSongs.RemoveRange(500, mostRecentSongs.Count - 499);
        }
    }

    private void UpdateMostRecentlyPlayed(MusicButton newSong)
    {
        if (!mostRecentSongs.Any(item => item.mb == newSong))
        {
            mostRecentSongs.Add(new PlayNextItem(newSong, nowPlayingTab.tabId));
        }
    }

    public void Stop(bool? main = null)
    {
        if (main == true)
        {
            activeAudioSource.Stop();
            print("Disposing inactive stream 3");

            activeMp3Stream.Dispose();
            activeAudioSource.clip = null;
        }
        if (main == false)
        {
            inactiveAudioSource.Stop();
            print("Disposing inactive stream 3");

            inactiveMp3Stream.Dispose();
            inactiveAudioSource.clip = null;
        }
        else
        {
            activeAudioSource.Stop();
            inactiveAudioSource.Stop();
            try
            {
                print("Disposing inactive stream 3");

                activeMp3Stream.Dispose();
            }
            catch (NullReferenceException) { print("null2"); }
            try
            {
                print("Disposing inactive stream 3");

                inactiveMp3Stream.Dispose();
            }
            catch (NullReferenceException) { print("null3"); }
            activeAudioSource.clip = null;
            inactiveAudioSource.clip = null;
            playbackTimerText.text = defaultPlaybackTimerText;
        }
        if (!crossfade)
        {
            usingInactiveAudioSource = !usingInactiveAudioSource;
        }

        if (crossfadeAudioCoroutine != null)
        {
            StopCoroutine(crossfadeAudioCoroutine);
        }

        crossfadeMaterial.SetFloat("Progress", 0);
        songPath = "";
        songName = "";
        isPaused = false;
        if (prevButtonImage != null)
        {
            prevButtonImage.color = ResourceManager.musicButtonGrey;
        }

        musicStatusImage.sprite = mac.stopImage;
        nowPlayingLabel.text = "";
        playButton.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
        pauseButton.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;
        NowPlayingWebpage.SongStopped();
    }

    public void Pause()
    {
        AudioSource sourceToUse = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;

        if (isPaused)
        {
            isPaused = false;
            Play();
            NowPlayingWebpage.SongUnpaused(sourceToUse.time);
        }
        else if (sourceToUse.isPlaying)
        {
            pauseButton.color = ResourceManager.orange;
            sourceToUse.Pause();
            isPaused = true;
            if (prevButtonImage != null)
            {
                prevButtonImage.color = ResourceManager.orange;
            }

            musicStatusImage.sprite = mac.pauseImage;
            NowPlayingWebpage.SongPaused(sourceToUse.time);
        }
    }

    public void SpacebarPressed()
    {
        if (activeAudioSource.clip == null)
        {
            Play();
        }
        else
        {
            Pause();
        }
    }

    public void Play()
    {
        AudioSource sourceToUse = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
        if (sourceToUse.clip != null)
        {
            sourceToUse.UnPause();
            isPaused = false;
            if (prevButtonImage != null)
            {
                prevButtonImage.color = ResourceManager.red;
            }

            musicStatusImage.sprite = mac.playImage;
            pauseButton.color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : ResourceManager.lightModeGrey;

        }
        else if (TryPlayRequestedNext())
        {
            return;
        }
        else if (shuffle)
        {
            StartCoroutine(ShuffleSelectNewSong());
        }
        else
        {
            ChangeCurrentlyPlayingTab(PlaylistTabs.selectedTab.tabId);
            if(nowPlayingTab.MusicButtons.Count > 0)
            {
                PlaylistItemSelected(nowPlayingTab.MusicButtons.First());
            }
        }
    }

    internal void StartNewFile()
    {
        Shuffle = false;
        Crossfade = false;
        AutoCheckForNewFiles = true;
        fileType = FileTypes.none;
        playbackScrubber.SetValueWithoutNotify(0f);
    }

    public void RefreshSongOrder(int oldID, int newID)
    {
        //print(newID);
        MusicButton item = PlaylistTabs.selectedTab.MusicButtons[oldID];

        PlaylistTabs.selectedTab.MusicButtons[newID].buttonId = oldID;
        PlaylistTabs.selectedTab.MusicButtons[oldID].buttonId = newID;


        //Put button in correct position in list

        if (PlaylistTabs.selectedTab == nowPlayingTab)
        {
            //print(PlaylistTabs.selectedTab) ;
            if(nowPlayingButton != null)
            {
                if (newID == nowPlayingButton.buttonId)
                {
                    nowPlayingButton.buttonId = oldID;
                }
                else if (oldID == nowPlayingButton.buttonId)
                {
                    nowPlayingButton.buttonId = newID;
                }
            }

        }

        PlaylistTabs.selectedTab.MusicButtons.Remove(item);
        PlaylistTabs.selectedTab.MusicButtons.Insert(newID, item);
    }



    private void PlaybackTimePercentChanged(float val)
    {
        float value = Mathf.Clamp(val, 0f, 1f);
        if (Mathf.Abs(value) > .995f)
        {
            shouldDelayPlayNext = true;
        }
        else
        {
            try
            {
                TryChangePlaybackPercent(value);
            }
            catch (IndexOutOfRangeException) { print("outofrange"); }
        }
    }

    private void SkipAheadTenthOfSecond()
    {
        if(fileType == FileTypes.mp3)
        {
            MpegFile streamToUse = usingInactiveAudioSource ? inactiveMp3Stream : activeMp3Stream;
            double newPercent = (streamToUse.Time.TotalSeconds + 0.2f) / streamToUse.Duration.TotalSeconds;
            TryChangePlaybackPercent((float)newPercent);
        }
        else if(fileType == FileTypes.ogg)
        {
            NVorbis.VorbisReader streamToUse = usingInactiveAudioSource ? inactiveVorbisStream : activeVorbisStream;
            double newPercent = (streamToUse.DecodedTime.TotalSeconds + 0.2f) / streamToUse.TotalTime.TotalSeconds;
            TryChangePlaybackPercent((float)newPercent);
        }
    }

    private void TryChangePlaybackPercent(float percent)
    {
        print(percent.ToString());
        if (nextDelayTimer == 0 && percent >= 0f && percent < 99f)
        {
            
            AudioSource sourceToUse = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
            if (fileType == FileTypes.mp3)
            {
                try
                {
                    newSeekTime = percent;
                    NowPlayingWebpage.SongPlaybackChanged(sourceToUse.time);
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
            else if (fileType == FileTypes.ogg)
            {

                NVorbis.VorbisReader streamToUse = usingInactiveAudioSource ? inactiveVorbisStream : activeVorbisStream;
                try
                {
                    if (streamToUse != null)
                    {
                        // calling Stop() then Play() gets unity to discard the current audio buffer, resulting in snappy seeking.
                        sourceToUse.Stop();
                        streamToUse.DecodedPosition = Convert.ToInt64(streamToUse.TotalSamples * percent);
                        playbackScrubber.SetValueWithoutNotify(streamToUse.TotalSamples * percent);
                        sourceToUse.Play();
                        if (isPaused) sourceToUse.Pause();
                    }
                    //NowPlayingWebpage.SongPlaybackChanged(sourceToUse.time);
                }
                catch (NullReferenceException e)
                {
                    print(e);
                }
            }
            else
            {
                playbackScrubber.SetValueWithoutNotify(0f);
            }
        }
    }

    IEnumerator UpdateStreamPosition()
    {
        bool err = false;
        while(true)
        {
            if(nextDelayTimer > 0)
            {
                yield return null;
            }
            else if(newSeekTime >= 0f)
            {
                
                AudioSource sourceToUse = usingInactiveAudioSource ? inactiveAudioSource : activeAudioSource;
                MpegFile streamToUse = usingInactiveAudioSource ? inactiveMp3Stream : activeMp3Stream;
                if(streamToUse != null)
                {
                    long positionInSamples = Convert.ToInt64(streamToUse.Length * newSeekTime);
                    yield return null;
                    print(positionInSamples);
                    // calling Stop() then Play() gets unity to discard the current audio buffer, resulting in snappy seeking.

                    try
                    {
                        nextDelayTimer = 1;
                        print(positionInSamples);
                        streamToUse.Position = Math.Max(10, positionInSamples);
                    }
                    catch (ObjectDisposedException)
                    {
                        err = true;
                    }
                    if (!err)
                    {
                        sourceToUse.Stop();
                        playbackScrubber.SetValueWithoutNotify(Convert.ToSingle(positionInSamples) / Convert.ToSingle(streamToUse.Length));
                        yield return null;
                        sourceToUse.Play();
                        if (isPaused) sourceToUse.Pause();

                    }
                    
                    yield return null;
                }
                newSeekTime = -1f;
                yield return null;
            }
            else
            {
                yield return null;
            }
        }

    }

    void UpdatePlaybackScrubberPosition()
    {
        float val = GetPercentPlayed();
        playbackScrubber.SetValueWithoutNotify(val);
    }

    float GetPercentPlayed()
    {
        float percentPlayed = 0f;
        dynamic streamToUse = GetActiveAudioStream();
        try
        {
            if (streamToUse is MpegFile)
            {
                percentPlayed = streamToUse.Position / (float)streamToUse.Length;
            }
            else if (streamToUse is VorbisReader)
            {
                percentPlayed = streamToUse.DecodedPosition / (float)streamToUse.TotalSamples;
            }
            else
            {
                percentPlayed = 0f;
            }
        }
        catch (NullReferenceException e)
        {
            print(e);
        }
        catch (ArgumentOutOfRangeException e)
        {
            print(e);
        }
        return percentPlayed;
    }


    // My code is _so_ good that it only needs one frame to catch up and not crash because of nLayer
    private IEnumerator DelayAndPlayNext()
    {
        print("Delaying and playing next");
        shouldDelayPlayNext = false;
        yield return null;
        Next();
        nextDelayTimer = 1;
    }

    // TODO sus
    internal void DeleteItem(MusicButton selectedButton)
    {
        LoadedFilesData.DeletedMusicClips.Add(selectedButton.Song.FileName);
        if (nowPlayingTab == PlaylistTabs.selectedTab && nowPlayingButton == selectedButton)
        {
            Stop();
            nowPlayingButton = null;
        }
        playNextList.RemoveAll(a => a.mb == selectedButton);
        Destroy(selectedButton.gameObject);
        PlaylistTabs.selectedTab.MusicButtons.Remove(selectedButton);
        //Re-number music buttons in tabs
        int currentID = 0;
        foreach (MusicButton mb in PlaylistTabs.selectedTab.MusicButtons)
        {
            mb.buttonId = currentID;
            currentID++;
        }
    }

    dynamic GetActiveAudioStream()
    {
        if (fileType == FileTypes.mp3)
        {
            return usingInactiveAudioSource ? inactiveMp3Stream : activeMp3Stream;
        }
        else if (fileType == FileTypes.ogg)
        {
            return usingInactiveAudioSource ? inactiveVorbisStream : activeVorbisStream;
        }
        return null;
    }

    void UpdatePlaybackTimeText()
    {
        dynamic streamToUse = GetActiveAudioStream();

        if(streamToUse is MpegFile)
        {
            if(streamToUse.Duration.TotalSeconds > 3600)
            {
                playbackTimerText.text = (streamToUse.Time.Hours).ToString() + ":" + (streamToUse.Time.Minutes).ToString("D2") + ":" + (streamToUse.Time.Seconds).ToString("D2") + "/" + (streamToUse.Duration.Hours).ToString() + ":" + (streamToUse.Duration.Minutes).ToString("D2") + ":" + (streamToUse.Duration.Seconds).ToString("D2");
            }
            else
            {
                playbackTimerText.text = (streamToUse.Time.Minutes).ToString() + ":" + (streamToUse.Time.Seconds).ToString("D2") + "/" + (streamToUse.Duration.Minutes).ToString() + ":" + (streamToUse.Duration.Seconds).ToString("D2");
            }
        }
        else if(streamToUse is VorbisReader)
        {
            if (streamToUse.TotalTime.TotalSeconds > 3600)
            {
                playbackTimerText.text = (streamToUse.DecodedTime.Hours).ToString() + ":" + (streamToUse.DecodedTime.Minutes).ToString("D2") + ":" + (streamToUse.DecodedTime.Seconds).ToString("D2") + "/" + (streamToUse.TotalTime.Hours).ToString() + ":" + (streamToUse.TotalTime.Minutes).ToString("D2") + ":" + (streamToUse.TotalTime.Seconds).ToString("D2");
            }
            else
            {
                playbackTimerText.text = (streamToUse.DecodedTime.Minutes).ToString() + ":" + (streamToUse.DecodedTime.Seconds).ToString("D2") + "/" + (streamToUse.TotalTime.Minutes).ToString() + ":" + (streamToUse.TotalTime.Seconds).ToString("D2");
            }
        }
    }

    private void FixedUpdate()
    {
        UpdatePlaybackScrubberPosition();
        UpdatePlaybackTimeText();
    }

    // Update is called once per frame
    private void Update()
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

        if (shouldDelayPlayNext)
        {
            if (!Input.GetMouseButton(0))
            {
                StartCoroutine(DelayAndPlayNext());
            }
        }

        if (mac.controlKeys.Any(key => Input.GetKey(key)) && searchButtons != null)
        {
            searchImage.SetActive(true);
            addNewTabLabelText.transform.localPosition = new Vector2(-6.27f, 0f);
        }
        else
        {
            searchImage.SetActive(false);
            addNewTabLabelText.transform.localPosition = new Vector2(0f, 0f);
        }
    }
    private int i = 0;
    private void LateUpdate()
    {
        dynamic aSourceToCheckCrossfade = GetActiveAudioStream();
        bool shouldStartCrossfade = false;
        if (aSourceToCheckCrossfade is MpegFile)
        {
            if (crossfade) shouldStartCrossfade = aSourceToCheckCrossfade.Time.TotalSeconds > (aSourceToCheckCrossfade.Duration.TotalSeconds - crossfadeTime);
            else
            {
                shouldStartCrossfade = aSourceToCheckCrossfade.Time.TotalMilliseconds >= aSourceToCheckCrossfade.Duration.TotalMilliseconds - 100;
            }

        }
        else if (aSourceToCheckCrossfade is VorbisReader)
        {
            if (crossfade) shouldStartCrossfade = aSourceToCheckCrossfade.DecodedTime.TotalSeconds > (aSourceToCheckCrossfade.TotalTime.TotalSeconds - crossfadeTime);
            else shouldStartCrossfade = aSourceToCheckCrossfade.DecodedTime.TotalMilliseconds >= aSourceToCheckCrossfade.TotalTime.TotalMilliseconds;
        }



        if (shouldStartCrossfade)
        {
            Next();
        }
        if (nextDelayTimer > 0)
        {
            nextDelayTimer--;
        }
        i++;
    }

    private void OnApplicationQuit()
    {
        Stop();
        try
        {
            activeMp3Stream.Dispose();
        }
        catch (Exception) { }
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

    internal void ChangePlaybackScrubberActive(bool activate)
    {
        playbackScrubber.interactable = activate;
    }
}

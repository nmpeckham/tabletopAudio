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
    public AudioSource aSource;

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

    private VolumeController vc;

    private bool autoCheckForNewFiles = false;

    MpegFile mp3Stream;
    VorbisReader vorbisStream;

    int buttonWithCursor;
    private GameObject activeRightClickMenu;

    public GameObject fftParent;
    public FftBar[] pieces;

    public GameObject TooltipParent;
    public int ButtonWithCursor
    {
        get
        {
            return buttonWithCursor;
        }
        set
        {
            //Debug.Log(value);
            if (buttonWithCursor != value)
            {
                //Destroy(activeRightClickMenu);
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

    // Start is called before the first frame update
    void Start()
    {
        pieces = fftParent.GetComponentsInChildren<FftBar>();
        mac = Camera.main.GetComponent<MainAppController>();
        buttonWithCursor = -1;
        vc = GetComponent<VolumeController>();
        musicButtons = new List<GameObject>();
        StartCoroutine("CheckForNewFiles");
        localVolumeSlider.onValueChanged.AddListener(ChangeLocalVolume);
        playbackScrubber.onValueChanged.AddListener(PlaybackTimeValueChanged);

        StartCoroutine(Fft());
    }

    IEnumerator Fft()
    {
        int fftSize = 512;
        float[] data = new float[fftSize];
        while (true)
        {
            aSource.GetSpectrumData(data, 0, FFTWindow.BlackmanHarris);
            for (int i = 0; i < 4;)
            {
                float sum = 0;
                for (int j = 0; j < Mathf.Pow(2, i) * 2; j++)
                {
                    sum += data[i + j];
                }
                sum /= Mathf.Pow(2, i) * 2;
                StartCoroutine(AdjustScale(sum * Mathf.Pow(1.2f, i + 1) * 3.5f, pieces[i].transform));   
                i++;
            }
            yield return new WaitForSecondsRealtime(0.03f);
        }
    }

    IEnumerator AdjustScale(float newScale, Transform obj)
    {
        float oldScale = obj.localScale.y;
        for(int i = 0; i < 5; i++) {
            obj.localScale = new Vector3(1, Mathf.Min(Mathf.Lerp(oldScale, newScale, i / 5f), 1));
            yield return new WaitForEndOfFrame();
        }
        yield return null;
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
        while(true)
        {
            if (autoCheckForNewFiles)
            {
                List<GameObject> toDelete = new List<GameObject>();
                foreach (string s in LoadedFilesData.musicClips)
                {
                    //Debug.Log(s);

                    string[] files = System.IO.Directory.GetFiles(mac.musicDirectory);
                    //Debug.Log(files[20]);
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
                foreach(GameObject g in musicButtons)
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
        nowPlayingButtonID = id;
        if (musicButtons.Count > 0)
        {
            try
            {
                //Debug.Log(musicButtons.Count);
                //Debug.Log(nowPlayingButtonID);
                MusicButton button = musicButtons[nowPlayingButtonID].GetComponent<MusicButton>();
                songPath = System.IO.Path.Combine(mac.musicDirectory, button.FileName);
                songName = button.FileName;
                AudioClip clip = null;
                long totalLength = 0;

                if (Path.GetExtension(songPath) == ".mp3")
                {
                    vorbisStream = null;
                    mp3Stream = new MpegFile(songPath);

                    totalLength = mp3Stream.Length / (mp3Stream.Channels * 4);
                    //Debug.Log(mp3Stream.Length > int.MaxValue);
                    if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                    clip = AudioClip.Create(songPath, (int)totalLength, mp3Stream.Channels, mp3Stream.SampleRate, true, MP3Callback);
                    //clip. = AudioClipLoadType.DecompressOnLoad();
                    aSource.clip = clip;
                    SetupInterfaceForPlay();
                    //StartCoroutine(FFTDisplay());
                }
                else if(Path.GetExtension(songPath) == ".ogg")
                {
                    mp3Stream = null;
                    vorbisStream = new NVorbis.VorbisReader(songPath);
                    totalLength = vorbisStream.TotalSamples;
                    if (totalLength > int.MaxValue) totalLength = int.MaxValue;
                    clip = AudioClip.Create(songPath, (int)totalLength, vorbisStream.Channels, vorbisStream.SampleRate, true, VorbisCallback);
                    aSource.clip = clip;
                    SetupInterfaceForPlay();
                }
                else
                {
                    aSource.clip = null;
                }

        }
            catch (IndexOutOfRangeException e)
        {
            //Debug.Log(e.Message);
            mac.ShowErrorMessage("Encoding Type Invalid: 0. " + e.Message);
        }
        catch (ArgumentException e)
        {
            //Debug.Log(e.Message);
            mac.ShowErrorMessage("Encoding Type Invalid: 1. " + e.Message);
        }
        catch (Exception e)
        {
            //Debug.Log(e.Message);
            mac.ShowErrorMessage("Unknown exception: 2. " + e.Message);
        }
    }
    }

    internal void ClearPlaylist()
    {
        foreach(GameObject mb in musicButtons)
        {
            Destroy(mb);
        }
    }

    void SetupInterfaceForPlay()
    {
        
        aSource.time = 0;
        aSource.Play();
        playbackScrubber.value = 0;
        Image buttonImage = musicButtons[nowPlayingButtonID].GetComponent<Image>();
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.musicButtonGrey;
        buttonImage.color = ResourceManager.red;

        prevButtonImage = buttonImage;
        //Debug.Log(channels);
        nowPlayingLabel.text = songName;
        musicStatusImage.sprite = mac.playImage;
    }

    void MP3Callback(float[] data)
    {
        try
        {
            mp3Stream.ReadSamples(data, 0, data.Length);
        }
        catch(NullReferenceException)
        {
            Stop();
        }
    }

    void VorbisCallback(float[] data)
    {
        vorbisStream.ReadSamples(data, 0, data.Length);
    }

    public void Next()
    {
        if(shuffle)
        {
            int newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
            while(nowPlayingButtonID == newButtonID)
            {
                newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
            }
            nowPlayingButtonID = newButtonID;
            ItemSelected(nowPlayingButtonID);
        }
        else
        {
            if (nowPlayingButtonID == musicButtons.Count - 1)
            {
                nowPlayingButtonID = 0;
                ItemSelected(0);
            }
            else
            {
                nowPlayingButtonID++;
                ItemSelected(nowPlayingButtonID);
            }

        }
        playbackScrubber.value = 0;
    }

    public void Previous()
    {
        if(musicButtons.Count > 0)
        {
            playbackScrubber.value = 0;
            if (shuffle)
            {
                int newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
                while (nowPlayingButtonID == newButtonID)
                {
                    newButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count);
                }
                nowPlayingButtonID = newButtonID;
                ItemSelected(nowPlayingButtonID);
            }

            else
            {
                if (nowPlayingButtonID > 0)
                {
                    nowPlayingButtonID--;
                    ItemSelected(nowPlayingButtonID);
                }
                else
                {
                    nowPlayingButtonID = musicButtons.Count - 1;
                    ItemSelected(nowPlayingButtonID);
                }
            }
        }
    }

    public void Stop()
    {
        aSource.Stop();
        isPaused = false;
        aSource.clip = null;
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.musicButtonGrey;
        if(mp3Stream != null) mp3Stream.Dispose();
        if (vorbisStream != null) vorbisStream.Dispose();
        musicStatusImage.sprite = mac.stopImage;
        nowPlayingLabel.text = "";
    }

    public void Pause()
    {
        aSource.Pause();
        isPaused = true;
        if (prevButtonImage != null) prevButtonImage.color = ResourceManager.orange;
        musicStatusImage.sprite = mac.pauseImage;
    }

    public void Play()
    {
        if(aSource.clip != null)
        {
            aSource.UnPause();
            isPaused = false;
            if (prevButtonImage != null) prevButtonImage.color = ResourceManager.red;
            musicStatusImage.sprite = mac.playImage;
        }
        else if(shuffle)
        {
            nowPlayingButtonID = UnityEngine.Random.Range(0, LoadedFilesData.musicClips.Count - 1);
            ItemSelected(nowPlayingButtonID);
        }
        else ItemSelected(0);
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
        aSource.volume = masterVolume * musicVolume;
    }

    private void ChangeLocalVolume(float newLocalVolume)
    {
        if (localVolumeSlider.value != newLocalVolume) localVolumeSlider.value = newLocalVolume;
        musicVolume = newLocalVolume;
        aSource.volume = masterVolume * musicVolume;
        localVolumeLabel.text = (musicVolume * 100).ToString("N0");
    }

    private void PlaybackTimeValueChanged(float val)
    {
        if(aSource.clip != null)
        {
            try
            {
                if (Mathf.Abs(val - (aSource.time / aSource.clip.length)) > 0.01)
                {
                    if (mp3Stream != null) mp3Stream.Position = Convert.ToInt64(mp3Stream.Length * val);
                    else if (vorbisStream != null) vorbisStream.DecodedPosition = Convert.ToInt64(vorbisStream.TotalSamples * val);
                    aSource.time = val * aSource.clip.length;
                }
            }
            catch(NullReferenceException)
            {
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!aSource.isPlaying && aSource.clip != null && !isPaused)
        {
            prevButtonImage.color = ResourceManager.musicButtonGrey;
            if (nowPlayingButtonID < musicButtons.Count - 1)
            {
                int newbuttonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : nowPlayingButtonID + 1;
                while (newbuttonID == nowPlayingButtonID)
                {
                    newbuttonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : nowPlayingButtonID + 1;
                }
                nowPlayingButtonID = newbuttonID;
                ItemSelected(nowPlayingButtonID);
            }
            else
            {

                int newbuttonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : 0;
                while(newbuttonID == nowPlayingButtonID)
                {
                    newbuttonID = shuffle ? UnityEngine.Random.Range(0, musicButtons.Count - 1) : 0;
                }
                nowPlayingButtonID = newbuttonID;
                ItemSelected(nowPlayingButtonID);
            }

        }
        if(aSource.clip != null)
        {
            playbackScrubber.value = aSource.time / aSource.clip.length;
            playbackTimerText.text = Mathf.Floor(aSource.time / 60).ToString() + ":" + (Mathf.FloorToInt(aSource.time % 60)).ToString("D2") + "/" + Mathf.FloorToInt(aSource.clip.length / 60) + ":" + Mathf.FloorToInt(aSource.clip.length % 60).ToString("D2");
        }
    }
    public void ShowRightClickMenu(int id)
    {
        toDeleteId = id;
        //StopAllCoroutines();
        if(activeRightClickMenu) Destroy(activeRightClickMenu);
        activeRightClickMenu = Instantiate(playlistRightClickMenuPrefab, Input.mousePosition, Quaternion.identity, TooltipParent.transform);
        StartCoroutine(CheckMousePos(Input.mousePosition));
    }

    IEnumerator CheckMousePos(Vector3 mousePos)
    {
        while(true)
        {
            if(Vector3.Distance(mousePos, Input.mousePosition) > 80) {
                Destroy(activeRightClickMenu);
                break;
            }
            if(mac.currentMenuState == MainAppController.MenuState.none && Input.GetKey(KeyCode.Escape))
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
        foreach(GameObject mbObj in musicButtons)
        {
            MusicButton mb = mbObj.GetComponent<MusicButton>();
            mb.id = currentID;
            currentID++;
        }
        Destroy(activeRightClickMenu);
    }
}

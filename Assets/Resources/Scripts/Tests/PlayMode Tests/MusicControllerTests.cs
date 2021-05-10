using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class MusicControllerTests
{
    bool sceneLoaded = false;
    MusicController mc;
    MainAppController mac;

    [OneTimeSetUp]
    public void Setup()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    IEnumerator AwaitLevelLoad()
    {
        //wait for scene to load
        while (Camera.main.name == null)
        {
            yield return null;
        }
        Camera.main.GetComponent<OptionsMenuController>().LoadItemSelected(@"D:\Music\TableTopAudio\saves\testRunner.xml");

        //wait for save to load
        yield return null;
        while (MusicController.NowPlayingTab == null)
        {
            yield return null;
        }
        mc = Camera.main.GetComponent<MusicController>();
        mac = Camera.main.GetComponent<MainAppController>();
        sceneLoaded = true;
    }

    [UnityTest]
    public IEnumerator AssertPlayButtonPlaysCorrectSong()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Play();
        Assert.IsFalse(MusicController.isPaused);
        Assert.AreEqual("Aberdeen - It Was Here", MusicController.SongName);
    }

    [UnityTest]
    public IEnumerator AssertPlayNextButtonPlaysNextSong()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Play();
        Next();
        Assert.IsFalse(MusicController.isPaused);
        Assert.AreEqual("Colbie Caillat  Bubbly", MusicController.SongName);
    }

    [UnityTest]
    public IEnumerator AssertPauseButtonPausesSong()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Play();
        Pause();
        Assert.IsTrue(MusicController.isPaused);
        Assert.AreEqual("Aberdeen - It Was Here", MusicController.SongName);
    }

    [UnityTest]
    public IEnumerator AssertStopButtonStopsSong()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Play();
        Stop();
        Assert.IsFalse(MusicController.isPaused);
        Assert.AreEqual(string.Empty, MusicController.SongName);
    }

    [UnityTest]
    public IEnumerator AssertPreviousButtonPlaysPreviousSong()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Play();
        Previous();
        Assert.IsFalse(MusicController.isPaused);
        Assert.AreEqual("Wintergatan - 07 Starmachine2000", MusicController.SongName);
    }

    [UnityTest]
    public IEnumerator AssertShuffleButtonEnablesShuffle()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Shuffle();
        Assert.IsTrue(mc.Shuffle);
    }

    [UnityTest]
    public IEnumerator AssertShuffleButtonTwiceDisablesShuffle()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Shuffle();
        Shuffle();
        Assert.IsFalse(mc.Shuffle);
    }

    [TearDown]
    public void Teardown()
    {
        MusicController mc = Camera.main.GetComponent<MusicController>();
        mc.Stop();
    }

    private void Play()
    {
        mac.ControlButtonClicked("PLAY-MUSIC");
    }

    private void Pause()
    {
        mac.ControlButtonClicked("PAUSE-MUSIC");
    }

    private void Next()
    {
        mac.ControlButtonClicked("NEXT");
    }

    private void Previous()
    {
        mac.ControlButtonClicked("PREVIOUS");
    }

    private void Shuffle()
    {
        mac.ControlButtonClicked("SHUFFLE");
    }

    private void Stop()
    {
        mac.ControlButtonClicked("STOP-MUSIC");
    }
}

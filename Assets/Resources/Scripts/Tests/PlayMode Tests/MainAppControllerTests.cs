using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MainAppControllerTests
{
    // A Test behaves as an ordinary method
    bool sceneLoaded = false;

    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    IEnumerator AwaitLevelLoad()
    {
        while (Camera.main.name == null)
        {
            yield return null;
        }
        sceneLoaded = true;
    }
    [UnityTest]
    public IEnumerator AssertNumberOfSFXPagesAndButtons()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        MainAppController mac = Camera.main.GetComponent<MainAppController>();

        Assert.AreEqual(mac.pageButtons.Count, 8);   //8 sfx page tabs
        Assert.AreEqual(mac.pageParents.Count, 8);    //8 pages of sfx buttons
        foreach (SFXPage buttonPage in mac.pageParents)
        {
            Assert.AreEqual(buttonPage.buttons.Count, 35);  //35 buttons per page
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator AssertUintToColorReturnsColor()
    {
        if (!sceneLoaded) yield return AwaitLevelLoad();
        Debug.Log(MainAppController.UIntToColor(0xFFF6768E));
        Assert.AreEqual(new Color(246f / 255f, 118f / 255f, 142f / 255f, 1f), MainAppController.UIntToColor(0xFFF6768E));
    }
}

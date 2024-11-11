using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SFXPageController : MonoBehaviour
{
    internal static int activePage;

    public GameObject pageParentParent; // parent for page parents
    public List<SFXPage> pageParents;
    public GameObject pageButtonParent;
    public List<GameObject> pageButtons;

    public Button optionsMenuButton;
    public Button stopSFXButton;

    private static int pagesMade;


    // Start is called before the first frame update
    internal void Init()
    {
        pageParents = new List<SFXPage>();
        pageButtons = new List<GameObject>();

        MakeSFXButtons();

        pageParents[0].gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES);

    }

    internal void MakeSFXButtons()
    {
        pageParents.ForEach(pp =>
        {
            pp.buttons.ForEach(btn =>
            {
                Destroy(btn);
            });
            pp.buttons.Clear();
        });
        foreach (GameObject g in pageButtons)
        {
            Destroy(g);
        }

        foreach (SFXPage o in pageParents)
        {
            Destroy(o.gameObject);
        }

        pageButtons.Clear();
        pageParents = new List<SFXPage>();
        for (pagesMade = 0; pagesMade < MainAppController.NUMPAGES;)    // + 1 to add page for remote trigger page;
        {
            MakePage();
        }
    }

    private void MakePage()
    {
        GameObject pageButton = Instantiate(Prefabs.pageButtonPrefab, pageButtonParent.transform);

        PageButton pb = pageButton.GetComponent<PageButton>();
        pb.id = pagesMade;
        pb.Init();

        pb.Label = (pagesMade + 1).ToString();



        pageButtons.Add(pageButton);
        pageButton.transform.SetSiblingIndex(pagesMade + 1);

        GameObject pp = Instantiate(Prefabs.pageParentPrefab, pageParentParent.transform);

        pp.name += " " + pagesMade;
        pp.GetComponent<SFXPage>().pageId = pagesMade;
        pageParents.Add(pp.GetComponent<SFXPage>());

        for (int j = 0; j < MainAppController.NUMBUTTONS; j++)
        {
            GameObject button = Instantiate(Prefabs.sfxButtonPrefab, pageParents[pagesMade].transform);
            SFXButton btn = button.GetComponent<SFXButton>();
            btn.id = j;
            btn.page = pagesMade;
            btn.Init();

            pp.GetComponent<SFXPage>().buttons.Add(button);
        }
        if (pagesMade == 0)
        {
            pageButton.GetComponent<Image>().color = Color.red;
        }
        optionsMenuButton.GetComponent<PageButton>().RefreshOrder();
        stopSFXButton.GetComponent<PageButton>().RefreshOrder();

        optionsMenuButton.GetComponent<PageButton>().Init();
        stopSFXButton.GetComponent<PageButton>().Init();

        if (pagesMade == MainAppController.NUMPAGES - 1)
        {
            pb.Label = "Remote Triggers";
            //print("assigning page parent");
            //print(pp.name);
            GetComponent<RemoteTriggerController>().remoteTriggerPage = pageButton;
            GetComponent<RemoteTriggerController>().pageParent = pp;

            StartCoroutine(pb.StartTimerToMakeInactive());
        }

        pagesMade++;
    }

    internal void StopAll()
    {
        foreach (SFXPage page in pageParents)
        {
            foreach (GameObject sfxButton in page.buttons)
            {
                sfxButton.GetComponent<SFXButton>().Stop();
            }
        }
    }

    internal void ChangeSFXPage(int pageID)
    {
        pageButtons[activePage].GetComponent<Image>().color = MainAppController.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white;
        activePage = pageID;
        pageButtons[activePage].GetComponent<Image>().color = ResourceManager.red;
        pageParents[activePage].transform.SetSiblingIndex(MainAppController.NUMPAGES - 1);
    }
}

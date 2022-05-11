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
        for (int i = 0; i < MainAppController.NUMPAGES; i++)
        {
            GameObject pageButton = Instantiate(Prefabs.pageButtonPrefab, pageButtonParent.transform);
            pageButton.GetComponentInChildren<TMP_Text>().text = (i + 1).ToString();
            pageButton.GetComponent<PageButton>().id = i;
            pageButton.GetComponent<PageButton>().Init();

            pageButtons.Add(pageButton);
            pageButton.transform.SetSiblingIndex(i + 1);

            GameObject pp = Instantiate(Prefabs.pageParentPrefab, pageParentParent.transform);
            pp.name += " " + i;
            pp.GetComponent<SFXPage>().pageId = i;
            pageParents.Add(pp.GetComponent<SFXPage>());

            for (int j = 0; j < MainAppController.NUMBUTTONS; j++)
            {
                GameObject button = Instantiate(Prefabs.sfxButtonPrefab, pageParents[i].transform);
                SFXButton btn = button.GetComponent<SFXButton>();
                btn.id = j;
                btn.page = i;
                btn.Init();

                pp.GetComponent<SFXPage>().buttons.Add(button);
            }
            if (i == 0)
            {
                pageButton.GetComponent<Image>().color = Color.red;
            }
        }
        optionsMenuButton.GetComponent<PageButton>().RefreshOrder();
        stopSFXButton.GetComponent<PageButton>().RefreshOrder();
        optionsMenuButton.GetComponent<PageButton>().Init();
        stopSFXButton.GetComponent<PageButton>().Init();

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//tab 0 = all songs, tab -1 = add tab button
public class PlaylistTabs : MonoBehaviour
{
    public PlaylistTab mainTab;
    public PlaylistTab addNewTab;

    public GameObject musicButtonContentParent;
    public GameObject buttonParent;

    [SerializeField]
    internal static List<PlaylistTab> tabs = new List<PlaylistTab>();

    public GameObject playlistTabParent;
    private static MusicController mc;
    private static MainAppController mac;

    public Sprite tabSelectedSprite;
    public Sprite tabUnselectedSprite;

    internal static PlaylistTab selectedTab;

    public TMP_InputField textField;
    public Button cancelButton;
    public Button confirmButton;

    public GameObject editTabLabelPanel;

    PlaylistTab nowEditing;

    internal void Init()
    {
        mainTab.Init();
        tabs.Clear();
        tabs.Add(mainTab);
        mainTab.musicContentView = GameObject.Find("MusicContentView");
        mac = GetComponent<MainAppController>();
        selectedTab = mainTab;
        mc = GetComponent<MusicController>();
        cancelButton.onClick.AddListener(CancelNameChange);
        confirmButton.onClick.AddListener(ConfirmNameChange);
    }
    internal PlaylistTab TabClicked(int id)
    {
        if (id == -1)    // add new tab
        {
            if(tabs.Count < 6)
            {
                addNewTab.gameObject.SetActive(true);
                GameObject newTabObj = Instantiate(Prefabs.playlistTabPrefab, playlistTabParent.transform);
                addNewTab.gameObject.transform.SetSiblingIndex(100);
                tabs.Add(newTabObj.GetComponent<PlaylistTab>());
                newTabObj.GetComponent<Image>().sprite = tabUnselectedSprite;
                PlaylistTab newTab = newTabObj.GetComponent<PlaylistTab>();
                newTab.tabId = tabs.Count - 1;
                newTab.Init();


                GameObject newContentView = Instantiate(Prefabs.musicContentViewPrefab, musicButtonContentParent.transform);
                newContentView.SetActive(false);
                newTab.musicContentView = newContentView;
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());

                return newTab;
            }
            else if(tabs.Count == 6)
            {
                addNewTab.gameObject.SetActive(false); 
            }
        }
        else
        {
            ChangeTab(id);
        }
        return null;
    }

    void ChangeTab(int newTab)
    {
        SetTabSize(newTab);
        mc.TabChanged();
        //TODO: This is destroyed on save load:
        //print(selectedTab.musicContentView.transform.childCount);
        foreach (Transform t in selectedTab.musicContentView.transform)
        {
            t.gameObject.SetActive(true);
        }
        selectedTab.musicContentView.SetActive(false);
        selectedTab = tabs[newTab];
        selectedTab.musicContentView.SetActive(true);
    }

    void SetTabSize(int newTab)
    {
        PlaylistTab currentTab = selectedTab;
        RectTransform nr = currentTab.GetComponent<RectTransform>();
        if(currentTab.tabId > 0) nr.sizeDelta = new Vector2(75, 40);
        currentTab.GetComponent<Image>().color = new Color(120 / 255f, 120 / 255f, 120 / 255f);    //TODO: move color to resourceManager
        currentTab.GetComponent<Image>().sprite = tabUnselectedSprite;

        currentTab = tabs[newTab];
        currentTab.GetComponent<Image>().color = new Color(200 / 255f, 200 / 255f, 200 / 255f);

        nr = currentTab.GetComponent<RectTransform>();
        if (currentTab.tabId > 0)
        {
            currentTab.GetComponent<Image>().sprite = tabSelectedSprite;
            nr.sizeDelta = new Vector2(90, 40);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());
    }

    internal void EditTabName(PlaylistTab tab)
    {
        if(tab.tabId > 0)
        {
            textField.text = tab.LabelText;
            editTabLabelPanel.SetActive(true);
            mac.currentMenuState = MainAppController.MenuState.editTabLabel;
            nowEditing = tab;
            textField.ActivateInputField();
        }
    }

    internal void ConfirmNameChange()
    {
        nowEditing.LabelText = textField.text;
        editTabLabelPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    internal void CancelNameChange()
    {
        textField.text = "";
        editTabLabelPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    internal void AddSongToPlaylist(int tabId, Song song)
    {
        GameObject mbObj = Instantiate(Prefabs.musicButtonPrefab, tabs[tabId].musicContentView.transform);
        MusicButton mb = mbObj.GetComponent<MusicButton>();
        mb.Init();
        mb.buttonId = tabs[tabId].MusicButtons.Count;
        tabs[tabId].MusicButtons.Add(mb);
        mb.Song = song;
    }

    internal void DeleteAllTabs()
    {
        foreach(PlaylistTab t in tabs)
        {
            if(t.tabId > 0)
            {
                Destroy(t.gameObject);
            }
        }
        tabs.RemoveAll(t => t.tabId > 0);
    }
}

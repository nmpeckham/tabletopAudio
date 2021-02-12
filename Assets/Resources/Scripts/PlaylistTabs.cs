using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlaylistTabs : MonoBehaviour
{
    public PlaylistTab mainTab;
    public PlaylistTab addNewTab;

    public GameObject musicButtonContentParent;

    internal List<PlaylistTab> tabs = new List<PlaylistTab>();

    public GameObject playlistTabParent;
    public GameObject playlistTabPrefab;
    private MusicController mc;
    private MainAppController mac;

    public Sprite tabSelectedSprite;
    public Sprite tabUnselectedSprite;

    internal PlaylistTab selectedTab;

    public TMP_InputField textField;
    public Button cancelButton;
    public Button confirmButton;

    public GameObject editTabLabelPanel;

    PlaylistTab nowEditing;

    internal void Init()
    {
        tabs.Add(mainTab);
        mainTab.musicContentView = GameObject.Find("MusicContentView");
        mac = GetComponent<MainAppController>();
        selectedTab = mainTab;
        mc = GetComponent<MusicController>();
        cancelButton.onClick.AddListener(CancelNameChange);
        confirmButton.onClick.AddListener(ConfirmNameChange);
    }
    internal void TabClicked(int id)
    {
        if (id == -1)    // add new tab
        {
            GameObject newTabObj = Instantiate(playlistTabPrefab, playlistTabParent.transform);
            addNewTab.gameObject.transform.SetSiblingIndex(100);
            tabs.Add(newTabObj.GetComponent<PlaylistTab>());
            newTabObj.GetComponent<Image>().sprite = tabUnselectedSprite;
            PlaylistTab newTab = newTabObj.GetComponent<PlaylistTab>();
            newTab.tabId = tabs.Count - 1;

            GameObject newContentView = Instantiate(Prefabs.musicContentViewPrefab, musicButtonContentParent.transform);
            newContentView.SetActive(false);
            newTab.musicContentView = newContentView;
        }
        else
        {
            ChangeTab(id);
        }
    }

    void ChangeTab(int newTab)
    {
        SetTabSize(newTab);
        selectedTab.musicContentView.SetActive(false);
        selectedTab = tabs[newTab];
        selectedTab.musicContentView.SetActive(true);
    }

    void SetTabSize(int newTab)
    {
        PlaylistTab currentTab = selectedTab;
        RectTransform nr = currentTab.GetComponent<RectTransform>();
        nr.sizeDelta = new Vector2(80, 100);
        currentTab.GetComponent<Image>().color = new Color(120 / 255f, 120 / 255f, 120 / 255f);    //TODO: move color to resourceManager
        currentTab.GetComponent<Image>().sprite = tabUnselectedSprite;

        currentTab = tabs[newTab];
        currentTab.GetComponent<Image>().color = new Color(200 / 255f, 200 / 255f, 200 / 255f);
        currentTab.GetComponent<Image>().sprite = tabSelectedSprite;
        nr = currentTab.GetComponent<RectTransform>();
        nr.sizeDelta = new Vector2(110, 100);
    }

    internal void EditTabName(PlaylistTab tab)
    {
        textField.text = tab.LabelText;
        editTabLabelPanel.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.editTabLabel;
        nowEditing = tab;
        textField.ActivateInputField();
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
        mb.buttonId = tabs[tabId].Playlist.Count;
        tabs[tabId].Playlist.Add(song);
        mb.Init();
        mb.Song = song;


    }
}

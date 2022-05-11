using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//tab 0 = all songs, tab -1 = add tab button
public class PlaylistTabs : MonoBehaviour
{

    public PlaylistTab mainTab;
    public PlaylistTab addNewTab;

    public GameObject musicButtonContentParent;
    public GameObject buttonParent;

    internal static List<PlaylistTab> tabs = new();

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
    private int tabsCreated = 1;
    public DisableClickDragScroll dcds;
    public GameObject musicLoadingAnimation;
    public Button sortButton;

    internal PlaylistTab NowEditing
    {
        get;
        set;
    }

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
        musicLoadingAnimation.SetActive(true);

    }
    internal void TabClicked(int id)
    {
        if (id == -1)    // add new tab
        {
            if (tabs.Count < 6)
            {
                addNewTab.gameObject.SetActive(true);
                GameObject newTabObj = Instantiate(Prefabs.playlistTabPrefab, playlistTabParent.transform);
                addNewTab.gameObject.transform.SetSiblingIndex(100);    //arbitrarily high to move to end of list
                tabs.Add(newTabObj.GetComponent<PlaylistTab>());
                newTabObj.GetComponent<Image>().sprite = tabUnselectedSprite;
                PlaylistTab newTab = newTabObj.GetComponent<PlaylistTab>();
                newTab.tabId = tabs.Count - 1;
                newTab.Init();
                newTab.LabelText = tabsCreated++.ToString();

                GameObject newContentView = Instantiate(Prefabs.musicContentViewPrefab, musicButtonContentParent.transform);
                newContentView.SetActive(false);
                newTab.musicContentView = newContentView;
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());

                // Add all songs from current search to new tab, make label the search term
                if (mac.controlKeys.Any(key => Input.GetKey(key)))
                {
                    int i = 0;
                    if (MusicController.searchButtons != null)
                    {
                        foreach (MusicButton mb in MusicController.searchButtons)
                        {
                            if (mb.gameObject.activeInHierarchy)
                            {
                                AddSongToPlaylist(newTab.tabId, mb.Song);
                                if (i == 0)
                                {
                                    newTab.LabelText = mc.searchField.text;
                                }

                                i++;
                            }
                        }
                    }
                }
            }
            if (tabs.Count == 6)
            {
                addNewTab.gameObject.SetActive(false);
            }
        }
        else
        {
            ChangeTab(id);
        }
    }

    internal void SortSongs()
    {
        List<MusicButton> sortedButtons = selectedTab.MusicButtons.OrderBy(mb => mb.Song.SortName).ToList();

        int newId = 0;
        bool nowPlayingUpdated = false;
        foreach (MusicButton mb in sortedButtons)
        {
            // ensure now playing ID for music controller is correct, so next/prev works as expected
            if (MusicController.nowPlayingButton != null && MusicController.nowPlayingButton.buttonId == mb.buttonId && MusicController.NowPlayingTab.tabId == selectedTab.tabId && !nowPlayingUpdated)
            {
                MusicController.nowPlayingButton.buttonId = newId;
                nowPlayingUpdated = true;
            }
            mb.buttonId = newId;
            mb.gameObject.transform.SetSiblingIndex(newId);
            newId++;
        }
        selectedTab.MusicButtons = sortedButtons;
    }

    private void ChangeTab(int newTab)
    {
        SetTabSize(newTab);
        mc.TabChanged();
        EnableSortButton();
        foreach (Transform t in selectedTab.musicContentView.transform)
        {
            t.gameObject.SetActive(true);
        }
        selectedTab.musicContentView.SetActive(false);
        selectedTab = tabs[newTab];
        selectedTab.musicContentView.SetActive(true);
        dcds.content = tabs[newTab].musicContentView.GetComponent<RectTransform>();
    }

    private void SetTabSize(int newTab)
    {
        PlaylistTab currentTab = selectedTab;
        currentTab.GetComponent<Image>().color = ResourceManager.tabInactiveColor;
        currentTab.GetComponent<Image>().sprite = tabUnselectedSprite;

        currentTab = tabs[newTab];
        currentTab.GetComponent<Image>().color = ResourceManager.tabActiveColor;

        currentTab.GetComponent<Image>().sprite = tabSelectedSprite;
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());
    }

    internal void EditTabName(PlaylistTab tab = null)
    {
        if (tab == null)
        {
            tab = NowEditing;
        }

        if (tab.tabId > 0)
        {
            textField.text = tab.LabelText;
            editTabLabelPanel.SetActive(true);
            mac.currentMenuState = MainAppController.MenuState.editTabLabel;
            NowEditing = tab;
            textField.ActivateInputField();
        }
    }

    internal void ConfirmNameChange()
    {
        NowEditing.LabelText = textField.text;
        editTabLabelPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    internal void CancelNameChange()
    {
        textField.text = "";
        editTabLabelPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    internal void AddSongToPlaylist(int tabId, Song song, int positionToInsertAt = -1)
    {
        musicLoadingAnimation.SetActive(false);
        GameObject mbObj = Instantiate(Prefabs.musicButtonPrefab, tabs[tabId].musicContentView.transform);
        MusicButton mb = mbObj.GetComponent<MusicButton>();
        mb.Init();
        mb.buttonId = tabs[tabId].MusicButtons.Count;
        tabs[tabId].MusicButtons.Add(mb);
        mb.Song = song;

        //ensure items only populate in search results when songs are still loading IF it matches the search
        if (!string.IsNullOrWhiteSpace(mc.searchField.text))
        {
            mc.SearchTextEntered(mc.searchField.text);
        }
        if (positionToInsertAt != -1)
        {
            for (int i = tabs[tabId].MusicButtons.Count - 2; i > positionToInsertAt; i--)
            {
                mc.RefreshSongOrder(i + 1, i);
            }
            mbObj.transform.SetSiblingIndex(positionToInsertAt + 1);
        }
    }

    internal void DeleteAllTabs()
    {
        foreach (PlaylistTab t in tabs)
        {
            if (t.tabId > 0)
            {
                Destroy(t.gameObject);
            }
        }
        tabs.RemoveAll(t => t.tabId > 0);
    }

    internal IEnumerator DeleteTab()
    {

        if (NowEditing.tabId > 0)
        {
            if (MusicController.NowPlayingTab.tabId == NowEditing.tabId)
            {
                mc.Stop();
                MusicController.NowPlayingTab = tabs[0];
            }
            ChangeTab(0);
            tabs.Remove(NowEditing);
            int i = 1;
            tabs.ForEach(t =>
            {
                if (t.tabId > 0)
                {
                    t.tabId = i;
                    i++;
                }
            });
            Destroy(NowEditing.gameObject);
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());
        }
    }

    internal void EnableSortButton()
    {
        sortButton.interactable = true;
    }

    internal void DisableSortButton()
    {
        sortButton.interactable = false;
    }
}

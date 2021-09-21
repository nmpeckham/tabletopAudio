using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class PlaylistRightClickController : MonoBehaviour
{
    private MainAppController mac;
    private MusicController mc;
    private PlaylistTabs pt;
    internal int selectedSongId = -1;

    internal RightClickRootMenu activeRightClickMenu;

    private float minX = -10f;
    private float maxX = 90f;
    private float minY = -10f;
    private float maxY = 60f;

    // Start is called before the first frame update
    void Start()
    {
        mac = GetComponent<MainAppController>();
        mc = GetComponent<MusicController>();
        pt = GetComponent<PlaylistTabs>();
    }

    internal void ShowRightClickMenu(int id)
    {
        mac.currentMenuState = MainAppController.MenuState.musicRightClickMenu;
        selectedSongId = id;
        if (activeRightClickMenu != null)
        {
            Destroy(activeRightClickMenu);
            activeRightClickMenu = null;
        }
        activeRightClickMenu = Instantiate(Prefabs.rightClickMenuPrefab, Input.mousePosition, Quaternion.identity, MainAppController.tooltipParent).GetComponent<RightClickRootMenu>();
        activeRightClickMenu.AddMenuItem(0, "Remove", activeRightClickMenu.buttonParent);
        activeRightClickMenu.AddMenuItem(1, "Play Next", activeRightClickMenu.buttonParent);
        activeRightClickMenu.AddMenuItem(5, "Clone", activeRightClickMenu.buttonParent);
        activeRightClickMenu.AddMenuItem(2, "Add To...", activeRightClickMenu.buttonParent);
        activeRightClickMenu.SetBounds(minX, minY, maxX, maxY);

        ShowAddToMenu();

        StartCoroutine(activeRightClickMenu.CheckMousePos());

        if (PlaylistTabs.tabs.Count == 1)
        {
            activeRightClickMenu.buttonParent.transform.GetChild(3).gameObject.SetActive(false);
        }
        else
        {
            activeRightClickMenu.buttonParent.transform.GetChild(3).gameObject.SetActive(true);
        }
        activeRightClickMenu.maxY = (((activeRightClickMenu.buttonParent.transform.GetComponentsInChildren<Transform>().Count() - 1) / 2) * 23) + 15;    //add 23 pixels of mouse room for each item in list plus an extra 15
    }

    internal void CloseDeleteMusicItemTooltip()
    {
        StopAllCoroutines();
        CloseDeleteAddToMenu();
        Destroy(activeRightClickMenu.gameObject);
        activeRightClickMenu = null;
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    internal void CloseDeleteAddToMenu()
    {
        activeRightClickMenu.HideSideMenu();
    }

    internal void AddToPlayNext(int id)
    {
        mc.AddToPlayNext(new PlayNextItem(selectedSongId, PlaylistTabs.selectedTab.tabId));
        CloseDeleteMusicItemTooltip();
    }

    internal void ShowAddToMenu()
    {
        //Prevent duplicates
        if(activeRightClickMenu.sideMenuButtons.Count == 0)
        {
            activeRightClickMenu.maxX += 120f;
            activeRightClickMenu.maxY = Mathf.Max(activeRightClickMenu.maxY, (PlaylistTabs.tabs.Count * 23f) - 1 + 10);

            foreach (PlaylistTab tab in PlaylistTabs.tabs)
            {
                if (tab.tabId != 0)
                {
                    activeRightClickMenu.AddToSideMenu(tab.tabId, tab.LabelText);
                }
            }
        }
    }

    internal void DuplicateItem()
    {
        print(PlaylistTabs.selectedTab.tabId);
        pt.AddSongToPlaylist(PlaylistTabs.selectedTab.tabId, PlaylistTabs.selectedTab.GetSongAtIndex(selectedSongId), selectedSongId);

    }

    internal void DeleteItem()
    {
        mc.DeleteItem(selectedSongId);
        CloseDeleteMusicItemTooltip();
    }

    internal void AddSongToPlaylist(int tabId)
    {
        GetComponent<PlaylistTabs>().AddSongToPlaylist(tabId, PlaylistTabs.selectedTab.GetSongAtIndex(selectedSongId));
        CloseDeleteMusicItemTooltip();
    }
}

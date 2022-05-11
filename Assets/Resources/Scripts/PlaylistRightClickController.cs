using System.Linq;
using UnityEngine;

public class PlaylistRightClickController : MonoBehaviour
{
    private MainAppController mac;
    private MusicController mc;
    private PlaylistTabs pt;
    internal MusicButton selectedButton = null;

    internal RightClickRootMenu activeRightClickMenu;

    private readonly float minX = -10f;
    private readonly float maxX = 90f;
    private readonly float minY = -10f;
    private readonly float maxY = 60f;

    // Start is called before the first frame update
    private void Start()
    {
        mac = GetComponent<MainAppController>();
        mc = GetComponent<MusicController>();
        pt = GetComponent<PlaylistTabs>();
    }

    internal void ShowRightClickMenu(MusicButton button)
    {
        mac.currentMenuState = MainAppController.MenuState.musicRightClickMenu;
        selectedButton = button;
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

    internal void AddToPlayNext()
    {
        mc.AddToPlayNext(new PlayNextItem(selectedButton, PlaylistTabs.selectedTab.tabId));
        CloseDeleteMusicItemTooltip();
    }

    internal void ShowAddToMenu()
    {
        //Prevent duplicates
        if (activeRightClickMenu.sideMenuButtons.Count == 0)
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
        pt.AddSongToPlaylist(PlaylistTabs.selectedTab.tabId, selectedButton.Song, selectedButton.buttonId);
    }

    internal void DeleteItem()
    {
        mc.DeleteItem(selectedButton);
        CloseDeleteMusicItemTooltip();
    }

    internal void AddSongToPlaylist(int tabId)
    {
        GetComponent<PlaylistTabs>().AddSongToPlaylist(tabId, selectedButton.Song);
        CloseDeleteMusicItemTooltip();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MusicRightClickController : MonoBehaviour
{
    private MainAppController mac;
    private MusicController mc;
    private PlaylistTabs pt;
    internal int selectedSongId = -1;

    public GameObject tooltipParent;

    private GameObject activeRightClickMenu;
    private GameObject addToMenu;

    private float minX = -10f;
    private float maxX = 80f;
    private float minY = -10f;
    private float maxY = 90f;

    // Start is called before the first frame update
    void Start()
    {
        mac = GetComponent<MainAppController>();
        mc = GetComponent<MusicController>();
        pt = GetComponent<PlaylistTabs>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    internal void ShowRightClickMenu(int id)
    {
        mac.currentMenuState = MainAppController.MenuState.musicRightClickMenu;
        selectedSongId = id;
        if (activeRightClickMenu) Destroy(activeRightClickMenu);
        activeRightClickMenu = Instantiate(Prefabs.rightClickMenuPrefab, Input.mousePosition, Quaternion.identity, tooltipParent.transform);
        StartCoroutine(CheckMousePos(Input.mousePosition));
    }

    internal void CloseDeleteMusicItemTooltip()
    {
        StopAllCoroutines();
        Destroy(activeRightClickMenu);
        Destroy(addToMenu);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
        maxX = 80f;
        maxY = 80f;
        
    }

    internal void AddToPlayNext(int id)
    {
        mc.AddToPlayNext(new PlayNextItem(selectedSongId, PlaylistTabs.selectedTab.tabId));
        CloseDeleteMusicItemTooltip();
    }

    internal void ShowAddToMenu()
    {
        maxX += 120f;
        maxY = Mathf.Max(maxY, (PlaylistTabs.tabs.Count * 23f) + 10);

        addToMenu = Instantiate(Prefabs.addToMenuPrefab, activeRightClickMenu.transform.position, Quaternion.identity, tooltipParent.transform);
        addToMenu.transform.position = new Vector3(addToMenu.transform.position.x + 85, addToMenu.transform.position.y);
        foreach(PlaylistTab tab in PlaylistTabs.tabs)
        {
            if(tab.tabId != 0)
            {
                GameObject tabOption = Instantiate(Prefabs.addToMenuItemPrefab, addToMenu.transform);
                tabOption.GetComponentInChildren<TMP_Text>().text = tab.LabelText;
                tabOption.GetComponentInChildren<AddMusicItemButton>().playlistTabId = tab.tabId;
            }
        }
    }

    IEnumerator CheckMousePos(Vector3 mousePos)
    {
        while (activeRightClickMenu)
        {
            float yDelta = Input.mousePosition.y - mousePos.y;
            float xDelta = Input.mousePosition.x - mousePos.x;
            if (yDelta < minY || yDelta > maxY || xDelta < minX || xDelta > maxX)
            {
                CloseDeleteMusicItemTooltip();
                break;
            }
            //else if (mac.currentMenuState == MainAppController.MenuState.mainAppView && Input.GetKey(KeyCode.Escape))
            //{
            //    CloseDeleteMusicItemTooltip();
            //    break;
            //}

            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    internal void DeleteItem()
    {
        mc.DeleteItem(selectedSongId);
        Destroy(activeRightClickMenu);
        activeRightClickMenu = null;
    }

    internal void AddSongToPlaylist(int tabId)
    {
        //print("tabId: " + tabId);
        //print("SelectedSongID: " + selectedSongId);
        //print("list size: " + mc.musicButtons.Count);
        GetComponent<PlaylistTabs>().AddSongToPlaylist(tabId, PlaylistTabs.selectedTab.GetSongAtIndex(selectedSongId));
        Destroy(activeRightClickMenu);
        Destroy(addToMenu);
        addToMenu = null;
        activeRightClickMenu = null;
    }
}

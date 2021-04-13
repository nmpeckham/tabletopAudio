using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class PlaylistRightClickController : MonoBehaviour
{
    private MainAppController mac;
    private MusicController mc;
    internal int selectedSongId = -1;

    public GameObject tooltipParent;

    internal RightClickRootMenu activeRightClickMenu;
    private static List<GameObject> addToMenu;

    private float minX = -10f;
    private float maxX = 90f;
    private float minY = -10f;
    private float maxY = 60f;

    // Start is called before the first frame update
    void Start()
    {
        addToMenu = new List<GameObject>();
        mac = GetComponent<MainAppController>();
        mc = GetComponent<MusicController>();
    }

    internal void ShowRightClickMenu(int id)
    {
        mac.currentMenuState = MainAppController.MenuState.musicRightClickMenu;
        selectedSongId = id;
        if (activeRightClickMenu) Destroy(activeRightClickMenu);
        activeRightClickMenu = Instantiate(Prefabs.rightClickMenuPrefab, Input.mousePosition, Quaternion.identity, tooltipParent.transform).GetComponent<RightClickRootMenu>();
        StartCoroutine(CheckMousePos(Input.mousePosition));

        if (PlaylistTabs.tabs.Count == 1)
        {
            activeRightClickMenu.buttonParent.transform.GetChild(2).gameObject.SetActive(false);
        }
        else
        {
            activeRightClickMenu.buttonParent.transform.GetChild(2).gameObject.SetActive(true);
        }
        maxY = (((activeRightClickMenu.buttonParent.transform.GetComponentsInChildren<Transform>().Count() - 1) / 2) * 23) + 15;
    }

    internal void CloseDeleteMusicItemTooltip()
    {
        StopAllCoroutines();
        Destroy(activeRightClickMenu.gameObject);
        activeRightClickMenu = null;
        CloseDeleteAddToMenu();
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
        maxX = 80f;
        maxY = 80f;
        
    }

    internal void CloseDeleteAddToMenu()
    {
        addToMenu.ForEach(go => Destroy(go));
        addToMenu.Clear();
    }

    internal void AddToPlayNext(int id)
    {
        mc.AddToPlayNext(new PlayNextItem(selectedSongId, PlaylistTabs.selectedTab.tabId));
        CloseDeleteMusicItemTooltip();
    }

    internal void ShowAddToMenu()
    {
        //Prevent duplicates
        if(addToMenu.Count == 0)
        {
            maxX += 120f;
            maxY = Mathf.Max(maxY, (PlaylistTabs.tabs.Count * 23f) - 1 + 10);

            //GameObject newMenu = Instantiate(Prefabs.addToMenuPrefab, activeRightClickMenu.GetComponent<RightClickRootMenu>().addToParent);
            //addToMenu.Add(newMenu);

            //newMenu.transform.position = new Vector3(newMenu.transform.position.x + activeRightClickMenu.GetComponent<RectTransform>().rect.width, newMenu.transform.position.y);
            foreach (PlaylistTab tab in PlaylistTabs.tabs)
            {
                if (tab.tabId != 0)
                {
                    GameObject tabOption = Instantiate(Prefabs.addToMenuItemPrefab, activeRightClickMenu.GetComponent<RightClickRootMenu>().addToParent);
                    addToMenu.Add(tabOption);
                    tabOption.GetComponentInChildren<TMP_Text>().text = tab.LabelText;
                    tabOption.GetComponentInChildren<AddMusicItemButton>().playlistTabId = tab.tabId;
                }
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

            yield return null;
        }
        yield return null;
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

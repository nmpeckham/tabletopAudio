using UnityEngine;

public static class Prefabs
{
    // Start is called before the first frame update
    static internal GameObject rightClickMenuPrefab;
    static internal GameObject rightClickItemPrefab;
    static internal GameObject tooltipPrefab;
    static internal GameObject musicButtonPrefab;
    static internal GameObject pageButtonPrefab;
    static internal GameObject pageParentPrefab;
    static internal GameObject sfxButtonPrefab;
    static internal GameObject errorPrefab;
    static internal GameObject quickRefAttributePrefab;
    static internal GameObject quickRefPrefab;
    static internal GameObject loadGameItemPrefab;
    static internal GameObject fileSelectItemPrefab;
    static internal GameObject musicContentViewPrefab;
    static internal GameObject addToMenuPrefab;
    static internal GameObject addToMenuItemPrefab;
    static internal GameObject playlistTabPrefab;
    static internal void LoadAll()
    {
        rightClickMenuPrefab = Resources.Load<GameObject>("Prefabs/RightClickMenu");
        rightClickItemPrefab = Resources.Load<GameObject>("Prefabs/RightClickItem");
        tooltipPrefab = Resources.Load<GameObject>("Prefabs/Tooltip");
        musicButtonPrefab = Resources.Load<GameObject>("Prefabs/PlaylistItem");
        pageButtonPrefab = Resources.Load<GameObject>("Prefabs/PageButton");
        pageParentPrefab = Resources.Load<GameObject>("Prefabs/PageParent");
        sfxButtonPrefab = Resources.Load<GameObject>("Prefabs/SFXButton");
        errorPrefab = Resources.Load<GameObject>("Prefabs/ErrorPrefab");
        quickRefAttributePrefab = Resources.Load<GameObject>("Prefabs/QuickRefDetailAttribute");
        quickRefPrefab = Resources.Load<GameObject>("Prefabs/QuickRefPrefab");
        loadGameItemPrefab = Resources.Load<GameObject>("Prefabs/LoadGameItem");
        fileSelectItemPrefab = Resources.Load<GameObject>("Prefabs/FileSelectItem");
        musicContentViewPrefab = Resources.Load<GameObject>("Prefabs/MusicContentView");
        addToMenuPrefab = Resources.Load<GameObject>("Prefabs/AddToMenu");
        addToMenuItemPrefab = Resources.Load<GameObject>("Prefabs/AddToItemPrefab");
        playlistTabPrefab = Resources.Load<GameObject>("Prefabs/PlaylistTabPrefab");
    }
}

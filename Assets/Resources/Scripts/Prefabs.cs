using UnityEngine;

public static class Prefabs
{
    // Start is called before the first frame update
    internal static GameObject rightClickMenuPrefab;
    internal static GameObject rightClickItemPrefab;
    internal static GameObject tooltipPrefab;
    internal static GameObject musicButtonPrefab;
    internal static GameObject pageButtonPrefab;
    internal static GameObject pageParentPrefab;
    internal static GameObject sfxButtonPrefab;
    internal static GameObject errorPrefab;
    internal static GameObject quickRefAttributePrefab;
    internal static GameObject quickRefPrefab;
    internal static GameObject loadGameItemPrefab;
    internal static GameObject fileSelectItemPrefab;
    internal static GameObject musicContentViewPrefab;
    internal static GameObject addToMenuPrefab;
    internal static GameObject addToMenuItemPrefab;
    internal static GameObject playlistTabPrefab;
    internal static GameObject searchFolderListItemPrefab;
    internal static void LoadAll()
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
        searchFolderListItemPrefab = Resources.Load<GameObject>("Prefabs/SearchFolderListItem");
    }
}

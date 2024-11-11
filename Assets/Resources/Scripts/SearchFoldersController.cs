using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchFoldersController : MonoBehaviour
{
    public GameObject searchFolderPanel;
    public GameObject searchFoldersScrollView;
    public TMP_Text currentPathLabel;
    public Button closeButton;

    public Button applyButton;
    private MainAppController mac;

    internal List<string> searchFolders = new();
    internal List<string> removedFolders = new();
    public TMP_Text searchFoldersText;
    private const string searchFolderLabel = " Folders Selected";

    private string currentPath = "";

    public Button ClearSearchFoldersButton;
    private MusicController mc;

    // Start is called before the first frame update
    internal void Init()
    {
        mac = GetComponent<MainAppController>();
        mc = GetComponent<MusicController>(); 
        closeButton.onClick.AddListener(CloseButtonClicked);
        //ItemToggled(true, MainAppController.workingDirectories["musicDirectory"]);
        ShowDriveListing();
        ClearSearchFoldersButton.onClick.AddListener(ClearSearchFolders);
        applyButton.onClick.AddListener(Apply);
    }

    void Apply()
    {
        CloseButtonClicked();
    }

    internal void ClearSearchFolders()
    {
        searchFolders.Clear();
        searchFoldersText.text = "0" + searchFolderLabel;
        removedFolders.Clear();
        foreach(SearchFolderListItem sfli in searchFolderPanel.GetComponentsInChildren<SearchFolderListItem>())
        {
            sfli.ChangeToggleNoNotify(false);
        }
        RestartMusicControllerSearchCoroutine();
    }

    private void RestartMusicControllerSearchCoroutine()
    {
        mc.StopActiveSongCheck();
        mc.StartActiveSongCheck();
    }

    private void ShowDriveListing()
    {
        currentPath = "";
        currentPathLabel.text = "Drive Listing";
        foreach (var s in DriveInfo.GetDrives())
        {
            GameObject newFolder = Instantiate(Prefabs.searchFolderListItemPrefab, searchFoldersScrollView.transform);
            SearchFolderListItem sfli = newFolder.GetComponent<SearchFolderListItem>();
            sfli.Init();
            sfli.Label = s.Name;
            sfli.Path = s.RootDirectory.FullName;
            sfli.searchToggle.gameObject.SetActive(false);
        }
    }

    private void DeleteAllListItems()
    {
        foreach (SearchFolderListItem sfli in searchFoldersScrollView.GetComponentsInChildren<SearchFolderListItem>())
        {
            Destroy(sfli.gameObject);
        }
    }

    internal void ItemClicked(SearchFolderListItem item = null)
    {
        DeleteAllListItems();
        if (item == null || item.Path == "..")
        {
            string[] splitPath = currentPath.Split(Path.DirectorySeparatorChar);
            string newPath = "";
            //At main listing screen
            if (string.IsNullOrWhiteSpace(splitPath[1]))
            {
                DeleteAllListItems();
                ShowDriveListing();
                return;
            }
            //In main drive root, append appropriate separator
            if (splitPath.Length == 2)
            {
                newPath = splitPath[0] + Path.DirectorySeparatorChar;
            }
            else
            {
                for (int i = 0; i < splitPath.Length - 1; i++)
                {
                    if (i > 0)
                    {
                        newPath += Path.DirectorySeparatorChar;
                    }
                    newPath = Path.Combine(newPath, splitPath[i]);
                }
            }

            currentPath = newPath;
        }
        else
        {
            currentPath = item.Path;
        }

        GameObject upFolderButton = AddSearchFolderListItem("..", false);
        SearchFolderListItem sfli = upFolderButton.GetComponent<SearchFolderListItem>();
        sfli.folderImage.sprite = ResourceManager.upFolderImage;
        sfli.searchToggle.gameObject.SetActive(false);
        upFolderButton.GetComponent<Image>().pixelsPerUnitMultiplier = 5;
        Vector2 tempRect = upFolderButton.GetComponent<RectTransform>().sizeDelta;
        tempRect.y = 30;
        upFolderButton.GetComponent<RectTransform>().sizeDelta = tempRect;

        try
        {
            foreach (string directory in Directory.EnumerateDirectories(currentPath))
            {
                GameObject newButton = AddSearchFolderListItem(directory, FolderIsSelected(directory));
            }
            currentPathLabel.text = currentPath;
        }
        catch (System.UnauthorizedAccessException e)
        {
            mac.ShowErrorMessage(e.Message, 0);
            ItemClicked();  // go up a directory
        }
        catch (IOException e)
        {
            mac.ShowErrorMessage(e.Message, 0);
            ItemClicked();
        }

    }

    private bool FolderIsSelected(string folder)
    {
        if (searchFolders.Contains(folder))
        {
            return true;
        }

        if (removedFolders.Contains(folder))
        {
            return false;
        }

        return false;
    }

    private GameObject AddSearchFolderListItem(string path, bool isSelected)
    {
        string[] splitDirectories = path.Split(Path.DirectorySeparatorChar);
        string label = splitDirectories[^1];
        GameObject newFolder = Instantiate(Prefabs.searchFolderListItemPrefab, searchFoldersScrollView.transform);
        SearchFolderListItem sfli = newFolder.GetComponent<SearchFolderListItem>();
        sfli.Init();
        sfli.Label = label;
        sfli.Path = path;
        sfli.ChangeToggleNoNotify(isSelected);
        return newFolder;
    }

    //Adding folders recurses and adds all subfolders. Removing a folder does not recurse.
    internal void ItemToggled(bool state, string folder)
    {
        if (state)
        {
            foreach (string s in System.IO.Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
            {
                if(!searchFolders.Contains(s)) AddSearchFolderItem(s);
            };
            if(!searchFolders.Contains(folder)) AddSearchFolderItem(folder);
        }
        else
        {
            //TODO: Check if any subfolders in deselected folder are still selected, set toggle state to "some"
            //foreach(string s in System.IO.Directory.EnumerateDirectories(folder, "*"))
            //{
            //    if(searchFolders.Contains(s))
            //    {
                    
            //    }
            //}
            RemoveSearchFolderItem(folder);
        }
        RestartMusicControllerSearchCoroutine();
    }

    internal void CloseButtonClicked()
    {
        searchFolderPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    internal void ShowSearchFolderMenu()
    {
        searchFolderPanel.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.searchFoldersMenu;
    }

    internal void AddSearchFolderItem(string s)
    {
        searchFolders.Add(s);
        searchFoldersText.text = searchFolders.Count.ToString() + searchFolderLabel;
    }

    internal void RemoveSearchFolderItem(string s)
    {
        searchFolders.RemoveAll(item => item == s);
        searchFoldersText.text = searchFolders.Count.ToString() + searchFolderLabel;
    }
}

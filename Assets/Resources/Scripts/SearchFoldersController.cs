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
    private MainAppController mac;

    public List<string> searchFolders = new();
    public List<string> removedFolders = new();

    private string currentPath = "";

    // Start is called before the first frame update
    internal void Init()
    {
        mac = GetComponent<MainAppController>();
        closeButton.onClick.AddListener(CloseButtonClicked);
        //ItemToggled(true, MainAppController.workingDirectories["musicDirectory"]);
        ShowDriveListing();
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
        if (item.Path == "..")
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
                if(!searchFolders.Contains(s)) searchFolders.Add(s);
            };
            if(!searchFolders.Contains(folder)) searchFolders.Add(folder);
        }
        else
        {
            searchFolders.RemoveAll(item => item == folder);
        }
    }

    internal void CloseButtonClicked()
    {
        searchFolderPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.optionsMenu;
    }

    internal void ShowSearchFolderMenu()
    {
        searchFolderPanel.SetActive(true);
        mac.currentMenuState = MainAppController.MenuState.searchFoldersMenu;
    }
}

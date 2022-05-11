using System;
using UnityEngine;
using UnityEngine.UI;

public class ShortcutViewController : MonoBehaviour
{
    public Button exitButton;
    public GameObject shortcutsPanel;

    public Button showSearchFoldersButton;

    // Start is called before the first frame update
    private void Start()
    {
        //PlayerPrefs.DeleteKey("shortcutsShown");
        showSearchFoldersButton.gameObject.SetActive(false);
        bool shortcutsShown = false;
        try
        {
            shortcutsShown = Convert.ToBoolean(PlayerPrefs.GetString("shortcutsShown"));
        }
        catch (FormatException) { }
        if (!shortcutsShown)
        {
            showSearchFoldersButton.gameObject.SetActive(true);
            GetComponent<MainAppController>().ControlButtonClicked("OPTIONS");
            ShowShortcuts();
        }
        exitButton.onClick.AddListener(CloseShortcuts);
        showSearchFoldersButton.onClick.AddListener(ShowSearchFolders);
    }

    private void ShowSearchFolders()
    {
        GetComponent<OptionsMenuController>().OptionMenuButtonClicked("show search folders");
        //Hide button after first run
        showSearchFoldersButton.gameObject.SetActive(false);
    }

    internal void ShowShortcuts()
    {
        Camera.main.GetComponent<MainAppController>().currentMenuState = MainAppController.MenuState.shortcutView;
        shortcutsPanel.SetActive(true);
        PlayerPrefs.SetString("shortcutsShown", true.ToString());
    }

    internal void CloseShortcuts()
    {
        Camera.main.GetComponent<MainAppController>().currentMenuState = MainAppController.MenuState.optionsMenu;
        shortcutsPanel.SetActive(false);
    }
}

using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Controls the file selection view for SFX buttons
public class FileSelectViewController : MonoBehaviour
{
    private static MainAppController mac;
    public Button closeButton;
    private static ButtonEditorController bec;
    public GameObject fileSelectionView;
    public GameObject fileLoadListScrollView;

    private void Start()
    {
        bec = Camera.main.GetComponent<ButtonEditorController>();
        mac = Camera.main.GetComponent<MainAppController>();
        closeButton.onClick.AddListener(CloseFileSelection);
    }

    internal void CloseFileSelection()
    {
        foreach (FileSelectItem fsi in fileLoadListScrollView.GetComponentsInChildren<FileSelectItem>())
        {
            Destroy(fsi.gameObject);
        }
        fileSelectionView.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.editingSFXButton;
    }

    internal void LoadFileSelectionView()
    {
        fileSelectionView.SetActive(true);


        foreach (string file in System.IO.Directory.GetFiles(MainAppController.workingDirectories["sfxDirectory"], "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".ogg")
            {

                GameObject item = Instantiate(Prefabs.fileSelectItemPrefab, fileLoadListScrollView.transform);
                string text = Path.GetFileName(file);
                item.GetComponentInChildren<TMP_Text>().SetText(text);
                item.GetComponent<FileSelectItem>().id = file;
            }
        }
    }

    internal void ItemSelected(string id)
    {
        CloseFileSelection();
        bec.UpdateFile(id);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

//Controls the file selection view for SFX buttons
public class FileSelectViewController : MonoBehaviour
{
    MainAppController mac;
    GameObject itemListPrefab;
    public Button closeButton;
    int activeButtonID = -1;
    private ButtonEditorController bec;
    public GameObject fileSelectionView;
    private List<string> itemsToCreate;
    public GameObject fileLoadListScrollView;
    public GameObject fileListItemPrefab;

    private void Start()
    {
        bec = Camera.main.GetComponent<ButtonEditorController>();
        mac = Camera.main.GetComponent<MainAppController>();
        closeButton.onClick.AddListener(CloseFileSelection);

        itemsToCreate = new List<string>();
    }

    private void CloseFileSelection()
    {
        fileSelectionView.gameObject.SetActive(false);
    }

    internal void LoadFileSelectionView(int buttonID)
    {
        activeButtonID = buttonID;
        fileSelectionView.gameObject.SetActive(true);

        

        foreach (string file in System.IO.Directory.GetFiles(mac.sfxDirectory))
        {
            //Debug.Log(LoadedFilesData.sfxClips.Count);
            //Debug.Log(LoadedFilesData.sfxClips.ContainsKey(file));
            if (!LoadedFilesData.sfxClips.ContainsKey(file))
            {

                AudioClip newClip = null;
                LoadedFilesData.sfxClips.Add(file, newClip);
                itemsToCreate.Add(file);
            }
        }
        foreach (string file in itemsToCreate)
        {
            //Debug.Log(file);
            GameObject item = Instantiate(fileListItemPrefab, fileLoadListScrollView.transform);
            string text = file.Replace(mac.sfxDirectory + mac.sep, "");
            item.GetComponentInChildren<TMP_Text>().SetText(text);
            item.GetComponent<FileSelectItem>().id = file;
        }
        itemsToCreate.Clear();
    }

    internal void ItemSelected(string id)
    {
        fileSelectionView.gameObject.SetActive(false);
        string clipName = id;
        AudioClip clip = Resources.Load<AudioClip>(clipName);
        bec.UpdateFile(clip, id);
    }
}

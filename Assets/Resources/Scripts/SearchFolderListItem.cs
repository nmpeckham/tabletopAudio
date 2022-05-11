using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchFolderListItem : MonoBehaviour
{
    private static SearchFoldersController sfc;
    public Image folderImage;
    internal Toggle searchToggle;
    private string path;
    internal string Path
    {
        get => path;
        set => path = value;
    }
    private string label;
    internal string Label
    {
        get => label;
        set
        {
            label = value;
            GetComponentInChildren<TMP_Text>().text = label;
        }
    }
    // Start is called before the first frame update
    internal void Init()
    {
        if (sfc == null)
        {
            sfc = Camera.main.GetComponent<SearchFoldersController>();
        }

        GetComponent<Button>().onClick.AddListener(Clicked);
        searchToggle = GetComponentInChildren<Toggle>();
        searchToggle.onValueChanged.AddListener(ToggleChanged);
    }

    private void ToggleChanged(bool state)
    {
        sfc.ItemToggled(state, path);
    }

    internal void ChangeToggleNoNotify(bool state)
    {
        searchToggle.SetIsOnWithoutNotify(state);
    }

    private void Clicked()
    {
        sfc.ItemClicked(this);
    }


}

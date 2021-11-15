using UnityEngine;
using UnityEngine.UI;

//Class for selecting a file to load in sound effect button editing menu
public class FileSelectItem : MonoBehaviour
{
    public string id;
    private FileSelectViewController vc;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(ItemSelected);
        vc = Camera.main.GetComponent<FileSelectViewController>();
    }

    private void ItemSelected()
    {
        vc.ItemSelected(id);
    }
}

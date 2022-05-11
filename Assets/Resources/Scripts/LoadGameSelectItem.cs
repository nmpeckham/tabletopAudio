using UnityEngine;
using UnityEngine.UI;

//Class for items in the load game selection view
public class LoadGameSelectItem : MonoBehaviour
{
    public string fileLocation;
    private static OptionsMenuController omc;

    private void Start()
    {
        omc = Camera.main.GetComponent<OptionsMenuController>();
        GetComponent<Button>().onClick.AddListener(Clicked);
    }

    private void Clicked()
    {
        omc.LoadItemSelected(fileLocation);
    }
}
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuButton : MonoBehaviour
{
    // Start is called before the first frame update
    public string id = "";
    private Button thisButton;
    private OptionsMenuController omc;

    private void Start()
    {
        omc = Camera.main.GetComponent<OptionsMenuController>();
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(ButtonClicked);
    }

    private void ButtonClicked()
    {
        omc.OptionMenuButtonClicked(id);
    }
}

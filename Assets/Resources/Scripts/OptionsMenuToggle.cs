using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuToggle : MonoBehaviour
{
    // Start is called before the first frame update
    public string id = "";
    private Toggle thisToggle;
    private OptionsMenuController omc;
    void Start()
    {
        omc = Camera.main.GetComponent<OptionsMenuController>();
        thisToggle = GetComponent<Toggle>();
        thisToggle.onValueChanged.AddListener(ToggleClicked);
    }

    private void ToggleClicked(bool active)
    {
        omc.OptionMenuButtonClicked(id, active);
    }
}

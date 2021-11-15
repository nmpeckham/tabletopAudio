using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Class for editing page button labels using right-click
public class EditPageLabel : MonoBehaviour
{
    public GameObject editLabelPanel;
    public TMP_InputField input;
    public Button cancel;
    public Button confirm;
    private TMP_Text buttonLabel;
    private MainAppController mac;

    internal TMP_Text ButtonLabel
    {
        get { return buttonLabel; }
        set { buttonLabel = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        mac = Camera.main.GetComponent<MainAppController>();
        cancel.onClick.AddListener(Cancel);
        confirm.onClick.AddListener(Confirm);
    }

    internal void Cancel()
    {
        input.text = "";
        editLabelPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    internal void Confirm()
    {
        buttonLabel.text = input.text;
        editLabelPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
    }

    public void StartEditing()
    {
        mac.currentMenuState = MainAppController.MenuState.editingPageLabel;
        input.text = buttonLabel.text;
        print("hello");
        editLabelPanel.SetActive(true);
        input.ActivateInputField();
    }
}

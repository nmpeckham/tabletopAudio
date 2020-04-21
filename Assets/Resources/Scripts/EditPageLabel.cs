using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        mac.currentMenuState = MainAppController.MenuState.none;
    }

    internal void Confirm()
    {
        buttonLabel.text = input.text;
        editLabelPanel.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.none;
    }

    public void StartEditing()
    {
        mac.currentMenuState = MainAppController.MenuState.editingPageLabel;
        input.text = buttonLabel.text;
        editLabelPanel.SetActive(true);
    }
}

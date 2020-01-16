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
    internal TMP_Text buttonLabel;
    private MainAppController mac;

    // Start is called before the first frame update
    void Start()
    {
        mac = Camera.main.GetComponent<MainAppController>();
        cancel.onClick.AddListener(Cancel);
        confirm.onClick.AddListener(Confirm);
    }

    void Cancel()
    {
        input.text = "";
        editLabelPanel.SetActive(false);

    }

    void Confirm()
    {
        buttonLabel.text = input.text;
        editLabelPanel.SetActive(false);
    }

    public void StartEditing()
    {
        input.text = buttonLabel.text;
        editLabelPanel.SetActive(true);
    }
}

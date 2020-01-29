using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

//Class to take commands from page buttons, including menu and "Stop SFX" buttons
public class PageButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int id;
    private Button thisButton;
    private MainAppController mac;
    private TMP_Text label;
    private bool hasPointer = false;

    public string Label
    {
        get 
        {
            return label.text;
        }
        set
        {
            label.text = value;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        label = GetComponentInChildren<TMP_Text>();
        mac = Camera.main.GetComponent<MainAppController>();
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(Clicked);
    }

    void Clicked()
    {
        if (id != 7 && id != -1)
        {
            mac.ChangeSFXPage(id);
            GetComponent<Image>().color = ResourceManager.red;
        }
        else if (id == 7) mac.ControlButtonClicked("STOP-SFX");
        else if (id == -1) mac.ControlButtonClicked("OPTIONS");
    }

    void Update()
    {
        if(mac.activePage != id) GetComponent<Image>().color = ResourceManager.transWhite;
        if(hasPointer && Input.GetMouseButtonDown(1) && id >= 0 && id < 7)
        {
            //Debug.Log("Clicked " + id);
            mac.EditPageLabel(label);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log(id);
        hasPointer = true;
        //Debug.Log(hasPointer);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hasPointer = false;
    }
}
      

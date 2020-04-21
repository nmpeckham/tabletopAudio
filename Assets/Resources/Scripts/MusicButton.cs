using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Class for music items in the playlist
public class MusicButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Button thisButton;
    public int id = -1;
    private string fileName;
    MusicController mc;
    float doubleClickTime = 0.5f;
    float timeSinceClick = 100f;

    public string FileName
    {
        get { return fileName; }
        set { fileName = value; }
    }


    // Start is called before the first frame update
    void Start()
    {
        mc = Camera.main.GetComponent<MusicController>();
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(ItemSelected);
        timeSinceClick = Time.time;
    }

    void ItemSelected()
    {
        if(Time.time - timeSinceClick < doubleClickTime)
        {
            mc.ItemSelected(id);
        }
        timeSinceClick = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        //timeSinceClick = Time.time;   
        if(Input.GetMouseButtonDown(1) && mc.ButtonWithCursor == id)
        {
            mc.ShowRightClickMenu(id);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mc.ButtonWithCursor = id;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mc.ButtonWithCursor = -1;
    }
}

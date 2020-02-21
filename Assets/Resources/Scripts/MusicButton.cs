using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Class for music items in the playlist
public class MusicButton : MonoBehaviour, IPointerEnterHandler
{
    Button thisButton;
    public int id = -1;
    public string file;
    MusicController mc;
    float doubleClickTime = 2f;
    float timeSinceClick = 100f;

    // Start is called before the first frame update
    void Start()
    {
        mc = Camera.main.GetComponent<MusicController>();
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(ItemSelected);
    }

    void ItemSelected()
    {
        if(timeSinceClick < doubleClickTime)
        {
            mc.ItemSelected(id);
        }
        timeSinceClick = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceClick += 0.1f;     
        if(Input.GetMouseButtonDown(1) && mc.ButtonWithCursor == id)
        {
            mc.ShowRightClickMenu();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mc.ButtonWithCursor = id;
    }
}

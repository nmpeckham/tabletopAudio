using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

//Class for music items in the playlist
public class MusicButton : MonoBehaviour,  IPointerClickHandler
{
    private Song song;
    private TMP_Text label;
    internal Song Song
    {
        get
        {
            return song;
        }
        set
        {
            song = value;
            label.text = song.FileName;
        }
    }

    internal int buttonId;
    MusicRightClickController mrcc;
    MusicController mc;
    float doubleClickTime = 0.8f;
    float timeSinceClick = 100f;

    // Start is called before the first frame update
    internal void Init()
    {
        mrcc = Camera.main.GetComponent<MusicRightClickController>();
        mc = Camera.main.GetComponent<MusicController>();
        timeSinceClick = Time.time;
        label = GetComponentInChildren<TMP_Text>();
    }

    void ItemSelected(int type)
    {
        if(type == 0)
        {
            if (Time.time - timeSinceClick < doubleClickTime)
            {
                mc.PlaylistItemSelected(buttonId);
            }
            timeSinceClick = Time.time;
        }
        else if(type == 1)
        {
            mrcc.ShowRightClickMenu(buttonId);
        }
    }

    void LeftClicked()
    {
        ItemSelected(0);
    }

    void RightClicked()
    {
        ItemSelected(1);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) LeftClicked();
        else if (eventData.button == PointerEventData.InputButton.Right) RightClicked();
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;

//Class for music items in the playlist
public class MusicButton : MonoBehaviour,  IPointerClickHandler, IComparable
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
            label.text = song.SortName;
        }
    }

    public int buttonId;
    static PlaylistRightClickController prcc;
    static MusicController mc;
    static float doubleClickTime = 0.8f;
    static float timeSinceClick = 100f;
    internal MoveMusicButton mmb;

    // Start is called before the first frame update
    internal void Init()
    {
        prcc = Camera.main.GetComponent<PlaylistRightClickController>();
        mc = Camera.main.GetComponent<MusicController>();
        timeSinceClick = Time.time;
        label = GetComponentInChildren<TMP_Text>();
        mmb = GetComponentInChildren<MoveMusicButton>();
        //mmb.Init();
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
            prcc.ShowRightClickMenu(buttonId);
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) LeftClicked();
        else if (eventData.button == PointerEventData.InputButton.Right) RightClicked();
    }

    public int CompareTo(object obj)
    {
        //Fix for songs with same name. Don't change :/
        string comp = ((MusicButton)obj).Song.SortName + ((MusicButton)obj).Song.FileName;
        return String.Compare(Song.SortName + Song.FileName, comp);
    }
}

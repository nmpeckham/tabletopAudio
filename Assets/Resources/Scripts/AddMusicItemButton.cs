using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AddMusicItemButton : MonoBehaviour, IPointerClickHandler
{
    public int playlistTabId;
    static PlaylistRightClickController prcc;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            prcc.AddSongToPlaylist(playlistTabId);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        prcc = Camera.main.GetComponent<PlaylistRightClickController>();
    }
}

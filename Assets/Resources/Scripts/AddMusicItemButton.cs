using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AddMusicItemButton : MonoBehaviour, IPointerClickHandler
{
    public int playlistTabId;
    static MusicRightClickController mrcc;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            mrcc.AddSongToPlaylist(playlistTabId);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        mrcc = Camera.main.GetComponent<MusicRightClickController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

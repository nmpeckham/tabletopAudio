using UnityEngine;
using UnityEngine.EventSystems;

public class RightClickSideMenuButton : MonoBehaviour, IPointerClickHandler
{
    public int playlistTabId;
    static PlaylistRightClickController prcc;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
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

using UnityEngine;
using UnityEngine.EventSystems;

public class RightClickSideMenuButton : MonoBehaviour, IPointerClickHandler
{
    public int playlistTabId;
    private static PlaylistRightClickController prcc;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            prcc.AddSongToPlaylist(playlistTabId);
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        prcc = Camera.main.GetComponent<PlaylistRightClickController>();
    }
}

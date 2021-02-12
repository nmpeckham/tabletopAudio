using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PlaylistTab : MonoBehaviour, IPointerClickHandler
{
    private Button thisButton;
    public int tabId;
    private List<Song> playlist = new List<Song>();
    internal GameObject musicContentView;
    private TMP_Text label;
    private string labelText;
    internal PlaylistTabs pt;
    internal string LabelText
    {
        get
        {
            return labelText;
        }
        set
        {
            label.text = value;
            labelText = value;
        }
    }
    internal List<Song> Playlist
    {
        get
        {
            return playlist;
        }

        set
        {
            playlist = value;
        }
    }
            
            //= new List<Song>();

    // Start is called before the first frame update
    void Start()
    {
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(ButtonClicked);
        label = GetComponentInChildren<TMP_Text>();
        if(tabId > 0) LabelText = tabId.ToString();
        pt = Camera.main.GetComponent<PlaylistTabs>();
    }

    private void ButtonClicked()
    {
        //print("Clicked");
        Camera.main.GetComponent<PlaylistTabs>().TabClicked(tabId);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Right)
        {
            pt.EditTabName(this);
        }
    }
}

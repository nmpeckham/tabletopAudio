using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlaylistTab : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    public int tabId;
    //private List<Song> playlist = new List<Song>();
    internal GameObject musicContentView;
    private List<MusicButton> musicButtons;
    private TMP_Text label;
    private string labelText = "*";
    static internal PlaylistTabs pt;

    private float initialMouseXPos = 0f;
    private bool shouldCheckMousePos = false;
    private Camera mainCam;


    private RectTransform rect;

    private bool ShouldCheckMousePos
    {
        get
        {
            return shouldCheckMousePos;
        }
        set
        {
            shouldCheckMousePos = value;
        }
    }

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
    internal List<MusicButton> MusicButtons
    {
        get
        {
            return musicButtons;
        }
        set
        {
            musicButtons = value;
        }
    }

    // Start is called before the first frame update
    internal void Init()
    {
        label = GetComponentInChildren<TMP_Text>();
        if (tabId > 0) LabelText = tabId.ToString();
        mainCam = Camera.main;
        pt = mainCam.GetComponent<PlaylistTabs>();

        rect = GetComponent<RectTransform>();
        musicContentView = GameObject.FindGameObjectWithTag("musicContentView");
        MusicButtons = new List<MusicButton>();
    }

    private void ButtonClicked()
    {
        mainCam.GetComponent<PlaylistTabs>().TabClicked(tabId);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && tabId > 0)
        {
            var activeRightClickMenu = Instantiate(Prefabs.rightClickMenuPrefab, Input.mousePosition, Quaternion.identity, MainAppController.tooltipParent).GetComponent<RightClickRootMenu>();
            activeRightClickMenu.AddMenuItem(4, "Delete Tab", activeRightClickMenu.buttonParent);
            activeRightClickMenu.AddMenuItem(3, "Edit Label", activeRightClickMenu.buttonParent);
            activeRightClickMenu.SetBounds(-10f, -10f, 90f, 60f);

            StartCoroutine(activeRightClickMenu.CheckMousePos());
            //pt.EditTabName(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            initialMouseXPos = Input.mousePosition.x;
            ShouldCheckMousePos = true;
            StartCoroutine(CheckMousePos());
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            ShouldCheckMousePos = false;
            ButtonClicked();
        }
    }

    private IEnumerator CheckMousePos()
    {
        while (ShouldCheckMousePos && rect != null)
        {
            float mouseXPos = Input.mousePosition.x;
            if (initialMouseXPos - mouseXPos > rect.rect.width)
            {
                MoveTabLeft();
            }
            else if (initialMouseXPos - mouseXPos < -rect.rect.width)
            {
                MoveTabRight();
            }
            yield return null;
        }
        yield break;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //shouldCheckMousePos = false;
        //print("exited");
    }

    private void MoveTabLeft()
    {
        if (transform.GetSiblingIndex() > 1 && tabId > 0)
        {
            transform.SetSiblingIndex(transform.GetSiblingIndex() - 1);
            initialMouseXPos = Input.mousePosition.x - rect.rect.width / 2;
        }
    }

    private void MoveTabRight()
    {
        if (transform.GetSiblingIndex() < PlaylistTabs.tabs.Count && tabId > 0)
        {
            print(transform.GetSiblingIndex());
            transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
            initialMouseXPos = Input.mousePosition.x + rect.rect.width / 2;
        }
    }

    internal Song GetSongAtIndex(int index)
    {
        return MusicButtons[index].Song;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tabId > 0) pt.NowEditing = this;
    }
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Controls the reordering of songs in the playlist
public class MoveMusicButton : MonoBehaviour, IPointerDownHandler, IPointerExitHandler
{
    private Vector3 mousePos;
    private static int buttonWithMouse = -1;
    private GameObject musicButton;
    private RectTransform buttonRectTransform;
    private Transform buttonTransform;
    private MusicController mc;
    // Start is called before the first frame update
    void Start()
    {
        musicButton = GetComponentInParent<MusicButton>().gameObject;
        buttonRectTransform = musicButton.GetComponent<RectTransform>();
        buttonTransform = musicButton.transform;
        mc = Camera.main.GetComponent<MusicController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (buttonWithMouse == buttonTransform.GetSiblingIndex() && Input.GetMouseButton(0))
        {
            if ((Input.mousePosition.y - mousePos.y) > buttonRectTransform.rect.height)
            {
                if ((buttonTransform.GetSiblingIndex() - 1) >= 0)
                {
                    int newIndex = buttonTransform.GetSiblingIndex() - 1;
                    mc.RefreshSongOrder(newIndex + 1, newIndex);
                    buttonTransform.SetSiblingIndex(newIndex);

                    buttonWithMouse--;
                    mousePos = Input.mousePosition;
                    //mc.nowPlayingButtonID -= 1;
                }
            }

            if ((Input.mousePosition.y - mousePos.y) < -buttonRectTransform.rect.height)
            {
                if ((buttonTransform.GetSiblingIndex() + 1) <= buttonTransform.parent.childCount - 1)
                {
                    int newIndex = buttonTransform.GetSiblingIndex() + 1;
                    mc.RefreshSongOrder(newIndex - 1, newIndex);
                    buttonTransform.SetSiblingIndex(newIndex);

                    buttonWithMouse++;
                    mousePos = Input.mousePosition;
                    //mc.nowPlayingButtonID += 1;
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mousePos = Input.mousePosition;
        buttonWithMouse = buttonTransform.GetSiblingIndex();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartCoroutine(CheckMouse());
    }

    IEnumerator CheckMouse()
    {
        while(Input.GetMouseButton(0))
        {
            yield return new WaitForEndOfFrame();
        }
        buttonWithMouse = -1;
        yield return null;
    }
}

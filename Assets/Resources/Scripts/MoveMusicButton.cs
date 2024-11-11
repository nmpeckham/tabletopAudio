using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Controls the reordering of songs in the playlist
public class MoveMusicButton : MonoBehaviour, IPointerDownHandler, IPointerExitHandler
{
    private float mouseYPos;
    private static int buttonWithMouse = -1;
    private GameObject musicButton;
    private RectTransform buttonRectTransform;
    public Transform buttonTransform;
    private static MusicController mc;
    private Image buttonImage;
    private Button moveButton;

    private void Awake()
    {
        moveButton = this.GetComponent<Button>();
        musicButton = GetComponentInParent<MusicButton>().gameObject;
        buttonRectTransform = musicButton.GetComponent<RectTransform>();
        buttonTransform = musicButton.transform;
        mc = Camera.main.GetComponent<MusicController>();
        buttonImage = buttonTransform.GetComponent<Image>();
        moveButton.onClick.AddListener(UpdateSongPosition);
    }

    internal void UpdateSongPosition()
    {
        StartCoroutine(CheckMousePos());

    }

    IEnumerator CheckMousePos()
    {
        Color originalColor = buttonImage.color;
        mouseYPos = Input.mousePosition.y;
        while (Input.GetMouseButton(0))
        {
            
            if (buttonWithMouse == buttonTransform.GetSiblingIndex())
            {
                buttonImage.color = new Color(originalColor.r + 0.12f, originalColor.g + 0.12f, originalColor.b + 0.12f);
                float difference = Input.mousePosition.y - mouseYPos;
                //print(difference);
                if (difference > buttonRectTransform.rect.height)
                {
                    MoveSongUp((int)(difference / buttonRectTransform.rect.height));
                }
                else if (difference < -buttonRectTransform.rect.height)
                {
                    MoveSongDown(-(int)(difference / buttonRectTransform.rect.height));
                }
            }
            yield return null;
        }
        buttonImage.color = originalColor;
    }

    internal void MoveSongDown(int numPlaces = 1)
    {
        for (int i = 0; i < numPlaces; i++)
        {
            if ((buttonTransform.GetSiblingIndex() + 1) <= buttonTransform.parent.childCount - 1)
            {
                int newIndex = buttonTransform.GetSiblingIndex() + 1;
                mc.RefreshSongOrder(newIndex - 1, newIndex);
                buttonTransform.SetSiblingIndex(newIndex);
                buttonWithMouse++;
            }
        }
        mouseYPos = Input.mousePosition.y;
    }

    internal void MoveSongUp(int numPlaces = 1)
    {
        for (int i = 0; i < numPlaces; i++)
        {
            if ((buttonTransform.GetSiblingIndex() - 1) >= 0)
            {
                int newIndex = buttonTransform.GetSiblingIndex() - 1;
                mc.RefreshSongOrder(newIndex + 1, newIndex);
                buttonTransform.SetSiblingIndex(newIndex);
                buttonWithMouse--;
            }
        }
        mouseYPos = Input.mousePosition.y;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mouseYPos = Input.mousePosition.y;
        buttonWithMouse = buttonTransform.GetSiblingIndex();
        StartCoroutine(CheckMousePos());

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartCoroutine(CheckMouse());
    }

    private IEnumerator CheckMouse()
    {
        while (Input.GetMouseButton(0))
        {
            yield return null;
        }
        buttonWithMouse = -1;
        yield return null;
    }
}

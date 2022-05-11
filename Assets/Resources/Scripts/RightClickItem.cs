using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RightClickItem : MonoBehaviour, IPointerEnterHandler
{
    public int id;
    private PlaylistRightClickController prcc;
    private PlaylistTabs pt;
    internal RightClickRootMenu parent;

    // Start is called before the first frame update
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Clicked);
        prcc = Camera.main.GetComponent<PlaylistRightClickController>();
        pt = Camera.main.GetComponent<PlaylistTabs>();
    }

    private void Clicked()
    {
        if (id == 0)
        {
            prcc.DeleteItem();
        }

        if (id == 1)
        {
            prcc.AddToPlayNext();
        }

        if (id == 2)
        {
            prcc.ShowAddToMenu();
        }

        if (id == 3)
        {
            pt.EditTabName();
        }

        if (id == 4)
        {
            StartCoroutine(pt.DeleteTab());
        }

        if (id == 5)
        {
            prcc.DuplicateItem();
        }

        StartCoroutine(Delete());
    }

    private IEnumerator Delete()
    {
        yield return null;
        Destroy(parent.gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (id == 2)
        {
            parent.ShowSideMenu();
        }
        else
        {
            parent.HideSideMenu();
        }
    }
}

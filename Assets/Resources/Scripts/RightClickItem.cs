using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RightClickItem : MonoBehaviour, IPointerEnterHandler
{
    public int id;
    PlaylistRightClickController prcc;
    PlaylistTabs pt;
    internal RightClickRootMenu parent;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Clicked);
        prcc = Camera.main.GetComponent<PlaylistRightClickController>();
        pt = Camera.main.GetComponent<PlaylistTabs>();
    }

    void Clicked()
    {
        if (id == 0) prcc.DeleteItem();
        if (id == 1) prcc.AddToPlayNext(id);
        if (id == 2) prcc.ShowAddToMenu();
        if (id == 3) pt.EditTabName();
        if (id == 4) StartCoroutine(pt.DeleteTab());
        if (id == 5) prcc.DuplicateItem();

        StartCoroutine(Delete());
    }

    IEnumerator Delete()
    {
        yield return null;
        Destroy(parent.gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (id == 2) parent.ShowSideMenu();
        else parent.HideSideMenu();
    }
}

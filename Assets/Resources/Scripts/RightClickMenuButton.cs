using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RightClickMenuButton : MonoBehaviour
{
    public int id;
    private PlaylistRightClickController prcc;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Clicked);
        prcc = Camera.main.GetComponent<PlaylistRightClickController>();
    }

    void Clicked()
    {
        print("id: " + id);
        if(id == 0) prcc.DeleteItem();
        if (id == 1) prcc.AddToPlayNext(id);
    }

}

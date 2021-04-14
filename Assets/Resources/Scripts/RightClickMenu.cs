using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RightClickMenu : MonoBehaviour
{
    public int id;
    PlaylistRightClickController prcc;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Clicked);
        prcc = Camera.main.GetComponent<PlaylistRightClickController>();
    }

    void Clicked()
    {
        if(id == 0) prcc.DeleteItem();
        if (id == 1) prcc.AddToPlayNext(id);
        if (id == 2) prcc.ShowAddToMenu();
    }
}

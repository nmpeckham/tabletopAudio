using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RightClickMenu : MonoBehaviour
{
    public int id;
    MusicRightClickController mrcc;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Clicked);
        mrcc = Camera.main.GetComponent<MusicRightClickController>();
    }

    void Clicked()
    {
        if(id == 0) mrcc.DeleteItem();
        if (id == 1) mrcc.AddToPlayNext(id);
        if (id == 2) mrcc.ShowAddToMenu();
    }
}

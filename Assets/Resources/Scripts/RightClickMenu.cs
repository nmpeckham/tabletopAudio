using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RightClickMenu : MonoBehaviour
{
    Button thisButton;
    public int id;
    MusicController mc;
    // Start is called before the first frame update
    void Start()
    {
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(Clicked);
        mc = Camera.main.GetComponent<MusicController>();
    }

    void Clicked()
    {
        if(id == 0) mc.DeleteItem();
        if (id == 1) mc.RefreshSongOrder();
    }
}

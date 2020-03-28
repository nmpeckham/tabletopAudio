using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RootDirectoryItem : MonoBehaviour
{
    public string location;
    private float lastClickTime;
    private float doubleClickTime = 0.8f;

    private void Start()
    {
        lastClickTime = Time.time;
        GetComponent<Button>().onClick.AddListener(Selected);
    }

    private void Selected()
    {
        //Debug.Log(Time.time - lastClickTime);
        if(Time.time - lastClickTime > doubleClickTime)
        {
            Camera.main.GetComponent<OptionsMenuController>().RootFolderSelected(location);
        }
        else
        {
            Camera.main.GetComponent<OptionsMenuController>().RootFolderOpened(location);
        }
        lastClickTime = Time.time;
    }
}

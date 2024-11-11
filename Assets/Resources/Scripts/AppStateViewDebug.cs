using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AppStateViewDebug : MonoBehaviour
{
    public TMP_Text menuState;
    internal void MenuStateChanged(string text)
    {
        menuState.text = text;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickRefAttributePrefab : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text detailText;

    private string title;
    private string detail;
    public string Title 
    { 
        get 
        { 
            return title; 
        } 
        set 
        {
            title = value;
            titleText.text = title;
        } 
    }

    public string Detail
    {
        get
        {
            return detail;
        }
        set
        {
            detail = value;
            detailText.text = detail;
        }
    }
}

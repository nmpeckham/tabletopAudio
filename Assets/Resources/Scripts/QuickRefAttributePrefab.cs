using TMPro;
using UnityEngine;

public class QuickRefAttributePrefab : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text detailText;

    private string title;
    private string detail;
    public string Title
    {
        get => title;
        set
        {
            title = value;
            titleText.text = title;
        }
    }

    public string Detail
    {
        get => detail;
        set
        {
            detail = value;
            detailText.text = detail;
        }
    }
}

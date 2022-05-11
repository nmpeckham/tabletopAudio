using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject tooltip;

    public string tooltipText;
    private static MainAppController mac;

    void Start()
    {
        mac =  Camera.main.GetComponent<MainAppController>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        tooltip = Instantiate(Prefabs.tooltipPrefab, Input.mousePosition, Quaternion.identity, MainAppController.tooltipParent);
        tooltip.GetComponentInChildren<TMP_Text>().text = tooltipText;
        if (tooltip)
        {
            tooltip.GetComponent<Image>().color = MainAppController.darkModeEnabled ? Color.black : Color.white;
            tooltip.GetComponentInChildren<TMP_Text>().color = MainAppController.darkModeEnabled ? Color.white : Color.black;
        }
        StartCoroutine(UpdateTooltipPosition());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopCoroutine("UpdateTooltipPosition");
        Destroy(tooltip);
        tooltip = null;
    }

    private IEnumerator UpdateTooltipPosition()
    {
        Color oldImageColor = tooltip.GetComponent<Image>().color;
        Color oldTextColor = tooltip.GetComponentInChildren<TMP_Text>().color;

        tooltip.GetComponent<Image>().color = new Color(oldImageColor.r, oldImageColor.g, oldImageColor.b, 0f);
        tooltip.GetComponentInChildren<TMP_Text>().color = new Color(oldTextColor.r, oldTextColor.g, oldTextColor.b, 0f);
        yield return new WaitForSecondsRealtime(0.5f);  //wait half a sec before showing tooltip
        //print(mac.mainCanvas.pixelRect.width);
        //print(mac.mainCanvas.pixelRect.height);



        //print(tooltip.transform.position);
        //print(tooltip.transform.localPosition);
        //print(tooltip.GetComponent<RectTransform>().rect.width);
        //print("");
        while (tooltip)
        {
            if (tooltip)
            {
                Rect rect = tooltip.GetComponent<RectTransform>().rect;
                float width = rect.width;
                //width = rect.xMax;//rect.xMax - rect.xMin + 10;
                //                  //print(width);
                //                  //print(rect.width);
                //float maxXPos = Screen.width;

                if (tooltip)
                {
                    tooltip.transform.position = Input.mousePosition;
                    print(rect.xMax);
                }
                if (tooltip.transform.position.x + width > Screen.width)
                {
                    tooltip.transform.position = new Vector3(Screen.width - rect.width, Input.mousePosition.y);
                }
            }
            tooltip.GetComponentInChildren<Image>().color = oldImageColor;
            tooltip.GetComponentInChildren<TMP_Text>().color = oldTextColor;

            yield return null;
        }
    }
}

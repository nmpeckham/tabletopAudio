using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    static private GameObject tooltip;
    static private Canvas mainCanvas;
    public string tooltipText;
    public bool spawnLeft = false;
    private static MainAppController mac;

    void Start()
    {
        mac =  Camera.main.GetComponent<MainAppController>();
        mainCanvas = GameObject.FindGameObjectWithTag("mainCanvas").GetComponent<Canvas>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(tooltip != null) Destroy(tooltip);
        tooltip = Instantiate(Prefabs.tooltipPrefab, Input.mousePosition, Quaternion.identity, MainAppController.tooltipParent);
        if (spawnLeft) tooltip.GetComponent<RectTransform>().pivot = Vector2.right;
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
        Destroy(tooltip);
        tooltip = null;
    }

    //TODO: Causes huge framerate drops :(
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
        while (tooltip != null)
        {
            Rect rect = tooltip.GetComponent<RectTransform>().rect;
            //float width = rect.width;
            //print(width);
            //print(Screen.width);
            //print(tooltip.transform.position.x);
            ////width = rect.xMax;//rect.xMax - rect.xMin + 10;
            ////                  //print(width);
            ////                  //print(rect.width);
            ////float maxXPos = Screen.width;

            tooltip.transform.position = Input.mousePosition;
            ////print(rect.x);
            //if (tooltip.transform.position.x + width > (Screen.width))
            //{
            //    Vector3 newPos;
            //    //RectTransformUtility.ScreenPointToWorldPointInRectangle(tooltip.GetComponent<RectTransform>(), new Vector2(Screen.width - width, Input.mousePosition.y), Camera.main, out newPos);
            //    print("newPos: " + newPos);
            //    tooltip.transform.position = newPos;
            //}
            tooltip.GetComponentInChildren<Image>().color = oldImageColor;
            tooltip.GetComponentInChildren<TMP_Text>().color = oldTextColor;

            yield return null;
        }
        Destroy(tooltip);
        tooltip = null;
    }
}

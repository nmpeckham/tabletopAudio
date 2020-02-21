using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class FadeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int id;  //0 = fade in, 1 = fade out;
    public GameObject tooltipPrefab;
    GameObject tooltip;
    public GameObject tooltipParent;

    void Start()
    {
        tooltipParent = GameObject.FindGameObjectWithTag("TooltipParent");
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("entered");
        tooltip = Instantiate(tooltipPrefab, tooltipParent.transform);
        tooltip.transform.position = (new Vector3(Input.mousePosition.x + tooltip.GetComponent<RectTransform>().rect.width / 2, Input.mousePosition.y + tooltip.GetComponent<RectTransform>().rect.height / 2, -1));
        tooltip.GetComponentInChildren<TMP_Text>().text = id == 0 ? "Fade In": "Fade Out";
        StartCoroutine(UpdateTooltipPosition());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Destroy(tooltip);
        tooltip = null;
    }

    IEnumerator UpdateTooltipPosition()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        while (tooltip != null)
        {
            tooltip.SetActive(true);
            tooltip.transform.position = (new Vector3(Input.mousePosition.x + tooltip.GetComponent<RectTransform>().rect.width / 2, Input.mousePosition.y + tooltip.GetComponent<RectTransform>().rect.height / 2, -1));
            yield return new WaitForEndOfFrame();
        }
    }
}

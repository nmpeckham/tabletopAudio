﻿using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject tooltipPrefab;
    private GameObject tooltip;
    private GameObject tooltipParent;
    public string tooltipText;

    void Start()
    {
        tooltipParent = GameObject.FindGameObjectWithTag("TooltipParent");
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltip = Instantiate(Prefabs.tooltipPrefab, Input.mousePosition, Quaternion.identity, tooltipParent.transform);
        tooltip.GetComponentInChildren<TMP_Text>().text = tooltipText;
        StartCoroutine(UpdateTooltipPosition());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopCoroutine("UpdateTooltipPosition");
        Destroy(tooltip);
        tooltip = null;
    }

    IEnumerator UpdateTooltipPosition()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        if (tooltip) tooltip.GetComponent<Image>().color = Color.white;
        if (tooltip) tooltip.GetComponentInChildren<TMP_Text>().color = Color.black;

        while (tooltip)
        {
            Rect rect = Rect.zero;
            if(tooltip) rect = tooltip.GetComponent<RectTransform>().rect;
            float width = rect.xMax - rect.xMin + 12;
            float maxXPos = Screen.width - width;

            if (tooltip)
            {
                tooltip.transform.position = Input.mousePosition;
            }
            if (tooltip.transform.position.x > maxXPos)
            {
                tooltip.transform.position = new Vector3(maxXPos, Input.mousePosition.y);
            }

            yield return null;
        }
    }
}

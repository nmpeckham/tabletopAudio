using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipPrefab;
    private GameObject tooltip;
    private GameObject tooltipParent;
    public string tooltipText;

    void Start()
    {
        tooltipParent = GameObject.FindGameObjectWithTag("TooltipParent");
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltip = Instantiate(tooltipPrefab, Input.mousePosition, Quaternion.identity, tooltipParent.transform);
        tooltip.GetComponentInChildren<TMP_Text>().text = tooltipText;
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
        while (tooltip)
        {
            tooltip.SetActive(true);
            tooltip.transform.position = Input.mousePosition;
            yield return new WaitForEndOfFrame();
        }
    }
}

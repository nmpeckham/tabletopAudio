using UnityEngine;
using UnityEngine.EventSystems;

public class FftClick : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Camera.main.GetComponent<FftController>().ChangeFftType();
    }
}

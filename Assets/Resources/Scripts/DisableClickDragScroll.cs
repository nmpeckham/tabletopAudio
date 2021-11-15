﻿using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DisableClickDragScroll : ScrollRect
{
    // Disables dragging to scroll
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
}

﻿using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DisableClickDragScroll : ScrollRect
{
    // Disables dragging to scroll
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
}

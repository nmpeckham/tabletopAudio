using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RightClickRootMenu : MonoBehaviour
{
    public Transform buttonParent;
    public Transform addToParent;
    internal float minX, minY, maxX, maxY = 0;
    internal List<GameObject> sideMenuButtons = new();

    internal void AddMenuItem(int id, string label, Transform parent)
    {
        var newItem = Instantiate(Prefabs.rightClickItemPrefab, Input.mousePosition, Quaternion.identity, parent);
        newItem.GetComponent<RightClickItem>().id = id;
        newItem.GetComponent<RightClickItem>().parent = this;
        newItem.GetComponentInChildren<TMP_Text>().text = label;
    }

    internal void SetBounds(float _minX, float _minY, float _maxX, float _maxY)
    {
        minX = _minX;
        minY = _minY;
        maxX = _maxX;
        maxY = _maxY;
    }

    internal IEnumerator CheckMousePos()
    {
        Vector3 mousePos = Input.mousePosition;
        while (true)
        {
            float yDelta = Input.mousePosition.y - mousePos.y;
            float xDelta = Input.mousePosition.x - mousePos.x;

            if (yDelta < minY || yDelta > maxY || xDelta < minX || xDelta > maxX)
            {
                sideMenuButtons.ForEach(go => Destroy(go));

                try
                {
                    Destroy(gameObject);
                }
                catch (MissingReferenceException)
                {
                    break;
                }

                break;
            }
            yield return null;
        }
    }

    internal void AddToSideMenu(int id, string label)
    {
        var newItem = Instantiate(Prefabs.addToMenuItemPrefab, addToParent);
        newItem.SetActive(false);
        newItem.GetComponentInChildren<TMP_Text>().text = label;
        newItem.GetComponentInChildren<RightClickSideMenuButton>().playlistTabId = id;
        sideMenuButtons.Add(newItem);
    }

    internal void HideSideMenu()
    {
        sideMenuButtons.ForEach(go => go.SetActive(false));
    }

    internal void ShowSideMenu()
    {
        sideMenuButtons.ForEach(go => go.SetActive(true));

    }

    internal void OnDestroy()
    {
        sideMenuButtons.ForEach(go => Destroy(go));
        Destroy(gameObject);
    }
}

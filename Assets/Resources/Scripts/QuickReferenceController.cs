using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Jobs;
using Unity.Jobs;
using UnityEngine.UI;

public class QuickReferenceController : MonoBehaviour
{
    private MainAppController mac;
    public GameObject quickRefPanel;
    public GameObject scrollViewContent;
    public TMP_InputField searchBox;
    public GameObject quickRefPrefab;
    public Button clearTextButton;

    // Start is called before the first frame update
    void Start()
    {
        quickRefPanel.SetActive(false);
        mac = GetComponent<MainAppController>();
        searchBox.onValueChanged.AddListener(TextEntered);
        clearTextButton.onClick.AddListener(ClearText);

        //string folder = "C:\\Users\\natha\\Documents\\GitHub\\tabletopAudio\\Assets\\StreamingAssets";
        //string files = "";
        //string[] fileList= System.IO.Directory.GetFiles(folder);

        //foreach (string a in fileList)
        //{

        //    if (Path.GetExtension(a) == ".json")
        //    {
        //        string b = "\"" + a.Replace(folder, "") + "\",";
        //        b = b.Replace("\\", "");
        //        files += b;
        //        files += "\n";
        //        string fileText = File.ReadAllText(a);
        //        fileText = "{\n\"contents\":\n" + fileText + "\n}";
        //        File.WriteAllText(a.Replace("5e-SRD-", ""), fileText);
        //    }
        //}
        //print(files);
    }

    private void ClearText()
    {
        searchBox.text = "";
        searchBox.ActivateInputField();

    }

    private void TextEntered(string val)
    {
        StopAllCoroutines();
        StartCoroutine(DbQuery(val));
    }

    internal void ShowLookupMenu()
    {
        StopAllCoroutines();
        mac.currentMenuState = MainAppController.MenuState.quickReference;
        StartCoroutine(SlideIn());
    }

    internal void HideLookupMenu()
    {
        StopAllCoroutines();
        mac.currentMenuState = MainAppController.MenuState.mainAppView;
        StartCoroutine(SlideOut());
    }

    IEnumerator SlideIn()
    {
        // Unity UI is based on magic, don't change
        quickRefPanel.SetActive(true);
        RectTransform quickRefTransform = quickRefPanel.GetComponent<RectTransform>();
        float endXPos = 0;
        while (quickRefTransform.localPosition.x < endXPos)
        {
            quickRefTransform.localPosition = new Vector3(Mathf.Min(quickRefTransform.localPosition.x + 50, endXPos), quickRefTransform.localPosition.y);
            yield return new WaitForEndOfFrame();
        }
        quickRefTransform.localPosition = new Vector3(endXPos, quickRefTransform.localPosition.y);
        searchBox.ActivateInputField();
    }

    IEnumerator SlideOut()
    {

        searchBox.DeactivateInputField();
        
        // Unity UI is based on magic, don't change
        RectTransform quickRefTransform = quickRefPanel.GetComponent<RectTransform>();

        float endXPos = -quickRefTransform.rect.width;
        while (quickRefTransform.localPosition.x > endXPos)
        {
            quickRefTransform.localPosition = new Vector3(Mathf.Max(quickRefTransform.localPosition.x - 50, endXPos), quickRefTransform.localPosition.y);
            yield return new WaitForEndOfFrame();
        }
        quickRefTransform.localPosition = new Vector3(endXPos, quickRefTransform.localPosition.y);
        foreach (QuickRefPrefab go in scrollViewContent.GetComponentsInChildren<QuickRefPrefab>())
        {
            Destroy(go.gameObject);
        }
        searchBox.text = "";
        quickRefPanel.SetActive(false);
    }

    private IEnumerator DbQuery(string query)
    {
        if (String.IsNullOrEmpty(query))
        {
            foreach (QuickRefPrefab go in scrollViewContent.GetComponentsInChildren<QuickRefPrefab>(true))
            {
                Destroy(go.gameObject);
            }
        }
        List<string> queries = query.ToUpper().Replace("'", "").Split(' ').ToList();
        List<string> matches = new List<string>();

        int numFound = 0;
        List<string> toRemove = new List<string>();
        foreach (string queryItem in queries)
        {
            queryItem.Replace(" ", "");
            if (String.IsNullOrEmpty(queryItem)) toRemove.Add(queryItem);
        }
        foreach (string queryItemToRemove in toRemove)
        {
            queries.Remove(queryItemToRemove);
        }
        int queryIndex = 0;
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (string queryItem in queries)
        {
            List<string> newMatches = new List<string>();
            numFound = 0;
            foreach (QuickRefPrefab go in scrollViewContent.GetComponentsInChildren<QuickRefPrefab>(true))
            {
                toDestroy.Add(go.gameObject);
            }

            foreach (var section in LoadedFilesData.qrdFiles)
            {
                if (numFound > 20) break;
                foreach (var dbItem in section.Value)
                {
                    //print(dbItem.Key);
                    if (numFound > 20) break;
                    string index = dbItem.Key.ToString().ToUpper().Replace("-", "");
                    string alias = "";
                    if (dbItem.Value.ContainsKey("alias"))
                    {
                        alias = dbItem.Value["alias"].ToString().ToUpper();
                    }

                    if (index.Contains(queryItem) || alias.Contains(queryItem))
                    {
                        if ((queryIndex > 0 && matches.Contains(index)) || queryIndex == 0)
                        {
                            GameObject prefab = Instantiate(quickRefPrefab, scrollViewContent.transform);
                            prefab.SetActive(false);
                            QuickRefPrefab qrp = prefab.GetComponent<QuickRefPrefab>();
                            qrp.Category = section.Key.Replace("-", " ");
                            try
                            {
                                foreach (var item in dbItem.Value["desc"].EnumerateArray())
                                {
                                    qrp.Description += item.ToString().Replace("- ", "") + "\n";
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                qrp.Description += dbItem.Value["desc"].ToString();
                            }
                            catch (KeyNotFoundException)
                            {
                                qrp.Description = "";
                            }
                            if (dbItem.Value.ContainsKey("name"))
                            {
                                //move items that start with the same first letter to top
                                if (dbItem.Value["name"].ToString().ToUpper()[0] == queryItem[0])
                                {
                                    prefab.transform.SetSiblingIndex(1);
                                }
                                //move items that are an exact match to top
                                if (dbItem.Value["name"].ToString().ToUpper() == queryItem)
                                {
                                    prefab.transform.SetSiblingIndex(1);
                                }
                                qrp.Title = dbItem.Value["name"].ToString();
                            }
                            else
                            {
                                qrp.Title = index[0] + index.Substring(1, index.Length - 1).ToLower().Replace("-", " ");
                            }
                            newMatches.Add(index);
                            numFound++;
                        }
                    }
                }
            }
            queryIndex++;
            matches = newMatches;
            yield return new WaitForEndOfFrame();
        }
        //destroy after search finished to prevent items in list disappearing for a frame
        foreach (GameObject goToDestroy in toDestroy)
        {
            Destroy(goToDestroy);
        }
        //activate each found object that was instantiated

        foreach (QuickRefPrefab go in scrollViewContent.GetComponentsInChildren<QuickRefPrefab>(true))
        {
            go.gameObject.SetActive(true);
        }
    }
}

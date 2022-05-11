using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuickReferenceController : MonoBehaviour
{
    private MainAppController mac;
    public GameObject quickRefPanel;
    public GameObject scrollViewContent;
    public TMP_InputField searchBox;
    public Button clearTextButton;
    public GameObject searchLoadingSpinner;
    public TMP_Text noResultsText;

    // Start is called before the first frame update
    private void Start()
    {
        //quickRefPanel.SetActive(false);
        mac = GetComponent<MainAppController>();
        searchBox.onValueChanged.AddListener(TextEntered);
        searchBox.onSubmit.AddListener(SubmitSearch);
        clearTextButton.onClick.AddListener(ClearText);
        searchLoadingSpinner.SetActive(false);

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

    internal void GiveSearchFocus()
    {
        searchBox.ActivateInputField();
    }

    void SubmitSearch(string search)
    {
        scrollViewContent.transform.GetChild(0).GetComponent<QuickRefPrefab>().Clicked();
    }
    private void ClearText()
    {
        searchBox.text = "";
        searchBox.ActivateInputField();
        noResultsText.gameObject.SetActive(false);
        searchLoadingSpinner.SetActive(false);

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

    private IEnumerator SlideIn()
    {
        // Unity UI is based on magic, don't change
        //quickRefPanel.SetActive(true);
        RectTransform quickRefTransform = quickRefPanel.GetComponent<RectTransform>();
        float endXPos = 0;
        while (quickRefTransform.localPosition.x < endXPos)
        {
            quickRefTransform.localPosition = new Vector3(Mathf.Min(quickRefTransform.localPosition.x + 50, endXPos), quickRefTransform.localPosition.y);
            yield return null;
        }
        quickRefTransform.localPosition = new Vector3(endXPos, quickRefTransform.localPosition.y);
        searchBox.ActivateInputField();
    }

    private IEnumerator SlideOut()
    {

        searchBox.DeactivateInputField();

        // Unity UI is based on magic, don't change
        RectTransform quickRefTransform = quickRefPanel.GetComponent<RectTransform>();
        float endXPos = -quickRefTransform.rect.width;
        noResultsText.gameObject.SetActive(false);
        searchLoadingSpinner.SetActive(false);
        while (quickRefTransform.localPosition.x > endXPos)
        {
            quickRefTransform.localPosition = new Vector3(Mathf.Max(quickRefTransform.localPosition.x - 50, endXPos), quickRefTransform.localPosition.y);
            yield return null;
        }
        quickRefTransform.localPosition = new Vector3(endXPos, quickRefTransform.localPosition.y);
        foreach (QuickRefPrefab go in scrollViewContent.GetComponentsInChildren<QuickRefPrefab>())
        {
            Destroy(go.gameObject);
        }
        searchBox.text = "";

        //quickRefPanel.SetActive(false);
    }

    private IEnumerator DbQuery(string query)
    {
        noResultsText.gameObject.SetActive(false);
        foreach (QuickRefPrefab go in scrollViewContent.GetComponentsInChildren<QuickRefPrefab>(true))
        {
            Destroy(go.gameObject);
        }
        if (!String.IsNullOrWhiteSpace(query))
        {
            searchLoadingSpinner.SetActive(true);
            //print(String.IsNullOrEmpty(query));
            Regex charsToRemove = new("[^a-zA-Z ]"); //remove any unwanted characters ("';?>.,<_)(*&^%$#@!", etc.)
            MatchCollection searchRemoveChars = charsToRemove.Matches(query);
            foreach (Match m in searchRemoveChars)
            {
                query = query.Replace(m.ToString(), "");
            }
            query = query.ToLower();
            Regex wordMatch = new("[a-zA-Z0-9]+");
            MatchCollection matchedWords = wordMatch.Matches(query);
            List<Match> matchedWordsList = matchedWords.OfType<Match>().ToList();

            List<(KeyValuePair<string, Dictionary<string, dynamic>> item, int score)> searchMatches = new();
            foreach (var section in LoadedFilesData.qrdFiles)
            {
                if (!section.Key.Contains("StartingEquipment") && !section.Key.Contains("Level") && !section.Key.Contains("Spellcasting"))
                {
                    foreach (var dbItem in section.Value)
                    {
                        (KeyValuePair<string, Dictionary<string, dynamic>> item, int score) matchScore = (dbItem, 0);
                        string index = dbItem.Key.ToString().ToLower().Replace("-", " ");
                        string alias = "";
                        if (dbItem.Value.ContainsKey("alias"))
                        {
                            alias = dbItem.Value["alias"].ToString().ToLower();
                        }

                        foreach (Match m in matchedWordsList)
                        {
                            foreach (string item in new string[] { alias, index })
                            {
                                if (!string.IsNullOrWhiteSpace(item))
                                {
                                    string newIndex = item;
                                    MatchCollection indexRemoveChars = charsToRemove.Matches(index);

                                    foreach (Match match in indexRemoveChars)
                                    {
                                        newIndex = newIndex.Replace(match.ToString(), "");
                                    }
                                    Regex wordStartMatch = new(@"\S*(" + m.ToString() + @")\S*");

                                    MatchCollection matchedStartWords = wordStartMatch.Matches(newIndex);
                                    List<Match> matchList = matchedStartWords.OfType<Match>().ToList();

                                    matchScore.score += matchList.Count;
                                }
                            }

                        }
                        if (matchScore.score > 0)
                        {
                            searchMatches.Add(matchScore);
                        }
                        if (mac.currentFPS < 10)
                        {
                            yield return null;
                        }
                    }
                    //yield return null;
                }

            }
            searchMatches = searchMatches.OrderByDescending(item => item.score).ToList();

            //Only show top 20 matches
            if (searchMatches.Count > 20)
            {
                searchMatches.RemoveRange(19, searchMatches.Count - 20);
            }

            searchLoadingSpinner.SetActive(false);
            if (searchMatches.Count == 0)
            {
                noResultsText.gameObject.SetActive(true);
            }
            else
            {
                noResultsText.gameObject.SetActive(false);
            }

            foreach (var searchMatch in searchMatches)
            {

                GameObject prefab = Instantiate(Prefabs.quickRefPrefab, scrollViewContent.transform);
                prefab.SetActive(false);
                QuickRefPrefab qrp = prefab.GetComponent<QuickRefPrefab>();
                qrp.Category = searchMatch.item.Value["categoryName"].Replace("-", " ");
                try
                {
                    foreach (var item in searchMatch.item.Value["desc"].EnumerateArray())
                    {
                        qrp.Description += item.ToString().Replace("- ", "") + "\n";
                    }
                }
                catch (InvalidOperationException)
                {
                    qrp.Description += searchMatch.item.Value["desc"].ToString();
                }
                catch (KeyNotFoundException)
                {
                    qrp.Description = "";
                }
                if (searchMatch.item.Value.ContainsKey("name"))
                {
                    qrp.Title = searchMatch.item.Value["name"].ToString();
                }
                if (searchMatch.item.Value.ContainsKey("index"))
                {
                    qrp.Index = searchMatch.item.Value["index"].ToString();
                }
                qrp.Description = qrp.Description.Replace("\n", "");
                prefab.SetActive(true);
            }
        }
        yield return null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuickRefDetailView : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    public TMP_Text rangeText;
    public TMP_Text componentsText;
    public TMP_Text schoolText;
    public TMP_Text levelText;
    public TMP_Text durationText;
    public TMP_Text castingTimeText;
    public TMP_Text ritualText;
    public TMP_Text damageText;
    public TMP_Text areaOfEffectText;
    public TMP_Text savingThrowText;

    public GameObject range;
    public GameObject components;
    public GameObject school;
    public GameObject level;
    public GameObject duration;
    public GameObject castingTime;
    public GameObject ritual;
    public GameObject damage;
    public GameObject areaOfEffect;
    public GameObject savingThrow;

    public GameObject quickRefObj;
    private MainAppController mac;

    public GameObject attributesPanel;
    public GameObject descriptionPanel;

    public Button closeButton;

    // Start is called before the first frame update
    void Start()
    {
        mac = GetComponent<MainAppController>();
        closeButton.onClick.AddListener(CloseQuickReferenceDetail);
    }

    internal void CloseQuickReferenceDetail()
    {
        quickRefObj.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.quickReference;
    }

    internal void ItemSelected(string category, string item)
    {
        System.Text.Json.JsonElement value;
        print(item);
        print(category);
        if (category == "Spell")
        {
            descriptionPanel.SetActive(true);
            quickRefObj.SetActive(true);
            mac.currentMenuState = MainAppController.MenuState.quickReferenceDetail;

            school.SetActive(true);
            duration.SetActive(true);
            castingTime.SetActive(true);
            range.SetActive(true);
            components.SetActive(true);
            ritual.SetActive(true);
            level.SetActive(true);

            damage.SetActive(true);
            areaOfEffect.SetActive(true);
            savingThrow.SetActive(true);
            range.GetComponentInChildren<TMP_Text>().text = "Range";
            damage.GetComponentInChildren<TMP_Text>().text = "Damage";


            components.GetComponentInChildren<TMP_Text>().text = "Components";

            foreach (Dictionary<string, dynamic> listItem in LoadedFilesData.qrdFiles[category])
            {
                if (listItem["index"].ToString() == item)
                {
                    print(listItem["index"]);
                    titleText.text = listItem["name"].ToString();
                    rangeText.text = listItem["range"].ToString();
                    listItem["school"].TryGetProperty("name", out value);// ["name"].ToString();
                    schoolText.text = value.ToString();
                    durationText.text = listItem["duration"].ToString();
                    castingTimeText.text = listItem["casting_time"].ToString();
                    levelText.text = listItem["level"].ToString();
                    ritualText.text = listItem["ritual"].ToString();

                    if (listItem.ContainsKey("dc"))
                    {
                        listItem["dc"].TryGetProperty("dc_type", out value);
                        value.TryGetProperty("name", out System.Text.Json.JsonElement temp1);
                        savingThrowText.text = temp1.ToString();
                    }

                    else savingThrowText.text = "N/A";


                    componentsText.text = "";
                    foreach (var component in listItem["components"].EnumerateArray())
                    {
                        componentsText.text += component.ToString();
                    }

                    if (listItem.ContainsKey("area_of_effect"))
                    {
                        listItem["area_of_effect"].TryGetProperty("size", out value);
                        areaOfEffectText.text = value.ToString() + "ft ";
                        listItem["area_of_effect"].TryGetProperty("type", out value);
                        areaOfEffectText.text += value.ToString();
                    }
                    else areaOfEffectText.text = "N/A";

                    if (listItem.ContainsKey("damage"))
                    {
                        listItem["damage"].TryGetProperty("damage_at_slot_level", out value);
                        value.TryGetProperty(listItem["level"].ToString(), out System.Text.Json.JsonElement temp1);
                        damageText.text = temp1.ToString() + " ";
                        listItem["damage"].TryGetProperty("damage_type", out value);
                        value.TryGetProperty("name", out System.Text.Json.JsonElement temp2);
                        damageText.text += temp2.ToString();
                    }
                    else
                    {
                        damageText.text = "N/A";
                    }



                    descriptionText.text = "";
                    if (listItem["ritual"].ToString() == "True")
                    {
                        descriptionText.text += "Can be cast as a ritual spell that takes 10 minutes + original casting time.\n\n";
                    }
                    descriptionText.text += "<b>At Level " + listItem["level"] + ":</b>";
                    foreach (var desc in listItem["desc"].EnumerateArray())
                    {
                        descriptionText.text += "\n\n" + desc.ToString();
                    }
                    if (listItem.ContainsKey("higher_level"))
                    {
                        descriptionText.text += "\n\n" + "<b>At Higher Levels:</b>";
                        foreach (var desc in listItem["higher_level"].EnumerateArray())
                        {
                            descriptionText.text += "\n\n" + desc.ToString();
                        }
                    }
                    if(listItem.ContainsKey("material"))
                    {
                        descriptionText.text += "\n\n" + "<b>Materials</b>\n";
                        try
                        {
                            foreach (var materialItem in listItem["material"].EnumerateArray())
                            {
                                descriptionText.text += materialItem.ToString() + "\n";
                            }
                        }
                        catch(System.InvalidOperationException)
                        {
                            descriptionText.text += listItem["material"].ToString() + "\n";
                        }

                    }
                    break;
                }
            }
        }
        else if (category == "Equipment")
        {

            foreach (Dictionary<string, dynamic> listItem in LoadedFilesData.qrdFiles[category])
            {
                if (listItem["index"].ToString() == item && listItem.ContainsKey("damage"))
                {
                    titleText.text = listItem["name"].ToString();
                    descriptionPanel.SetActive(false);
                    quickRefObj.SetActive(true);
                    savingThrow.SetActive(false);
                    level.SetActive(false);
                    duration.SetActive(false);
                    castingTime.SetActive(false);
                    ritual.SetActive(false);
                    areaOfEffect.SetActive(false);

                    listItem["damage"].TryGetProperty("damage_dice", out value);
                    damageText.text = value.ToString() + " ";
                    listItem["damage"].TryGetProperty("damage_type", out value);// ["name"].ToString();
                    value.TryGetProperty("name", out System.Text.Json.JsonElement temp2);
                    damageText.text += temp2.ToString();
                    if (listItem.ContainsKey("range"))
                    {
                        listItem["range"].TryGetProperty("normal", out value);
                        rangeText.text = value.ToString() + "ft";
                        listItem["range"].TryGetProperty("long", out value);
                        if (!string.IsNullOrEmpty(value.ToString()))
                        {
                            rangeText.text += ", " + value.ToString() + "ft";
                        }
                    }

                    components.GetComponentInChildren<TMP_Text>().text = "Category";
                    componentsText.text = listItem["weapon_category"].ToString();

                    school.GetComponentInChildren<TMP_Text>().text = "Properties";
                    schoolText.text = "";
                    int i = 0;
                    foreach (var property in listItem["properties"].EnumerateArray())
                    {
                        property.TryGetProperty("name", out value);
                        if (i == 0)
                        {
                            schoolText.text += value.ToString();
                        }
                        else
                        {
                            schoolText.text += ", " + value.ToString();
                        }
                        i++;

                    }
                    break;
                }
            }
        }
        else if (category == "Condition")
        {
            quickRefObj.SetActive(true);
            attributesPanel.SetActive(false);
            descriptionPanel.SetActive(true);
            mac.currentMenuState = MainAppController.MenuState.quickReferenceDetail;
            foreach (Dictionary<string, dynamic> listItem in LoadedFilesData.qrdFiles[category])
            {

                if (listItem["index"].ToString() == item)
                {
                    descriptionText.text = "";
                    titleText.text = listItem["name"].ToString();
                    foreach (var desc in listItem["desc"].EnumerateArray())
                    {
                        descriptionText.text += desc.ToString() + "\n\n";
                    }
                    break;
                }
            }
        }
        else if(category == "Language")
        {
            quickRefObj.SetActive(true);
            attributesPanel.SetActive(true);
            descriptionPanel.SetActive(false);
            mac.currentMenuState = MainAppController.MenuState.quickReferenceDetail;
            damage.SetActive(true);
            range.SetActive(true);
            components.SetActive(true);
            school.SetActive(false);
            level.SetActive(false);
            duration.SetActive(false);
            castingTime.SetActive(false);
            ritual.SetActive(false);
            areaOfEffect.SetActive(false);
            savingThrow.SetActive(false);

            foreach (Dictionary<string, dynamic> listItem in LoadedFilesData.qrdFiles[category])
            {

                if (listItem["index"].ToString() == item)
                {
                    //if (listItem.ContainsKey("desc"))
                    //{
                    //    descriptionText.text = "";
                    //    descriptionPanel.SetActive(true);
                    //    descriptionText.text += listItem["desc"].ToString();
                    //}

                    titleText.text = listItem["name"].ToString();

                    damage.GetComponentInChildren<TMP_Text>().text = "Script";
                    damageText.text = listItem["script"].ToString();
                    components.GetComponentInChildren<TMP_Text>().text = "Type";
                    componentsText.text = listItem["type"].ToString();
                    range.GetComponentInChildren<TMP_Text>().text = "Typical Speakers";
                    rangeText.text = "";
                    int i = 1;
                    foreach (var speaker in listItem["typical_speakers"].EnumerateArray())
                    {
                        //Don't add comma on last item
                        if (i == listItem["typical_speakers"].GetArrayLength()) rangeText.text += speaker.ToString();
                        else rangeText.text += speaker + ", ";
                        i++;
                    }

                    break;
                }
            }


        }
        else
        {
            quickRefObj.SetActive(false);
        }

    }
}

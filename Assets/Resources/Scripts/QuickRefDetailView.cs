using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;



public class QuickRefDetailView : MonoBehaviour
{

    public TMP_Text titleText;
    public TMP_Text descriptionText;

    public Image quickRefCategoryImage;
    public TMP_Text quickRefCategoryText;
    public GameObject quickRefObj;
    private MainAppController mac;

    public GameObject quickRefAttributePrefab;
    public GameObject attributesPanel;
    public GameObject descriptionPanel;

    public Button closeButton;

    // Start is called before the first frame update
    void Start()
    {
        mac = GetComponent<MainAppController>();
        closeButton.onClick.AddListener(CloseQuickReferenceDetail);
        //TestAllItems();
    }

    private void TestAllItems()
    {
        foreach(string category in LoadedFilesData.qrdFiles.Keys)
        {
            foreach(var item in LoadedFilesData.qrdFiles[category])
            {
                ItemSelected(category, item.Key);
            }
        }
    }

    internal void CloseQuickReferenceDetail()
    {
        quickRefObj.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.quickReference;
    }

    internal void ItemSelected(string category, string item)
    {
        //print(category);
        //print(item);
        DestroyAllAttributeItems();
        System.Text.Json.JsonElement extractedJsonValue;
        string categoryFileName = category.Replace(" ", "-") + ".json";
        quickRefCategoryImage.color = ResourceManager.categoryColors[categoryFileName];
        quickRefCategoryText.text = category.Replace("-", " ");
        mac.currentMenuState = MainAppController.MenuState.quickReferenceDetail;
        Dictionary<string, string> attributes = new Dictionary<string, string>();
        if (category == "Spell")
        {
            descriptionPanel.SetActive(true);
            quickRefObj.SetActive(true);
            attributesPanel.SetActive(true);
            
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            titleText.text = listItem["name"].ToString();
            attributes.Add("Range", listItem["range"].ToString());
            listItem["school"].TryGetProperty("name", out extractedJsonValue);
            attributes.Add("School", extractedJsonValue.ToString());
            attributes.Add("Duration", listItem["duration"].ToString());
            attributes.Add("Casting Time", listItem["casting_time"].ToString());
            attributes.Add("Level", listItem["level"].ToString());
            attributes.Add("Ritual", listItem["ritual"].ToString());

            string dcText;
            if (listItem.ContainsKey("dc"))
            {
                listItem["dc"].TryGetProperty("dc_type", out extractedJsonValue);
                extractedJsonValue.TryGetProperty("name", out System.Text.Json.JsonElement temp1);
                dcText = temp1.ToString();
            }

            else dcText = "N/A";
            attributes.Add("Saving Throw", dcText);


            string componentsText = "";
            foreach (var component in listItem["components"].EnumerateArray())
            {
                componentsText += component.ToString();
            }
            attributes.Add("Components", componentsText);

            string areaOfEffectText;
            if (listItem.ContainsKey("area_of_effect"))
            {
                listItem["area_of_effect"].TryGetProperty("size", out extractedJsonValue);
                areaOfEffectText = extractedJsonValue.ToString() + "ft ";
                listItem["area_of_effect"].TryGetProperty("type", out extractedJsonValue);
                areaOfEffectText += extractedJsonValue.ToString();
            }
            else areaOfEffectText = "N/A";
            attributes.Add("Area Of Effect", areaOfEffectText);

            string damageText = "";
            if (listItem.ContainsKey("damage"))
            {
                bool error = false;
                try
                {
                    listItem["damage"].TryGetProperty("damage_at_slot_level", out extractedJsonValue);
                    extractedJsonValue.TryGetProperty(listItem["level"].ToString(), out System.Text.Json.JsonElement temp1);
                    damageText = temp1.ToString() + " ";
                }
                catch (System.InvalidOperationException)
                {
                    error = true;
                }
                if (error)
                {
                    try
                    {
                        // Cantrip, get damage for first level. Description provides enough info for higher levels
                        listItem["damage"].TryGetProperty("damage_at_character_level", out extractedJsonValue);
                        extractedJsonValue.TryGetProperty("1", out System.Text.Json.JsonElement temp1);
                        damageText = temp1.ToString() + " ";
                    }
                    catch(System.InvalidOperationException) { }
                }
                System.Text.Json.JsonElement temp2 = new System.Text.Json.JsonElement();
                listItem["damage"].TryGetProperty("damage_type", out extractedJsonValue);
                try
                {
                    extractedJsonValue.TryGetProperty("name", out temp2);
                }
                catch(System.InvalidOperationException) { }
                damageText += string.IsNullOrEmpty(temp2.ToString()) ? "any" : temp2.ToString();
            }
            else
            {
                damageText = "N/A";
            }
            attributes.Add("Damage", damageText);

            descriptionText.text = "";
            if (listItem["ritual"].ToString() == "True")
            {
                descriptionText.text += "Can be cast as a ritual spell that takes 10 minutes + original casting time.\n\n";
            }
            descriptionText.text += Title("At Level " + listItem["level"].ToString());
            foreach (var desc in listItem["desc"].EnumerateArray())
            {
                descriptionText.text += Body(desc.ToString());
            }
            if (listItem.ContainsKey("higher_level"))
            {
                descriptionText.text += Title("At Higher Levels");
                foreach (var desc in listItem["higher_level"].EnumerateArray())
                {
                    descriptionText.text += Body(desc.ToString());
                }
            }
            if (listItem.ContainsKey("material"))
            {
                descriptionText.text += Title("Materials");
                try
                {
                    foreach (var materialItem in listItem["material"].EnumerateArray())
                    {
                        descriptionText.text += Body(materialItem.ToString());
                    }
                }
                catch (System.InvalidOperationException)
                {
                    descriptionText.text += listItem["material"].ToString() + "\n";
                }

            }
        }
        else if (category == "Equipment")
        {
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];
            quickRefObj.SetActive(true);
            titleText.text = listItem["name"].ToString();

            if (listItem.ContainsKey("damage"))
            {


                descriptionPanel.SetActive(false);
                quickRefObj.SetActive(true);


                listItem["damage"].TryGetProperty("damage_dice", out extractedJsonValue);
                string damageText = extractedJsonValue.ToString() + " ";
                listItem["damage"].TryGetProperty("damage_type", out extractedJsonValue);// ["name"].ToString();
                extractedJsonValue.TryGetProperty("name", out System.Text.Json.JsonElement temp2);
                damageText += temp2.ToString();
                attributes.Add("Damage", damageText);

                string rangeText;
                if (listItem.ContainsKey("range"))
                {
                    listItem["range"].TryGetProperty("normal", out extractedJsonValue);
                    rangeText = extractedJsonValue.ToString() + "ft";
                    listItem["range"].TryGetProperty("long", out extractedJsonValue);
                    if (!string.IsNullOrEmpty(extractedJsonValue.ToString()))
                    {
                        rangeText += ", " + extractedJsonValue.ToString() + "ft";
                    }
                }
                else rangeText = "N/A";
                attributes.Add("Range", rangeText);

                attributes.Add("Category", listItem["weapon_category"].ToString());

                int i = 0;
                string propertiesText = "";
                foreach (var property in listItem["properties"].EnumerateArray())
                {
                    property.TryGetProperty("name", out extractedJsonValue);
                    if (i == 0)
                    {
                        propertiesText += extractedJsonValue.ToString();
                    }
                    else
                    {
                        propertiesText += ", " + extractedJsonValue.ToString();
                    }
                    i++;
                }
                attributes.Add("Properties", propertiesText);
            }
            else
            {
                if(listItem.ContainsKey("desc"))
                {
                    descriptionText.text = "";
                    foreach (var desc in listItem["desc"].EnumerateArray())
                    {
                        descriptionText.text += desc.ToString();// + "\n\n";
                    }
                }
            }
        }
        else if (category == "Condition")
        {
            quickRefObj.SetActive(true);
            attributesPanel.SetActive(false);
            descriptionPanel.SetActive(true);
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            descriptionText.text = "";
            titleText.text = listItem["name"].ToString();
            foreach (var desc in listItem["desc"].EnumerateArray())
            {
                descriptionText.text += desc.ToString() + "\n\n";
            }
        }
        else if (category == "Language")
        {
            quickRefObj.SetActive(true);
            attributesPanel.SetActive(true);
            descriptionPanel.SetActive(false);

            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            titleText.text = listItem["name"].ToString();

            string scriptText = listItem["script"].ToString();
            attributes.Add("Script", scriptText);

            attributes.Add("Type", listItem["type"].ToString());

            string typicalSpeakersText = "";
            int i = 1;
            foreach (var speaker in listItem["typical_speakers"].EnumerateArray())
            {
                //Don't add comma on last item
                if (i == listItem["typical_speakers"].GetArrayLength()) typicalSpeakersText += speaker.ToString();
                else typicalSpeakersText += speaker + ", ";
                i++;
            }
            attributes.Add("Typical Speakers", typicalSpeakersText);


        }
        else if (category == "Magic-School")
        {
            quickRefObj.SetActive(true);
            attributesPanel.SetActive(false);
            descriptionPanel.SetActive(true);

            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];
            titleText.text = listItem["name"].ToString();
            descriptionText.text = listItem["desc"].ToString();
        }
        else if (category == "Monster")
        {//AC, Resistances, Hit dice/hp, immunities, speed, vulnerabilities, condition immunities, type & subtype, CR/xp, languages
            quickRefObj.SetActive(true);
            attributesPanel.SetActive(true);
            print("Item: " + item);
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];
            titleText.text = listItem["name"].ToString();
            attributes.Add("Size", listItem["size"].ToString());
            attributes.Add("Type", Capitalize(listItem["type"].ToString()));
            attributes.Add("Armor Class", listItem["armor_class"].ToString());
            attributes.Add("Alignment", Capitalize(listItem["alignment"].ToString().Split(' ')));

            string speedText = Capitalize(listItem["speed"].ToString());

            attributes.Add("Speed", speedText);
            attributes.Add("Hit Dice", listItem["hit_dice"].ToString() + " (" + listItem["hit_points"].ToString() + "hp)");
            //attributes.Add("CR", listItem["challenge_rating"].ToString());

            string damageResistances = listItem["damage_resistances"].ToString();
            attributes.Add("Damage Resistances", string.IsNullOrEmpty(damageResistances) ? "N/A" : Capitalize(damageResistances));

            string damageImmunities = listItem["damage_immunities"].ToString();
            attributes.Add("Damage Immunities", string.IsNullOrEmpty(damageImmunities) ? "N/A" : Capitalize(damageImmunities));

            string damageVulnerabilities = listItem["damage_vulnerabilities"].ToString();
            attributes.Add("Damage Vulnerabilities", string.IsNullOrEmpty(damageVulnerabilities) ? "N/A" : Capitalize(damageVulnerabilities));

            string conditionImmunities = listItem["condition_immunities"].ToString();
            attributes.Add("Condition Immunities", string.IsNullOrEmpty(conditionImmunities) ? "N/A" : Capitalize(conditionImmunities)); ;
            
            string str = listItem["strength"].ToString();
            string dex = listItem["dexterity"].ToString();
            string con = listItem["constitution"].ToString();
            string intel = listItem["intelligence"].ToString();
            string wis = listItem["wisdom"].ToString();
            string cha = listItem["charisma"].ToString();

            string descText = "<mspace=11><b>STR | DEX | CON | INT | WIS | CHA</b>\n"; //dirty, should be improved
            descText += str.PadRight(3) + " | " + dex.PadRight(3) + " | " + con.PadRight(3) + " | " + intel.PadRight(3) + " | " + wis.PadRight(3) + " | " + cha.PadRight(3) + "</mspace>\n";
            
            if(listItem.ContainsKey("actions"))
            {
                descText += Title("Actions");
                foreach (var actionItem in listItem["actions"].EnumerateArray())
                {
                    actionItem.TryGetProperty("name", out extractedJsonValue);
                    descText += Subtitle(extractedJsonValue.ToString());
                    actionItem.TryGetProperty("desc", out extractedJsonValue);
                    descText += Body(extractedJsonValue.ToString());
                }
            }

            if (listItem.ContainsKey("special_abilities")) {
                descText += Title("Special Abilities");
                foreach (var specialItem in listItem["special_abilities"].EnumerateArray())
                {
                    specialItem.TryGetProperty("name", out extractedJsonValue);
                    descText += Subtitle(extractedJsonValue.ToString());
                    specialItem.TryGetProperty("desc", out extractedJsonValue);
                    descText += Body(extractedJsonValue.ToString());

                }
            }

            if (listItem.ContainsKey("legendary_actions"))
            {
                descText += Title("Legendary Actions");
                if (listItem.ContainsKey("legendary_desc"))
                {
                    descText += Body(listItem["legendary_desc"].ToString());
                }
                foreach (var legendaryItem in listItem["legendary_actions"].EnumerateArray())
                {
                    legendaryItem.TryGetProperty("name", out extractedJsonValue);
                    descText += Subtitle(extractedJsonValue.ToString());
                    legendaryItem.TryGetProperty("desc", out extractedJsonValue);
                    descText += Body(extractedJsonValue.ToString());
                }
            }
            descriptionText.text = descText;
        }
        else if (category == "Magic-Item" || category == "Rule-Section" || category == "Feature")
        {
            quickRefObj.SetActive(true);
            //attributesPanel.SetActive(true);
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];
            titleText.text = listItem["name"].ToString();

            descriptionText.text = "";
            int i = 0;
            try
            {
                foreach (var desc in listItem["desc"].EnumerateArray())
                {
                    if (i == 0 && category == "Magic-Item") descriptionText.text += Subtitle(desc.ToString());// + "\n\n";
                    else descriptionText.text += Body(desc.ToString());// + "\n\n";
                    i++;
                }
            }
            catch(System.InvalidOperationException)
            {
                descriptionText.text = listItem["desc"].ToString();
            }

        }
        else
        {
            quickRefObj.SetActive(false);
            mac.currentMenuState = MainAppController.MenuState.quickReference;
        }
        foreach(KeyValuePair<string, string> attr in attributes)
        {
            CreateAttributeItem(attr.Key, attr.Value);
        }
    }
    void CreateAttributeItem(string title, string detail)
    {
        GameObject go = Instantiate(quickRefAttributePrefab, attributesPanel.transform);
        QuickRefAttributePrefab qrap = go.GetComponent<QuickRefAttributePrefab>();
        qrap.Title = title;
        qrap.Detail = detail;
    }

    void DestroyAllAttributeItems()
    {
        foreach(QuickRefAttributePrefab qrap in attributesPanel.GetComponentsInChildren<QuickRefAttributePrefab>())
        {
            Destroy(qrap.gameObject);
        }
    }

    string Capitalize(string toCaps)
    {
        return toCaps.Substring(0, 1).ToUpper() + toCaps.Substring(1, toCaps.Length - 1);
    }

    string Capitalize(string[] toCaps)
    {
        string ret = "";
        foreach(string s in toCaps)
        {
            ret += Capitalize(s) + " ";
        }
        return ret;
    }

    string Margin(string toIndent, int amount)
    {
        return string.Format("<margin={0}%>", amount) + toIndent + "</margin>";
    }

    string Title(string titleString)
    {
        return "<b><u><size=20>" + titleString + "</b></u></size>\n";
    }

    string Subtitle(string subtitleString)
    {
        int subtitleMarginAmount = 2;
        return Margin("<b><size=16>" + subtitleString + "</size></b>", subtitleMarginAmount) +"\n";
    }
    string Body(string bodyString)
    {
        int bodyMarginAmount = 4;
        return Margin(bodyString, bodyMarginAmount) + "\n";
    }
}

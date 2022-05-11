using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class QuickRefDetailView : MonoBehaviour
{

    public TMP_Text titleText;
    public TMP_Text descriptionText;

    public Image quickRefCategoryImage;
    public TMP_Text quickRefCategoryText;
    public GameObject quickRefObj;
    private MainAppController mac;

    public GameObject attributesPanel;
    public GameObject descriptionPanel;

    public Button closeButton;

    // Start is called before the first frame update
    private void Start()
    {
        mac = GetComponent<MainAppController>();
        closeButton.onClick.AddListener(CloseQuickReferenceDetail);
        //TestAllItems();
    }

    private void TestAllItems()
    {
        foreach (string category in LoadedFilesData.qrdFiles.Keys)
        {
            foreach (var item in LoadedFilesData.qrdFiles[category])
            {
                ItemSelected(category, item.Key);
            }
        }
    }

    internal void CloseQuickReferenceDetail()
    {
        quickRefObj.SetActive(false);
        mac.currentMenuState = MainAppController.MenuState.quickReference;
        mac.gameObject.GetComponent<QuickReferenceController>().GiveSearchFocus();
    }

    internal void ItemSelected(string category, string item)
    {
        print(category);
        print(item);
        DestroyAllAttributeItems();
        System.Text.Json.JsonElement extractedJsonValue;
        string categoryFileName = category.Replace(" ", "-");
        quickRefCategoryImage.color = ResourceManager.categoryColors[categoryFileName];
        quickRefCategoryText.text = category.Replace("-", " ");
        mac.currentMenuState = MainAppController.MenuState.quickReferenceDetail;
        Dictionary<string, string> attributes = new();
        descriptionText.text = "";
        if (category.Contains("Spell"))
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
            attributes.Add("Ritual", Capitalize(listItem["ritual"].ToString()));

            string dcText;
            if (listItem.ContainsKey("dc"))
            {
                listItem["dc"].TryGetProperty("dc_type", out extractedJsonValue);
                extractedJsonValue.TryGetProperty("name", out System.Text.Json.JsonElement temp1);
                dcText = temp1.ToString();
            }

            else
            {
                dcText = "N/A";
            }

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
            else
            {
                areaOfEffectText = "N/A";
            }

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
                    catch (System.InvalidOperationException) { }
                }
                System.Text.Json.JsonElement temp2 = new();
                listItem["damage"].TryGetProperty("damage_type", out extractedJsonValue);
                try
                {
                    extractedJsonValue.TryGetProperty("name", out temp2);
                }
                catch (System.InvalidOperationException) { }
                damageText += string.IsNullOrEmpty(temp2.ToString()) ? "any" : temp2.ToString();
            }
            else
            {
                damageText = "N/A";
            }
            attributes.Add("Damage", damageText);

            descriptionText.text = "";

            int i = 0;
            descriptionText.text += Title("Classes");
            string classText = "";
            foreach (var castClass in listItem["classes"].EnumerateArray())
            {
                if (i > 0)
                {
                    classText += ", ";
                }

                castClass.TryGetProperty("name", out extractedJsonValue);
                classText += extractedJsonValue.ToString();
                i++;
            }
            descriptionText.text += Body(classText);

            descriptionText.text += Title("At Level " + listItem["level"].ToString());
            foreach (var desc in listItem["desc"].EnumerateArray())
            {
                descriptionText.text += Body(desc.ToString());
            }
            if (listItem["ritual"].ToString() == "True")
            {
                descriptionText.text += Body("Can be cast as a ritual spell that takes 10 minutes + original casting time.");
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
                catch (System.InvalidOperationException)    //Not a list, only one component
                {
                    descriptionText.text += Body(listItem["material"].ToString());
                }

            }
        }
        else if (category == "Equipment")
        {
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];
            quickRefObj.SetActive(true);
            titleText.text = listItem["name"].ToString();
            descriptionPanel.SetActive(false);
            if (listItem.ContainsKey("speed"))
            {
                listItem["speed"].TryGetProperty("quantity", out extractedJsonValue);
                string speedText = extractedJsonValue.ToString();
                listItem["speed"].TryGetProperty("unit", out extractedJsonValue);
                speedText += " " + extractedJsonValue.ToString();
                attributes.Add("Speed", speedText);
            }
            if (listItem.ContainsKey("vehicle_category"))
            {
                attributes.Add("Vehicle Type", listItem["vehicle_category"].ToString());
            }
            if (listItem.ContainsKey("weight"))
            {
                attributes.Add("Weight", listItem["weight"].ToString() + " lb");
            }
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
            else
            {
                rangeText = "N/A";
            }

            attributes.Add("Range", rangeText);
            int i = 0;
            if (listItem.ContainsKey("properties"))
            {
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
            if (listItem.ContainsKey("special"))
            {
                string specialText = "";
                descriptionPanel.SetActive(true);
                foreach (var specialItem in listItem["special"].EnumerateArray())
                {
                    if (i > 0)
                    {
                        specialText += "\n";
                    }
                    //specialItem.TryGetProperty("name", out extractedJsonValue);
                    specialText += specialItem.ToString() + "\n";
                    i++;
                }
                descriptionText.text += specialText;
            }
            i = 0;
            if (listItem.ContainsKey("damage"))
            {

                listItem["damage"].TryGetProperty("damage_dice", out extractedJsonValue);
                string damageText = extractedJsonValue.ToString() + " ";
                listItem["damage"].TryGetProperty("damage_type", out extractedJsonValue);// ["name"].ToString();
                extractedJsonValue.TryGetProperty("name", out System.Text.Json.JsonElement temp2);
                damageText += temp2.ToString();
                attributes.Add("Damage", damageText);

                attributes.Add("Category", listItem["weapon_category"].ToString());
            }
            else
            {
                if (listItem.ContainsKey("desc"))
                {
                    descriptionPanel.SetActive(true);
                    descriptionText.text = "";
                    foreach (var desc in listItem["desc"].EnumerateArray())
                    {
                        descriptionText.text += desc.ToString() + "\n";// + "\n\n";
                    }
                }
            }
            if (LoadedFilesData.qrdFiles["Proficiency"].ContainsKey(item + "s"))
            {
                Dictionary<string, dynamic> profListItem = LoadedFilesData.qrdFiles["Proficiency"][item + "s"];
                i = 0;
                string classText = "";
                if (profListItem.ContainsKey("classes") || listItem.ContainsKey("races"))
                {
                    foreach (var weaponClass in profListItem["classes"].EnumerateArray())
                    {
                        if (i > 0)
                        {
                            classText += ", ";
                        }

                        weaponClass.TryGetProperty("name", out extractedJsonValue);
                        classText += extractedJsonValue.ToString();
                        i++;
                    }

                    string raceText = "";
                    i = 0;
                    foreach (var weaponRace in profListItem["races"].EnumerateArray())
                    {
                        if (i > 0)
                        {
                            classText += ", ";
                        }

                        weaponRace.TryGetProperty("name", out extractedJsonValue);
                        raceText += extractedJsonValue.ToString();
                        i++;
                    }

                    attributes.Add("Proficient Classes", string.IsNullOrWhiteSpace(classText) ? "None" : classText);
                    attributes.Add("Proficient Races", string.IsNullOrWhiteSpace(raceText) ? "None" : raceText);
                }
            }
            if (listItem.ContainsKey("cost"))
            {
                listItem["cost"].TryGetProperty("quantity", out extractedJsonValue);
                string costText = extractedJsonValue.ToString();
                listItem["cost"].TryGetProperty("unit", out extractedJsonValue);
                costText += extractedJsonValue.ToString();
                attributes.Add("Cost", costText);
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
                if (i == listItem["typical_speakers"].GetArrayLength())
                {
                    typicalSpeakersText += speaker.ToString();
                }
                else
                {
                    typicalSpeakersText += speaker + ", ";
                }

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

            if (listItem.ContainsKey("actions"))
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

            if (listItem.ContainsKey("special_abilities"))
            {
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
            descriptionPanel.SetActive(true);
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];
            titleText.text = listItem["name"].ToString();

            descriptionText.text = "";
            if (category == "Feature")
            {
                listItem["class"].TryGetProperty("name", out extractedJsonValue);
                descriptionText.text += Title("Class: " + extractedJsonValue.ToString());
                listItem["subclass"].TryGetProperty("name", out extractedJsonValue);
                if (!string.IsNullOrWhiteSpace(extractedJsonValue.ToString()))
                {
                    descriptionText.text += Subtitle("Subclass: " + extractedJsonValue.ToString());
                }
            }
            int i = 0;
            try
            {
                foreach (var desc in listItem["desc"].EnumerateArray())
                {
                    if (i == 0 && category == "Magic-Item")
                    {
                        descriptionText.text += Subtitle(desc.ToString());
                    }
                    else
                    {
                        descriptionText.text += Body(desc.ToString());
                    }

                    i++;
                }
            }
            catch (System.InvalidOperationException)
            {
                descriptionText.text = listItem["desc"].ToString();
            }
        }
        else if (category == "Proficiency")
        {
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            if (listItem.ContainsKey("classes") || listItem.ContainsKey("races"))
            {
                quickRefObj.SetActive(true);
                descriptionPanel.SetActive(false);
                attributesPanel.SetActive(true);
                titleText.text = listItem["name"].ToString();

                int i = 0;
                string classText = "";
                foreach (var weaponClass in listItem["classes"].EnumerateArray())
                {
                    if (i > 0)
                    {
                        classText += ", ";
                    }

                    weaponClass.TryGetProperty("name", out extractedJsonValue);
                    classText += extractedJsonValue.ToString();
                    i++;
                }

                string raceText = "";
                i = 0;
                foreach (var weaponRace in listItem["races"].EnumerateArray())
                {
                    if (i > 0)
                    {
                        classText += ", ";
                    }

                    weaponRace.TryGetProperty("name", out extractedJsonValue);
                    raceText += extractedJsonValue.ToString();
                    i++;
                }

                string skillText = "";
                i = 0;
                foreach (var skillReference in listItem["references"].EnumerateArray())
                {
                    if (i > 0)
                    {
                        skillText += ", ";
                    }

                    skillReference.TryGetProperty("type", out extractedJsonValue);
                    if (extractedJsonValue.ToString() == "skills")
                    {
                        skillReference.TryGetProperty("name", out extractedJsonValue);
                        skillText += extractedJsonValue.ToString();
                        i++;
                    }

                }


                attributes.Add("Proficient Classes", string.IsNullOrWhiteSpace(classText) ? "None" : classText);
                attributes.Add("Proficient Races", string.IsNullOrWhiteSpace(raceText) ? "None" : raceText);
                attributes.Add("Skills", string.IsNullOrWhiteSpace(skillText) ? "None" : skillText);
            }



        }
        else if (category == "Skill")
        {
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            if (listItem.ContainsKey("ability_score"))
            {
                quickRefObj.SetActive(true);
                descriptionPanel.SetActive(true);
                attributesPanel.SetActive(true);
                titleText.text = listItem["name"].ToString();

                string scoreText = "";
                var abilityScore = listItem["ability_score"];

                abilityScore.TryGetProperty("name", out extractedJsonValue);
                scoreText += extractedJsonValue.ToString();

                int i = 0;
                try
                {
                    foreach (var desc in listItem["desc"].EnumerateArray())
                    {
                        descriptionText.text += Body(desc.ToString());
                        i++;
                    }
                }
                catch (System.InvalidOperationException)
                {
                    descriptionText.text = listItem["desc"].ToString();
                }
                attributes.Add("Ability Score", string.IsNullOrWhiteSpace(scoreText) ? "None" : scoreText);
            }
        }
        else if (category == "Trait")
        {
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            if (listItem.ContainsKey("races") || listItem.ContainsKey("subraces"))
            {
                quickRefObj.SetActive(true);
                descriptionPanel.SetActive(true);
                attributesPanel.SetActive(true);
                titleText.text = listItem["name"].ToString();

                string raceText = "";
                int i = 0;
                foreach (var traitRace in listItem["races"].EnumerateArray())
                {
                    if (i > 0)
                    {
                        raceText += ", ";
                    }

                    traitRace.TryGetProperty("name", out extractedJsonValue);
                    raceText += extractedJsonValue.ToString();
                    i++;
                }

                string subraceText = "";
                i = 0;
                foreach (var traitSubrace in listItem["subraces"].EnumerateArray())
                {
                    if (i > 0)
                    {
                        subraceText += ", ";
                    }

                    traitSubrace.TryGetProperty("name", out extractedJsonValue);
                    subraceText += extractedJsonValue.ToString();
                    i++;
                }

                try
                {
                    foreach (var desc in listItem["desc"].EnumerateArray())
                    {
                        descriptionText.text += Body(desc.ToString());
                    }
                }

                catch (System.InvalidOperationException)
                {
                    descriptionText.text = listItem["desc"].ToString();
                }
                attributes.Add("Races", string.IsNullOrWhiteSpace(raceText) ? "None" : raceText);
                attributes.Add("Subraces", string.IsNullOrWhiteSpace(subraceText) ? "None" : subraceText);

            }
        }
        else if (category == "Class")
        {
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            quickRefObj.SetActive(true);
            descriptionPanel.SetActive(true);
            attributesPanel.SetActive(true);
            titleText.text = listItem["name"].ToString();
            descriptionText.text += listItem["desc"].ToString() + "\n\n";
            descriptionText.text += Title("Proficiencies");
            string proficiencyText = "";
            int i = 0;
            foreach (var proficiency in listItem["proficiencies"].EnumerateArray())
            {
                if (i > 0)
                {
                    proficiencyText += "; ";
                }

                proficiency.TryGetProperty("name", out extractedJsonValue);
                proficiencyText += extractedJsonValue.ToString();
                i++;
            }
            descriptionText.text += Body("\n" + proficiencyText);


            foreach (var choice in listItem["proficiency_choices"].EnumerateArray())
            {
                i = 0;
                proficiencyText = "";
                choice.TryGetProperty("choose", out extractedJsonValue);
                string numberToChoose = extractedJsonValue.ToString();
                descriptionText.text += Subtitle("Choose " + numberToChoose + " from: ");
                choice.TryGetProperty("from", out extractedJsonValue);
                foreach (var proficiency in extractedJsonValue.EnumerateArray())
                {
                    if (i > 0)
                    {
                        proficiencyText += "; ";
                    }

                    proficiencyText += proficiency.GetProperty("name").ToString().Replace("Skill: ", "");
                    i++;
                }
                descriptionText.text += Body(proficiencyText);
                proficiencyText += "\n";
            }

            string subclassText = "";
            i = 0;
            foreach (var subclass in listItem["subclasses"].EnumerateArray())
            {
                if (i > 0)
                {
                    subclassText += ", ";
                }

                subclass.TryGetProperty("name", out extractedJsonValue);
                subclassText += extractedJsonValue.ToString();
            }
            attributes.Add("Subclasses", subclassText);

            string savingThrowText = "";
            i = 0;
            foreach (var savingThrow in listItem["saving_throws"].EnumerateArray())
            {
                if (i > 0)
                {
                    savingThrowText += ", ";
                }

                savingThrow.TryGetProperty("name", out extractedJsonValue);
                savingThrowText += extractedJsonValue.ToString();
                i++;
            }

            attributes.Add("Proficient Saving Throws", savingThrowText);

            string hitDieText = listItem["hit_die"].ToString();
            attributes.Add("Hit Die", "d" + hitDieText);

            descriptionText.text += Title("Starting Equipment");
            Dictionary<string, dynamic> equipmentListItem = LoadedFilesData.qrdFiles["StartingEquipment"][item];
            foreach (var equipment in equipmentListItem["starting_equipment"].EnumerateArray())
            {
                equipment.TryGetProperty("equipment", out extractedJsonValue);
                var equipmentName = extractedJsonValue.GetProperty("name");
                equipment.TryGetProperty("quantity", out extractedJsonValue);
                string quantity = extractedJsonValue.ToString();
                descriptionText.text += Body(equipmentName.ToString() + " x " + quantity);
            }

        }
        else if (category == "Ability-Score")
        {
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];

            quickRefObj.SetActive(true);
            descriptionPanel.SetActive(true);
            titleText.text = listItem["name"].ToString();

            string descText = "";
            foreach (var descriptionPart in listItem["desc"].EnumerateArray())
            {
                descText += descriptionPart.ToString() + "\n\n";
            }
            descriptionText.text += Body(descText);
            descriptionText.text += Subtitle("Skills");

            string skillText = "";
            int i = 0;
            foreach (var skill in listItem["skills"].EnumerateArray())
            {
                if (i > 0)
                {
                    skillText += ", ";
                }

                skill.TryGetProperty("name", out extractedJsonValue);
                skillText += extractedJsonValue.ToString();
                i++;
            }
            descriptionText.text += Body(skillText);
        }
        else if (category == "Race")
        {
            quickRefObj.SetActive(true);
            Dictionary<string, dynamic> listItem = LoadedFilesData.qrdFiles[category][item];
            System.Text.Json.JsonElement extractedJsonElement2;

            string raceName = listItem["name"].ToString();
            titleText.text = raceName;

            string speed = listItem["speed"].ToString();
            attributes.Add("Speed", speed + "ft");

            string size = listItem["size"].ToString();
            attributes.Add("Size", size);

            string formattedDescText = "";

            formattedDescText += Title("Age");
            formattedDescText += Body(listItem["age"].ToString());

            formattedDescText += Title("Alignment");
            formattedDescText += Body(listItem["alignment"].ToString());




            try
            {
                foreach (var desc in listItem["ability_bonuses"].EnumerateArray())
                {
                    desc.TryGetProperty("name", out extractedJsonValue);
                    string name = extractedJsonValue.ToString();

                    desc.TryGetProperty("bonus", out extractedJsonValue);
                    string bonus = extractedJsonValue.ToString();

                    attributes.Add(name, "+" + bonus);
                }
            }
            catch (System.InvalidOperationException) { }

            formattedDescText += Title("Languages");
            formattedDescText += Body(listItem["language_desc"].ToString());

            listItem["language_options"].TryGetProperty("choose", out extractedJsonValue);
            string number = extractedJsonValue.ToString();
            int i = 0;
            if (!string.IsNullOrEmpty(number))
            {
                formattedDescText += Title("Language Choice");
                formattedDescText += Subtitle("Choose " + number + " from: ");
                listItem["language_options"].TryGetProperty("from", out extractedJsonValue);

                string languageChoices = "";
                i = 0;
                foreach (var languageOption in extractedJsonValue.EnumerateArray())
                {
                    languageOption.TryGetProperty("name", out extractedJsonElement2);
                    string languageName = extractedJsonElement2.ToString();
                    if (i > 0) languageChoices += ", ";
                    languageChoices += languageName;
                    i++;
                }
                formattedDescText += Body(languageChoices);
            }
            
            string proficiencies = "";
            i = 0;
            foreach (var proficiency in listItem["starting_proficiencies"].EnumerateArray())
            {
                if(i == 0) formattedDescText += Title("Proficiencies");
                print(proficiency.ToString());
                proficiency.TryGetProperty("name", out extractedJsonElement2);
                string languageName = extractedJsonElement2.ToString();
                if (i > 0) proficiencies += ", ";
                proficiencies += languageName;
                i++;
            }
            if(!string.IsNullOrWhiteSpace(proficiencies)) formattedDescText += Body(proficiencies);

            listItem["starting_proficiency_options"].TryGetProperty("choose", out extractedJsonValue);
            number = extractedJsonValue.ToString();
            i = 0;
            if (!string.IsNullOrEmpty(number))
            {
                formattedDescText += Title("Proficiency Choice");
                formattedDescText += Subtitle("Choose " + number + " from: ");
                listItem["starting_proficiency_options"].TryGetProperty("from", out extractedJsonValue);

                string proficencyChoices = "";
                i = 0;
                foreach (var proficiencyOption in extractedJsonValue.EnumerateArray())
                {
                    proficiencyOption.TryGetProperty("name", out extractedJsonElement2);
                    string proficiencyName = extractedJsonElement2.ToString();
                    if (i > 0) proficencyChoices += ", ";
                    proficencyChoices += proficiencyName.Replace("Skill: ", "");
                    i++;
                }
                formattedDescText += Body(proficencyChoices);
            }

            i = 0;
            string subraceText = "";
            foreach(var subrace in listItem["subraces"].EnumerateArray())
            {
                if (i > 0) subraceText += ", ";
                subrace.TryGetProperty("name", out extractedJsonValue);
                subraceText += extractedJsonValue.ToString();
            }
            if(!string.IsNullOrWhiteSpace(subraceText))
            {
                formattedDescText += Title("Subraces");
                formattedDescText += Body(subraceText);
            }


            string traits = "";
            i = 0;
            foreach (var trait in listItem["traits"].EnumerateArray())
            {
                if (i == 0) formattedDescText += Title("Traits");
                trait.TryGetProperty("name", out extractedJsonElement2);
                string traitName = extractedJsonElement2.ToString();
                if (i > 0) traits += ", ";
                traits += traitName;
                i++;
            }
            if(!string.IsNullOrWhiteSpace(traits)) formattedDescText += Body(traits);

            if(listItem.ContainsKey("trait_options"))
            {
                listItem["trait_options"].TryGetProperty("choose", out extractedJsonValue);
                number = extractedJsonValue.ToString();
                listItem["trait_options"].TryGetProperty("type", out extractedJsonValue);
                string traitType = extractedJsonValue.ToString();
                i = 0;
                if (!string.IsNullOrEmpty(number))
                {
                    formattedDescText += Title("Trait Choice");
                    formattedDescText += Subtitle("Choose " + number + " " + traitType + " from: ");
                    listItem["trait_options"].TryGetProperty("from", out extractedJsonValue);

                    string traitChoices = "";
                    i = 0;
                    foreach (var traitOption in extractedJsonValue.EnumerateArray())
                    {
                        traitOption.TryGetProperty("name", out extractedJsonElement2);
                        string proficiencyName = extractedJsonElement2.ToString();
                        if (i > 0) traitChoices += ", ";
                        traitChoices += proficiencyName;
                        i++;
                    }
                    formattedDescText += Body(traitChoices);
                }
            }
            

            string abiliity_bonuses = "";
            i = 0;
            foreach (var bonus in listItem["ability_bonuses"].EnumerateArray())
            {
                if (i == 0) formattedDescText += Title("Ability Bonuses");
                print(bonus.ToString());
                bonus.TryGetProperty("name", out extractedJsonElement2);
                string bonusName = extractedJsonElement2.ToString();
                if (i > 0) abiliity_bonuses += ", ";
                abiliity_bonuses += bonusName;
                i++;
            }
            if (!string.IsNullOrWhiteSpace(abiliity_bonuses)) formattedDescText += Body(abiliity_bonuses);

            listItem["ability_bonus_options"].TryGetProperty("choose", out extractedJsonValue);
            number = extractedJsonValue.ToString();
            i = 0;
            if (!string.IsNullOrEmpty(number))
            {
                formattedDescText += Title("Ability Bonus Choice");
                formattedDescText += Subtitle("Choose " + number + " from: ");
                listItem["ability_bonus_options"].TryGetProperty("from", out extractedJsonValue);

                string abilityBonusChoices = "";
                i = 0;
                foreach (var bonusChoice in extractedJsonValue.EnumerateArray())
                {
                    bonusChoice.TryGetProperty("name", out extractedJsonElement2);
                    string proficiencyName = extractedJsonElement2.ToString();
                    if (i > 0) abilityBonusChoices += ", ";
                    abilityBonusChoices += proficiencyName;
                    i++;
                }
                formattedDescText += Body(abilityBonusChoices);
            }

            //ability bonus + options

            formattedDescText += Title("Size");
            formattedDescText += Body(listItem["size_description"].ToString());

            descriptionText.text = formattedDescText;
        }
        else
        {
            quickRefObj.SetActive(false);
            mac.currentMenuState = MainAppController.MenuState.quickReference;
        }
        foreach (KeyValuePair<string, string> attr in attributes)
        {
            CreateAttributeItem(attr.Key, attr.Value);
        }
        string unformattedDescText = descriptionText.text;
        Regex rx = new(@"(\d)*d(\d)+( )*(\+)*( )*(\d)*");       //matches xdx + x \ xdx+x: 5d8, 12d100, 4d10, 2d6, 3d10+5, 4d4 + 2, etc
        MatchCollection matches = rx.Matches(unformattedDescText);
        foreach (var match in matches)
        {
            unformattedDescText = unformattedDescText.Replace(match.ToString(), "<b><u>" + match.ToString() + "</b></u>");
        }
        rx = new Regex(@"(\d)*d*(\d)+( |-)(feet|foot|mile)(( |-)(cube|radius|sphere|line|cone|cylinder))*");      //matches x( |-)(foot|feet)( |-)(y): 150 feet cube, 10-mile sphere, 5 foot sphere, 1543-mile line, 20-foot-radius, 5-feet-sphere, etc
        matches = rx.Matches(unformattedDescText);
        foreach (var match in matches)
        {
            unformattedDescText = unformattedDescText.Replace(match.ToString(), "<b><u>" + match.ToString() + "</b></u>");
        }
        rx = new Regex(@"DC (\d)+");      //matches DC x: DC 10, DC 4, DC 14, etc
        matches = rx.Matches(unformattedDescText);
        foreach (var match in matches)
        {
            unformattedDescText = unformattedDescText.Replace(match.ToString(), "<b><u>" + match.ToString() + "</b></u>");
        }
        rx = new Regex(@"AC (\d)+");      //matches AC x: AC 10, AC 4, AC 14, etc
        matches = rx.Matches(unformattedDescText);
        foreach (var match in matches)
        {
            unformattedDescText = unformattedDescText.Replace(match.ToString(), "<b><u>" + match.ToString() + "</b></u>");
        }
        rx = new Regex(@"(\d)+( |-)(acid|bludgeoning|cold|fire|force|lightning|necrotic|piercing|poison|psychic|radiant|slashing|thunder)( |-)damage");      //matches x (type)( |-)damage: 5 slashing damage, 12 fire-damage, 13-bludgeoning damage, etc
        matches = rx.Matches(unformattedDescText);
        foreach (var match in matches)
        {
            unformattedDescText = unformattedDescText.Replace(match.ToString(), "<b><u>" + match.ToString() + "</b></u>");
        }
        descriptionText.text = unformattedDescText;



    }

    private void CreateAttributeItem(string title, string detail)
    {
        GameObject go = Instantiate(Prefabs.quickRefAttributePrefab, attributesPanel.transform);
        QuickRefAttributePrefab qrap = go.GetComponent<QuickRefAttributePrefab>();
        qrap.Title = title;
        qrap.Detail = detail;
    }

    private void DestroyAllAttributeItems()
    {
        foreach (QuickRefAttributePrefab qrap in attributesPanel.GetComponentsInChildren<QuickRefAttributePrefab>())
        {
            Destroy(qrap.gameObject);
        }
    }

    private string Capitalize(string toCaps)
    {
        return toCaps.Substring(0, 1).ToUpper() + toCaps.Substring(1, toCaps.Length - 1);
    }

    private string Capitalize(string[] toCaps)
    {
        string ret = "";
        foreach (string s in toCaps)
        {
            ret += Capitalize(s) + " ";
        }
        return ret;
    }

    private string Margin(string toIndent, int amount)
    {
        return string.Format("<margin={0}%>", amount) + toIndent + "</margin>";
    }

    private string Title(string titleString)
    {
        return "<b><u><line-height=2000%><size=20>" + titleString + "</b></u></line-height></size>\n";
    }

    private string Subtitle(string subtitleString)
    {
        int subtitleMarginAmount = 2;
        return Margin("<b><size=16>" + subtitleString + "</size></b>", subtitleMarginAmount) + "\n";
    }

    private string Body(string bodyString)
    {
        int bodyMarginAmount = 4;
        return Margin(bodyString, bodyMarginAmount) + "\n";
    }
}

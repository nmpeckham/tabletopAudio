using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Holds data for colors and images
internal static class ResourceManager
{
    internal static Color red = Color.red;
    internal static Color green = Color.green;
    internal static Color black = Color.black;
    internal static Color orange = new Color(1, 0.56f, 0, 1);
    internal static Color grey = new Color(0.878f, 0.878f, 0.878f, 1f);
    internal static Color musicButtonGrey = new Color(0.443f, 0.443f, 0.443f, 1f);

    internal static Color lightModeGrey = new Color(200 / 255f, 200 / 255f, 200 / 255f);
    internal static Color darkModeGrey = new Color(38 / 255f, 38 / 255f, 38 / 255f);
    internal static Color sfxButtonLight = new Color(224 / 255f, 224 / 255f, 224 / 255f);
    internal static Color sfxButtonDark = new Color(100 / 255f, 100 / 255f, 100 / 255f);
    internal static Color sfxPageBG = new Color(149 / 255f, 149 / 255f, 149 / 255f);

    //internal static Color 

    internal static string[] dbFiles = 
        {
            "Ability-Score.json",
            "Class.json",
            "Condition.json",
            "Damage-Type.json",
            "Equipment.json",
            "Equipment-Category.json",
            "Feature.json",
            "Language.json",
            "Level.json",
            "Magic-Item.json",
            "Magic-School.json",
            "Monster.json",
            "Proficiency.json",
            "Race.json",
            "Rule.json",
            "Rule-Section.json",
            "Skill.json",
            "Spellcasting.json",
            "Spell.json",
            "StartingEquipment.json",
            "Subclass.json",
            "Subrace.json",
            "Trait.json",
            "Weapon-Property.json",
        };

    internal static Dictionary<string, Color> categoryColors = new Dictionary<string, Color>();

    internal static readonly List<uint> kellysMaxContrastSet = new List<uint>
{
    0xFFFFB300, //Vivid Yellow
    0xFF803E75, //Strong Purple
    0xFFFF6800, //Vivid Orange
    0xFFA6BDD7, //Very Light Blue
    0xFFC10020, //Vivid Red
    0xFFCEA262, //Grayish Yellow
    0xFF817066, //Medium Gray
    0xFFC0C0C0, //Gray
    0xFF000000, //Black
    0xFF007D34, //Vivid Green
    0xFFF6768E, //Strong Purplish Pink
    0xFF00538A, //Strong Blue
    0xFFFF7A5C, //Strong Yellowish Pink
    0xFF53377A, //Strong Violet
    0xFFFF8E00, //Vivid Orange Yellow
    0xFFB32851, //Strong Purplish Red
    0xFFF4C800, //Vivid Greenish Yellow
    0xFF7F180D, //Strong Reddish Brown
    0xFF93AA00, //Vivid Yellowish Green
    0xFF593315, //Deep Yellowish Brown
    0xFFF13A13, //Vivid Reddish Orange
    0xFF232C16, //Dark Olive Green
    0xFFCC0007, //Medium Red
    0xFF99FF33  //Lime green
};
}

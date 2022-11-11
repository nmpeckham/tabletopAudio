using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

//Holds data for colors and images
internal static class ResourceManager
{
    internal static Sprite infoImage;
    internal static Sprite warningImage;
    internal static Sprite errorImage;

    internal static Sprite upFolderImage;

    internal static Color red = Color.red;
    internal static Color green = new(0.1f, 1f, 0.1f);
    internal static Color black = Color.black;
    internal static Color orange = new(1, 0.56f, 0, 1);
    internal static Color grey = new(0.878f, 0.878f, 0.878f, 1f);
    internal static Color musicButtonGrey = new(0.294f, 0.294f, 0.294f, 0.69f);

    internal static Color lightModeGrey = new(200 / 255f, 200 / 255f, 200 / 255f);
    internal static Color darkModeGrey = new(36 / 255f, 36 / 255f, 36 / 255f);
    internal static Color sfxButtonLight = new(224 / 255f, 224 / 255f, 224 / 255f);
    internal static Color sfxButtonDark = new(100 / 255f, 100 / 255f, 100 / 255f);
    internal static Color sfxPageBG = new(149 / 255f, 149 / 255f, 149 / 255f);

    internal static Color tabInactiveColor = new(120 / 255f, 120 / 255f, 120 / 255f);
    internal static Color tabActiveColor = new(200 / 255f, 200 / 255f, 200 / 255f);

    internal static List<char> charTable = new();


    //internal static Color 

    internal static string[] dbFiles;

    internal static Dictionary<string, Color> categoryColors = new();

    internal static readonly List<uint> kellysMaxContrastSet = new()
    {
        0xFFFFB300, //Vivid Yellow
        0xFF803E75, //Strong Purple
        0xFFFF6800, //Vivid Orange
        0xFFA6BDD7, //Very Light Blue
        0xFFC10020, //Vivid Red
        0xFFCEA262, //Grayish Yellow
        0xFF817066, //Medium Gray
        0xFFC0C0C0, //Gray
        //0xFF000000, //Black
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

    internal static void Init()
    {
        dbFiles = Directory.GetFiles(Application.streamingAssetsPath).Where(a => Path.GetExtension(a) == ".json").ToArray();

        // For detecting unsupported characters in track names
        TMP_FontAsset englishAsset = Resources.Load<TMP_FontAsset>("TextMesh Pro/Fonts/Lato-Regular SDF");
        englishAsset.characterTable.ForEach(item => charTable.Add(Convert.ToChar(item.unicode)));

        errorImage = Resources.Load<Sprite>("Button Icons/baseline_error_white_48dp");
        infoImage = Resources.Load<Sprite>("Button Icons/baseline_info_white_48dp");
        warningImage = Resources.Load<Sprite>("Button Icons/baseline_warning_white_48dp");

        upFolderImage = Resources.Load<Sprite>("Button Icons/baseline_folder_upload_white_48dp");
    }
}

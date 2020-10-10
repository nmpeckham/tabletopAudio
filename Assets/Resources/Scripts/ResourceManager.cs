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
}

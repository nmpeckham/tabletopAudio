using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGizmos : MonoBehaviour
{
    // Allows changing of dark mode in Unity editor without entering play mode
#if UNITY_EDITOR
    public bool isEnabled = false;
    public bool darkModeChanged = false;
    private void OnDrawGizmos()
    {
        if(isEnabled)
        {
            DarkModeController dmc = GetComponent<DarkModeController>();
            dmc.SwapDarkMode(darkModeChanged);
        }

    }
#endif
}

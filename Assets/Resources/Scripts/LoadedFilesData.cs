﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Stores data about loaded music files and sfx files
public static class LoadedFilesData
{
    public static List<string> musicClips = new List<string>();
    public static List<string> deletedMusicClips = new List<string>();
    public static Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();

    // Category, item name, item attributes
    public static Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>> qrdFiles = new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>();
}

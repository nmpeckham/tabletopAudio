using System.Collections.Generic;
using UnityEngine;

//Stores data about loaded music files and sfx files
public static class LoadedFilesData
{
    public static List<Song> songs = new();
    public static List<string> deletedMusicClips = new();
    public static Dictionary<string, AudioClip> sfxClips = new();

    // Category, item name, item attributes
    public static Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>> qrdFiles = new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>();
}

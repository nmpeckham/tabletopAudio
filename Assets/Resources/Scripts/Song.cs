using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Song
{
    private string fileName;
    internal string sortName;
    internal string artist = null;
    internal string title = null;
    internal TimeSpan duration = TimeSpan.Zero;

    internal Song(string _filename, string _title, TimeSpan _duration, string _artist = null)
    {
        this.FileName = _filename;
        this.sortName = _filename;
        this.title = _title;
        this.duration = _duration;
        this.artist = _artist;
    }

    public string FileName
    {
        get { return fileName; }
        set { fileName = value; }
    }
}

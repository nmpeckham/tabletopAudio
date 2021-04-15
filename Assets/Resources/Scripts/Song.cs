using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.IO;

public class Song
{
    private string fileName;
    internal string SortName {
        get { return sortName; }
    }
    private string sortName = "";
    internal string artist = null;
    internal string title = null;
    internal TimeSpan duration = TimeSpan.Zero;

    internal Song(string _filename, string _title, TimeSpan _duration, string _artist = null)
    {
        this.FileName = _filename;
        this.title = Sanitize(_title);
        this.duration = _duration;
        this.artist = Sanitize(_artist);

        if (!String.IsNullOrEmpty(this.artist))
        {
            this.sortName = this.artist + " - " + this.title;
        }
        else
        {
            this.sortName = this.title;
        }
    }

    public string FileName
    {
        get { return fileName; }
        set { fileName = value; }
    }

    private string Sanitize(string s)
    {
        if (s != null)
        {
            string[] unwanted = { "\0", "\n", "\r" };
            string cleanString = s;
            cleanString = cleanString.Replace(".mp3", "").Replace(".ogg", "");

            foreach (string c in unwanted)
            {
                cleanString = cleanString.Replace(c, "");
            }
            //regex to remove starting song numbers
            Match match = Regex.Match(s, @"\d{1,} *\.*-* *");
            if(!String.IsNullOrEmpty(match.ToString()))
            {
                cleanString = cleanString.Replace(match.ToString(), "");
            }
            //cleanString += (artist + title).GetHashCode();
            return cleanString;
        }
        else return null;
    }
}

using System;
using System.Text.RegularExpressions;

public class Song
{
    private string fileName;
    internal string includingFolder;
    internal string SortName => sortName;
    private readonly string sortName;
    internal string artist;
    internal string title;
    internal TimeSpan duration = TimeSpan.Zero;

    internal Song(string _filename, string _title, TimeSpan _duration, string _includingFolder, string _artist = null)
    {
        FileName = _filename;
        title = Sanitize(_title);
        duration = _duration;
        artist = Sanitize(_artist);
        includingFolder = _includingFolder;

        if (!String.IsNullOrEmpty(artist))
        {
            sortName = artist + " - " + title;
        }
        else
        {
            sortName = title;
        }
    }

    public string FileName
    {
        get => fileName;
        set => fileName = value;
    }

    private string Sanitize(string s)
    {
        //TODO: Removes leading numbers from 3 AM - Matchbox 20.
        if (s != null)
        {
            string[] unwanted = { "\0", "\n", "\r", "\t" };
            string cleanString = s;
            cleanString = cleanString.Replace(".mp3", "").Replace(".ogg", "");

            foreach (string c in unwanted)
            {
                cleanString = cleanString.Replace(c, "");
            }
            //regex to remove starting song numbers
            Match match = Regex.Match(s, @"^\d{2}[ _\-\.]+");
            if (!String.IsNullOrEmpty(match.ToString()))
            {
                cleanString = cleanString.Replace(match.ToString(), "");
            }
            return cleanString;
        }
        else
        {
            return null;
        }
    }
}

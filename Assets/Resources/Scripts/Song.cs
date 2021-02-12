using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Song
{
    private string fileName;
    internal string sortName;

    internal Song(string _filename)
    {
        this.FileName = _filename;
        this.sortName = _filename;
    }

    public string FileName
    {
        get { return fileName; }
        set { fileName = value; }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

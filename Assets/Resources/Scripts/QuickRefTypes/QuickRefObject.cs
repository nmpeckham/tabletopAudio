using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class QuickRefObject
{
    public List<Dictionary<string, dynamic>> contents { get; set; }
    public QuickRefObject()
    {
        contents = new List<Dictionary<string, dynamic>>();
    }
    
}

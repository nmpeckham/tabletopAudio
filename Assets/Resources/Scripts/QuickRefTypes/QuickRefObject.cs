using System.Collections.Generic;

[System.Serializable]
public class QuickRefObject
{
    public List<Dictionary<string, dynamic>> Contents { get; set; }
    public QuickRefObject()
    {
        Contents = new List<Dictionary<string, dynamic>>();
    }

}

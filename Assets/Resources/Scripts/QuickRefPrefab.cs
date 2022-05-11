using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuickRefPrefab : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text categoryText;
    public Button thisButton;
    public Image categoryColor;

    private string index;
    private string category;
    private string description;
    private string title;
    private QuickRefDetailView qrd;

    public string Category
    {
        get => category;
        set
        {
            category = value;
            categoryText.text = category;
            string categoryFileName = category.Replace(" ", "-");
            //print(categoryFileName);
            //foreach(var a in ResourceManager.categoryColors)
            //{
            //    print(a);
            //}
            categoryColor.color = ResourceManager.categoryColors[categoryFileName];
        }
    }
    public string Index
    {
        get => index;
        set => index = value;
    }
    public string Description
    {
        get => description;
        set
        {
            description = value;
            descriptionText.text = description;
        }
    }
    public string Title
    {
        get => title;
        set
        {
            title = value;
            titleText.text = title;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        qrd = Camera.main.GetComponent<QuickRefDetailView>();
        thisButton.onClick.AddListener(Clicked);
    }

    internal void Clicked()
    {
        string itemCategory = category.Replace(" ", "-");
        qrd.ItemSelected(itemCategory, index);
        print("clicked!!");
    }
}

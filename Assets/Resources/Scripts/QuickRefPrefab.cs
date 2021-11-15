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

    private string category;
    private string description;
    private string title;
    private QuickRefDetailView qrd;

    public string Category
    {
        get { return category; }
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
    public string Description
    {
        get { return description; }
        set
        {
            description = value;
            descriptionText.text = description;
        }
    }
    public string Title
    {
        get { return title; }
        set
        {
            title = value;
            titleText.text = title;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        qrd = Camera.main.GetComponent<QuickRefDetailView>();
        thisButton.onClick.AddListener(Clicked);
    }

    private void Clicked()
    {
        string itemCategory = category.Replace(" ", "-");
        string itemId = title.ToLower().Replace(" ", "-").Replace(",", "").Replace("/", "-").Replace("(", "").Replace(")", "").Replace(":", "").Replace("'", "");
        if (itemCategory == "Monster")
        {
            itemId = title;
        }

        qrd.ItemSelected(itemCategory, itemId);
    }
}

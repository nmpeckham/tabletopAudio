using UnityEngine;
using UnityEngine.UI;

public class OpenURL : MonoBehaviour
{
    public string url;

    // Start is called before the first frame update
    private void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(Open);
    }

    private void Open()
    {
        Application.OpenURL(url);
    }
}

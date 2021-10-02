using UnityEngine;
using UnityEngine.UI;

//Class for Play, Pause, Stop, Previous, Next, and Shuffle Buttons
public class AudioControlButton : MonoBehaviour
{
    public string id;
    private Button thisButton;
    MainAppController mac;
    // Start is called before the first frame update
    void Start()
    {
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(Clicked);
        mac = Camera.main.GetComponent<MainAppController>();
    }

    void Clicked()
    {
        mac.ControlButtonClicked(id);
    }
}

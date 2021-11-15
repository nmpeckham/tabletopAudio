using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuSlider : MonoBehaviour
{
    // Start is called before the first frame update
    public string id = "";
    private Slider thisSlider;
    private OptionsMenuController omc;
    void Start()
    {
        omc = Camera.main.GetComponent<OptionsMenuController>();
        thisSlider = GetComponent<Slider>();
        thisSlider.onValueChanged.AddListener(ValueChanged);
    }

    private void ValueChanged(float val)
    {
        omc.OptionMenuSliderChanged(id, val);
    }
}

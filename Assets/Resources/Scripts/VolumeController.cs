using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Applies volume changes across the app
public class VolumeController : MonoBehaviour
{
    public Slider volumeSlider;
    public TMP_Text volumeLabel;
    private MainAppController mac;
    private MusicController mc;

    // Start is called before the first frame update
    void Start()
    {
        mac = Camera.main.GetComponent<MainAppController>();
        volumeSlider.onValueChanged.AddListener(VolumeChanged);
        mc = Camera.main.GetComponent<MusicController>();
    }

    internal void VolumeChanged(float volume)
    {
        if (volumeSlider.value != volume) volumeSlider.value = volume;
        foreach (GameObject obj in mac.SFXButtons[mac.activePage])
        {
            obj.GetComponent<SFXButton>().ChangeMasterVolume(volume);
            volumeLabel.text = (volume * 100).ToString("N0");
        }
        mc.ChangeMasterVolume(volume);
    }
}

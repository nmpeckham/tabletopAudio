using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using Extensions;

//Applies volume changes across the app
public class VolumeController : MonoBehaviour
{
    public  Slider volumeSlider;
    public  TMP_Text volumeLabel;
    private MainAppController mac;
    private MusicController mc;
    public  AudioMixerGroup AMG;
    private float masterVolume = 1;
    public float MasterVolume
    {
        get
        {
            return masterVolume;
        }
        set
        {
            masterVolume = value;
            AMG.audioMixer.SetFloat("MasterVolume", masterVolume.ToDB());
            if (volumeSlider.value != masterVolume) volumeSlider.value = masterVolume;
            volumeLabel.text = (masterVolume * 100).ToString("N0") + "%";
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        mac = Camera.main.GetComponent<MainAppController>();
        volumeSlider.onValueChanged.AddListener(VolumeChanged);
    }

    internal void VolumeChanged(float volume)
    {
        MasterVolume = volume;
    }
}
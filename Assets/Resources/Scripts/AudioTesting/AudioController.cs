using NLayer;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    private AudioSource aSource;
    MpegFile newFile;
    // Start is called before the first frame update
    void Start()
    {
        aSource = Camera.main.GetComponent<AudioSource>();
        newFile = new MpegFile(@"D:\Music\TableTopAudio\music\02 Grey Street.mp3");
        AudioClip clip = AudioClip.Create("test", (int)newFile.Length, newFile.Channels, newFile.SampleRate, true, Callback);
        aSource.clip = clip;
        aSource.Play();
    }

    void Callback(float[] data)
    {
        newFile.ReadSamples(data, 0, data.Length);
    }
}

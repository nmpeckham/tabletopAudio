using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using NLayer;
using NVorbis;
using UnityEngine.UI;
using System;


public class GenerateMusicFFTBackgrounds : MonoBehaviour
{
    private MainAppController mac;
    public GameObject musicButtonParent;

    private Coroutine generatorCoroutine;

    private List<GameObject> buttons = new List<GameObject>();
    // Start is called before the first frame update
    internal void Begin()
    {
        mac = GetComponent<MainAppController>();
        GetSongs();
        generatorCoroutine = StartCoroutine(GenerateBackgrounds());
    }

    void GetSongs()
    {
        buttons.Clear();
        foreach(MusicButton mb in musicButtonParent.GetComponentsInChildren<MusicButton>())
        {
            buttons.Add(mb.gameObject);
        }
    }

    internal void StopGeneration()
    {
        StopCoroutine(generatorCoroutine);
        generatorCoroutine = null;
    }

    private IEnumerator GenerateBackgrounds()
    {
        yield return new WaitForSecondsRealtime(1);
        print("started");
        print(buttons.Count);
        int minBufferSize = 1024;
        foreach (GameObject btn in buttons)
        {
            string filePath = Path.Combine(mac.musicDirectory, LoadedFilesData.songs[btn.GetComponent<MusicButton>().buttonId].FileName);

            string extension = Path.GetExtension(filePath);
            if (extension == ".mp3")
            {
                MpegFile reader = new MpegFile(filePath);
                List<float> samples = new List<float>();
                long samplesRead = 0;
                Texture2D tex = new Texture2D(455, 26);
                float[] buf = new float[minBufferSize];
                int actualRead;
                float rmsEnergy;
                long newPosition;
                for(int i = 1; i  < 456; i++)
                {

                    actualRead = reader.ReadSamples(buf, 0, buf.Length-1);
                    samplesRead += actualRead;
                    rmsEnergy = CalculateRMS(buf);
                    samples.Add(rmsEnergy);
                    if (actualRead == 0 ) break;
                    newPosition = (reader.Length  / 455) * i;
                    reader.Position = newPosition;

                    //catch (ArgumentOutOfRangeException e)
                    //{
                    //    Debug.LogError(e);
                    //    print("out of range");
                    //}
                    //catch (OutOfMemoryException e)
                    //{
                    //    Debug.LogError(e);
                    //    print("out of memory");
                    //}
                    //catch(IndexOutOfRangeException e)
                    //{
                    //    Debug.LogError(e);
                    //    print("index out of range");
                    //}
                    //catch(ArgumentException e)
                    //{
                    //    Debug.LogError(e);
                    //    print("argument exception");
                    //    break;
                    //}
                    for (int x = 0; x < samples.Count; x++)
                    {
                        for (int y = 0; y < (Mathf.Abs(samples[x]) * 1200f + 3f); y++)
                        {
                            tex.SetPixel(x, y, Color.gray);
                        }
                        tex.SetPixel(x, 0, Color.white); // Set the bottom row of pixels white, as a divider line
                    }
                    tex.Apply();
                    btn.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, 455, 26), Vector2.zero);
                    if (samples.Count % 8 == 0) yield return new WaitForEndOfFrame();

                }
            }
            else if (extension == ".ogg")
            {
                VorbisReader reader = new VorbisReader(filePath);
            }
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }
    float CalculateRMS(float[] buffer)
    {
        float sum = 0;
        foreach(float item in buffer)
        {
            sum += Mathf.Pow(item, 2);
        }
        return Mathf.Sqrt(sum) / buffer.Length;
    }
}

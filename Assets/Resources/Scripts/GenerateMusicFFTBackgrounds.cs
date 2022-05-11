using NLayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class GenerateMusicFFTBackgrounds : MonoBehaviour
{
    public GameObject musicButtonParent;

    private Coroutine generatorCoroutine;

    private readonly List<GameObject> buttons = new();
    // Start is called before the first frame update
    internal void Begin()
    {
        GetSongs();
        generatorCoroutine = StartCoroutine(GenerateBackgrounds());
    }

    private void GetSongs()
    {
        buttons.Clear();
        foreach (MusicButton mb in musicButtonParent.GetComponentsInChildren<MusicButton>())
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
        int minBufferSize = 4096;
        foreach (GameObject btn in buttons)
        {
            string filePath = Path.Combine(MainAppController.workingDirectories["musicDirectory"], LoadedFilesData.songs[btn.GetComponent<MusicButton>().buttonId].FileName);

            string extension = Path.GetExtension(filePath);
            if (extension == ".mp3")
            {
                MpegFile reader = new(filePath);
                reader.StereoMode = StereoMode.DownmixToMono;   //Convert to mono
                List<float> samples = new();
                Texture2D tex = new(512, 26);
                float[] buf = new float[minBufferSize];
                float rmsEnergy;
                for (int i = 1; i < 512; i++)
                {
                    //if (i > 456) break;
                    try
                    {
                        reader.ReadSamples(buf, 0, buf.Length);
                        //actualRead = reader.ReadSamples(buf, 0, buf.Length - 1);
                        //samplesRead += actualRead;
                        rmsEnergy = CalculateRMS(buf);
                        samples.Add(rmsEnergy);
                        //if (actualRead == 0) break;
                        //newPosition = (reader.Length / 512) * i;
                        reader.Position += (reader.Length / 512);
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        Debug.LogError(e);
                        print("out of range");
                    }
                    catch (OutOfMemoryException e)
                    {
                        Debug.LogError(e);
                        print("out of memory");
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.LogError(e);
                        print("index out of range");
                    }
                    catch (ArgumentException e)
                    {
                        Debug.LogError(e);
                        print("argument exception");
                        break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        print("unknown exception, be afraid");
                        break;
                    }
                    if (samples.Count % 16 == 0)
                    {
                        yield return null;
                    }
                }
                samples = Normalize(samples);
                for (int x = 0; x < samples.Count; x++)
                {
                    for (int y = 0; y < samples[x] * 26f; y++)
                    {
                        tex.SetPixel(x, y, Color.gray);
                    }
                    tex.SetPixel(x, 0, Color.white); // Set the bottom row of pixels white, as a divider line
                }
                tex.Apply();
                btn.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, 455, 26), Vector2.zero);


            }
            else if (extension == ".ogg")
            {
                //
            }
            yield return null;
        }
        yield return null;
    }

    private List<float> Normalize(List<float> samples)
    {
        float max = samples.Max();
        List<float> normalizedSamples = new();
        foreach (float sample in samples)
        {
            normalizedSamples.Add((sample) / max);
        }
        print(normalizedSamples.Max());
        print(normalizedSamples.Average());
        print("\n");
        return normalizedSamples;
    }

    private float CalculateRMS(float[] samples)
    {
        float sum = 0;
        foreach (float item in samples)
        {
            sum += Mathf.Pow(Mathf.Abs(item), 2f);
        }
        return Mathf.Sqrt(sum / samples.Length);
    }
}

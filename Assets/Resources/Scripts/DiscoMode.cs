using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscoMode : MonoBehaviour
{
    internal bool discoModeActive = false;
    private float currentCooldown = 0;
    internal float cooldown = 15;
    private SFXPageController spc;
    internal static List<Color> colours = new()
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.cyan,
        Color.yellow,
        Color.magenta,
    };
    private readonly List<List<int>> diagonal = new()
    {
        new List<int>
        {
            0
        },
        new List<int>
        {
            1, 7
        },
        new List<int>
        {
            2, 8, 14
        },
        new List<int>
        {
            3, 9, 15, 21
        },
        new List<int>
        {
            4, 10, 16, 22, 28
        },
        new List<int>
        {
            5, 11, 17, 23, 29
        },
        new List<int>
        {
            6, 12, 18, 24, 30
        },
        new List<int>
        {
            13, 19, 25, 31
        },
        new List<int>
        {
            20, 26, 32
        },
        new List<int>
        {
            27, 33
        },
        new List<int>
        {
            34
        }
    };
    private readonly List<List<int>> square = new()
    {
        new List<int>
        {
            0, 1, 2, 3, 4, 5, 6, 13, 20, 27, 34, 33, 32, 31, 30, 29, 28, 21, 14, 7
        },
        new List<int>
        {
            8, 9, 10, 11, 12, 19, 26, 25, 24, 23, 22, 15
        },
        new List<int>
        {
            16, 18
        },
        new List<int>
        {
            17
        }
    };
    private int discoSetting;
    private MainAppController mac;
    private int iteration = 0;

    private enum Settings
    {
        random,
        diagonal,
        square,
        reverseDiagonal,
    }

    private Settings setting = Settings.random;

    // Start is called before the first frame update
    private void Start()
    {
        mac = Camera.main.GetComponent<MainAppController>();
        spc = Camera.main.GetComponent<SFXPageController>();
        StartCoroutine(ChangeSetting());
    }

    // Update is called once per frame
    private void Update()
    {
        currentCooldown--;
    }

    internal void SetDiscoMode(bool val)
    {
        discoModeActive = val;
        StartCoroutine(ToggleDiscoModes());
    }
    internal void ChangeColors()
    {
        if (currentCooldown <= 0 && discoModeActive)
        {
            if (setting == Settings.random)
            {
                foreach (GameObject go in GameObject.FindGameObjectsWithTag("sfxButton"))
                {
                    float rand = UnityEngine.Random.Range(0f, 1f);
                    if (rand > 0.5f)
                    {
                        go.GetComponent<SFXButton>().ChangeColor();
                    }
                }
            }
            else if (setting == Settings.diagonal)
            {
                foreach (int i in diagonal[iteration.Mod(11)])
                {
                    spc.pageParents[SFXPageController.activePage].buttons[i].GetComponent<SFXButton>().ChangeColor();
                }
                iteration++;
            }
            else if (setting == Settings.reverseDiagonal)
            {
                foreach (int i in diagonal[iteration.Mod(11)])
                {
                    spc.pageParents[SFXPageController.activePage].buttons[i].GetComponent<SFXButton>().ChangeColor();
                }
                iteration--;
            }
            else if (setting == Settings.square)
            {
                foreach (int i in square[iteration.Mod(4)])
                {
                    spc.pageParents[SFXPageController.activePage].buttons[i].GetComponent<SFXButton>().ChangeColor();
                }
                iteration++;
            }

            currentCooldown = cooldown;
        }
    }

    private IEnumerator ToggleDiscoModes()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("sfxButton"))
        {
            go.GetComponent<SFXButton>().SetDiscoMode(discoModeActive);
            if (discoModeActive)
            {
                int rand = UnityEngine.Random.Range(0, 5);
                for (int i = 0; i < rand; i++)
                {
                    yield return null;
                }
            }
        }
    }

    private IEnumerator ChangeSetting()
    {
        while (true)
        {
            yield return new WaitForSeconds(30);
            discoSetting++;
            setting = (Settings)(discoSetting % Enum.GetValues(typeof(Settings)).Length);
            if (discoSetting % 5 == 0)
            {
                SFXButton.fadeToBlack = !SFXButton.fadeToBlack;
            }
        }
    }
}

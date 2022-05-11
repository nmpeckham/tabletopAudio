using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Class to take commands from page buttons, including menu and "Stop SFX" buttons
public class PageButton : MonoBehaviour, IPointerDownHandler
{


    public int id;
    private static MainAppController mac;
    private TMP_Text label;
    public Button playAllButton;
    public Button stopAllButton;
    public Button fadeInButton;
    public Button fadeOutButton;
    public Image indicatorImage;
    private static SFXPageController spc;

    [SerializeField]
    private int activeAudioSources = 0;


    public int ActiveAudioSources
    {
        set
        {
            activeAudioSources = value;
            if (activeAudioSources == 0)
            {
                indicatorImage.color = new Color(1, 1, 1, 0);
            }
            else
            {
                indicatorImage.color = Color.white;
            }
        }
        get => activeAudioSources;

    }

    public string Label
    {
        get => label.text;
        set => label.text = value;
    }

    // Start is called before the first frame update
    internal void Init()
    {
        label = GetComponentInChildren<TMP_Text>();
        mac = Camera.main.GetComponent<MainAppController>();
        playAllButton.onClick.AddListener(PlayAll);
        stopAllButton.onClick.AddListener(StopAll);
        fadeInButton.onClick.AddListener(FadeIn);
        fadeOutButton.onClick.AddListener(FadeOut);
        spc = Camera.main.GetComponent<SFXPageController>();

        if (id == -2 && gameObject.transform.GetSiblingIndex() != MainAppController.NUMPAGES + 1)
        {
            gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES + 1);
        }

        //if (id == 0)
        //{
        //    SFXPageController.activePage = this;
        //}
    }

    private void PlayAll()
    {
        foreach (GameObject btn in spc.pageParents[id].buttons)
        {
            SFXButton sfxBtn = btn.GetComponent<SFXButton>();
            if (!sfxBtn.IsPlaying)
            {
                sfxBtn.Play(true);
            }
        }
    }

    private void StopAll()
    {
        foreach (GameObject btn in spc.pageParents[id].buttons)
        {
            btn.GetComponent<SFXButton>().Stop(true);
        }
    }

    private void FadeIn()
    {
        foreach (GameObject btn in spc.pageParents[id].buttons)
        {
            btn.GetComponent<SFXButton>().FadeVolume("in", true);
        }
    }

    private void FadeOut()
    {
        foreach (GameObject btn in spc.pageParents[id].buttons)
        {
            btn.GetComponent<SFXButton>().FadeVolume("out", true);
        }
    }

    internal void RefreshOrder()
    {
        if (id == -2 && gameObject.transform.GetSiblingIndex() != MainAppController.NUMPAGES + 1)
        {
            gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES + 1);
        }

        if (id == -1 && gameObject.transform.GetSiblingIndex() != 0)
        {
            gameObject.transform.SetSiblingIndex(0);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            mac.EditPageLabel(label);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked();
        }
    }

    internal void Clicked()
    {
        if (id >= 0)
        {
            spc.ChangeSFXPage(id);
        }
        else if (id == -2)
        {
            mac.ControlButtonClicked("STOP-SFX");
        }
        else if (id == -1)
        {
            mac.ControlButtonClicked("OPTIONS");
        }
    }
}


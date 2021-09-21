using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

//Class to take commands from page buttons, including menu and "Stop SFX" buttons
public class PageButton : MonoBehaviour, IPointerDownHandler
{
    private static PageButton activeButton;
    public int id;
    private MainAppController mac;
    private TMP_Text label;
    public Button playAllButton;
    public Button stopAllButton;
    public Button fadeInButton;
    public Button fadeOutButton;
    public Image indicatorImage;

    [SerializeField]
    private int activeAudioSources = 0;

    
    public int ActiveAudioSources
    {
        set { 
            activeAudioSources = value; 
            if(activeAudioSources == 0) indicatorImage.color = new Color(1, 1, 1, 0);
            else indicatorImage.color = Color.white;
        }
        get
        {
            return activeAudioSources;
        }
        
    }

    public string Label
    {
        get 
        {
            return label.text;
        }
        set
        {
            label.text = value;
        }
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

        if (id == -2 && gameObject.transform.GetSiblingIndex() != MainAppController.NUMPAGES + 1) gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES + 1);
        if (id == 0) activeButton = this;
    }

    void PlayAll()
    {
        foreach(GameObject btn in mac.pageParents[id].buttons)
        {
            SFXButton sfxBtn = btn.GetComponent<SFXButton>();
            if (!sfxBtn.isPlaying) sfxBtn.Play(true);
        }
    }

    void StopAll()
    {
        foreach (GameObject btn in mac.pageParents[id].buttons)
        {
            btn.GetComponent<SFXButton>().Stop(true);
        }
    }

    void FadeIn()
    {
        foreach (GameObject btn in mac.pageParents[id].buttons)
        {
            btn.GetComponent<SFXButton>().FadeVolume("in", true);
        }
    }

    void FadeOut()
    {
        foreach (GameObject btn in mac.pageParents[id].buttons)
        {
            btn.GetComponent<SFXButton>().FadeVolume("out", true);
        }
    }

    internal void RefreshOrder()
    {
        if (id == -2 && gameObject.transform.GetSiblingIndex() != MainAppController.NUMPAGES + 1) gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES + 1);
        if (id == -1 && gameObject.transform.GetSiblingIndex() != 0) gameObject.transform.SetSiblingIndex(0);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            mac.EditPageLabel(label);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (id >= 0)
            {
                activeButton.GetComponent<Image>().color = mac.darkModeEnabled ? ResourceManager.darkModeGrey : Color.white;
                activeButton = this;
                mac.ChangeSFXPage(id);
                GetComponent<Image>().color = ResourceManager.red;
            }
            else if (id == -2) mac.ControlButtonClicked("STOP-SFX");
            else if (id == -1) mac.ControlButtonClicked("OPTIONS");
        }
    }
}
      

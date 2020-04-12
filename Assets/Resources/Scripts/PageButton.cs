using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

//Class to take commands from page buttons, including menu and "Stop SFX" buttons
public class PageButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int id;
    private Button thisButton;
    private MainAppController mac;
    private TMP_Text label;
    private bool hasPointer = false;
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
    void Start()
    {
        label = GetComponentInChildren<TMP_Text>();
        mac = Camera.main.GetComponent<MainAppController>();
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(Clicked);
        playAllButton.onClick.AddListener(PlayAll);
        stopAllButton.onClick.AddListener(StopAll);
        fadeInButton.onClick.AddListener(FadeIn);
        fadeOutButton.onClick.AddListener(FadeOut);

        if (id == -2 && gameObject.transform.GetSiblingIndex() != MainAppController.NUMPAGES + 1) gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES + 1);
    }

    void PlayAll()
    {
        foreach(GameObject btn in mac.sfxButtons[id])
        {
            SFXButton sfxBtn = btn.GetComponent<SFXButton>();
            if(!sfxBtn.isPlaying) sfxBtn.Play();
        }
    }

    void StopAll()
    {
        foreach (GameObject btn in mac.sfxButtons[id])
        {
            btn.GetComponent<SFXButton>().Stop();
        }
    }

    void FadeIn()
    {
        foreach (GameObject btn in mac.sfxButtons[id])
        {
            btn.GetComponent<SFXButton>().FadeIn();
        }
    }

    void FadeOut()
    {
        foreach (GameObject btn in mac.sfxButtons[id])
        {
            btn.GetComponent<SFXButton>().FadeOut();
        }
    }

    void Clicked()
    {
        if (id >= 0)
        {
            mac.ChangeSFXPage(id);
            GetComponent<Image>().color = ResourceManager.red;
        }
        else if (id == -2) mac.ControlButtonClicked("STOP-SFX");
        else if (id == -1) mac.ControlButtonClicked("OPTIONS");
    }

    void Update()
    {
        // Fixes weird behaviour of page sibling indexes. Are different in builds and editor, and can change randomly
        if (id == -2 && gameObject.transform.GetSiblingIndex() != MainAppController.NUMPAGES + 1) gameObject.transform.SetSiblingIndex(MainAppController.NUMPAGES + 1);
        if(id == -1 && gameObject.transform.GetSiblingIndex() != 0) gameObject.transform.SetSiblingIndex(0);
        if (mac.activePage != id) GetComponent<Image>().color = ResourceManager.transWhite;
        if(hasPointer && Input.GetMouseButtonDown(1) && id >= 0 && id < MainAppController.NUMPAGES)
        {
            //Debug.Log("Clicked " + id);
            mac.EditPageLabel(label);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log(id);
        hasPointer = true;
        //Debug.Log(hasPointer);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hasPointer = false;
    }
}
      

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;

public class RemoteTriggerController : MonoBehaviour
{
    internal Dictionary<string, PlayerData.Player> remotePlayerData;

    private NetworkReceiver receiver;
    public GameObject remoteTriggerPage;
    public GameObject pageParent;

    public Button closeButton;
    public Button exitButton;   //why?

    internal void Init()
    {
        PlayerData pd = new PlayerData();
        remotePlayerData = pd.GeneratePlayerData();
        receiver = GetComponent<NetworkReceiver>();

        receiver.Init();
        remoteTriggerPage.SetActive(true);
    }

    //internal void AssignButtons()
    //{
    //    PlaylistTab remoteTriggerTab = remoteTriggerPage.GetComponent<PlaylistTab>();
    //    remoteTriggerTab.MusicButtons.
    //}

    internal void Close()
    {
        if (receiver != null)
        {
            receiver.Close();
        }
        remoteTriggerPage.SetActive(false);
    }
}

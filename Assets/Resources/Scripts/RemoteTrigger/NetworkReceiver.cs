using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class NetworkReceiver : MonoBehaviour
{
    static UdpClient clientData;
    static int portNumber = 65002;
    static public int receiveBufferSize = 120000;

    static public bool showDebug = false;
    static IPEndPoint ipEndPointData;
    static private object obj = null;
    static private System.AsyncCallback AC;
    static byte[] receivedBytes;

    private RemoteTriggerController rtc;
    private NetworkSender sender;

    private string errorMessage = "";

    private List<ColumnRow> ButtonsToPlay = new List<ColumnRow>();

    Dictionary<string, Dictionary<string, ColumnRow>> buttonMap = new()
    {
        { 
            "ADAM",     new Dictionary<string, ColumnRow>() 
            {
                { 
                    "MORT",     new ColumnRow(0, 0) 
                } 
            } 
        },
        { 
            "DEREK",    new Dictionary<string, ColumnRow>() 
            { 
                { 
                    "GOBBERT",  new ColumnRow(0, 1) 
                } 
            } 
        },
        {
            "STEPHEN",  new Dictionary<string, ColumnRow>() 
            {
                { 
                    "WOLFGANG", new ColumnRow(0, 2) 
                },
                { 
                    "BALUG",    new ColumnRow(0, 3) 
                }
            }
        },
    };

    public void Init()
    {
        InitializeUDPListener();
        rtc = GetComponent<RemoteTriggerController>();
        sender = GetComponent<NetworkSender>();
        StartCoroutine(PlayButtons());

    }

    private void OnApplicationQuit()
    {
        Close();
    }

    internal void Close()
    {
        StopUDPListener();
    }

    static private void StopUDPListener()
    {
        if (clientData != null)
        {
            clientData.Close();
            clientData.Dispose();
        }
    }

    private void InitializeUDPListener()
    {
        ipEndPointData = new IPEndPoint(IPAddress.Any, portNumber);
        clientData = new UdpClient();

        clientData.ExclusiveAddressUse = false;
        clientData.EnableBroadcast = true;
        clientData.Client.Bind(ipEndPointData);

        AC = new System.AsyncCallback(ReceivedUDPPacket);
        clientData.BeginReceive(AC, obj);
        Debug.Log("UDP - Start Receiving..");
    }

    private void ReceivedUDPPacket(System.IAsyncResult result)
    {
        receivedBytes = clientData.EndReceive(result, ref ipEndPointData);

        ParsePacket();

        clientData.BeginReceive(AC, obj);
    }

    private NetworkMessages.BasicMessage ParsePacket()
    {
            try
            {
            Debug.Log(Encoding.UTF8.GetString(receivedBytes));
            var decodedMessage = JsonUtility.FromJson<NetworkMessages.BasicMessage>(Encoding.UTF8.GetString(receivedBytes));

            if (decodedMessage.MessageType == "initialize")
            {
                if (rtc.remotePlayerData.ContainsKey(decodedMessage.PlayerName.ToUpper()))
                {
                    rtc.remotePlayerData[decodedMessage.PlayerName.ToUpper()].localIP = IPAddress.Parse(decodedMessage.IPAddress);
                }
            }
            else if (decodedMessage.MessageType == "syncRequest")
            {
                NetworkMessages.S_PlayerInformationMessage newMessage = new NetworkMessages.S_PlayerInformationMessage();
                newMessage.characters = rtc.remotePlayerData[decodedMessage.PlayerName.ToUpper().Trim()].characters;
                newMessage.MessageType = "characterInformation";
                newMessage.PlayerName = decodedMessage.PlayerName;
                
                string dataToSend = JsonConvert.SerializeObject(newMessage);
                Debug.Log("Data to send: " + dataToSend);
                if (dataToSend != null)
                {
                    print(decodedMessage.MessageType);
                    print(decodedMessage.PlayerName);
                    print(decodedMessage.IPAddress);

                    sender.Init(decodedMessage.IPAddress);
                    sender.SendString(dataToSend);
                    sender.StopUDPSender();
                }
            }
            else if(decodedMessage.MessageType == "requestPlay")
            {
                NetworkMessages.S_RequestPlayMessage rpMessage = JsonUtility.FromJson<NetworkMessages.S_RequestPlayMessage>(Encoding.UTF8.GetString(receivedBytes));
                ColumnRow buttonToPlay = buttonMap[rpMessage.PlayerName.ToUpper()][rpMessage.CharacterName.ToUpper()];

                ButtonsToPlay.Add(buttonToPlay);
                //SFXPage remotePlayTab = null;
                //rtc.remoteTriggerPage.TryGetComponent<SFXPage>(out remotePlayTab);
                //SFXButton tempButton = null;
                //foreach(GameObject mb in remotePlayTab.buttons)
                //{
                //    if(mb.TryGetComponent<SFXButton>(out tempButton))
                //    {
                //        if(tempButton.id == buttonToPlay.row * 7 + buttonToPlay.column) //7 because there are 7 buttons per row
                //        {
                //            tempButton.Clicked();
                //        }
                //    }
                //}



                //print(buttonToPlay);
                //print(rpMessage.PlayerName);
                //print(rpMessage.CharacterName);
                //print(rpMessage.SfxId);
                //RowColumn buttonToPlay = buttonMap[decodedMessage.PlayerName][decodedMessage.Char]
            }
        }
        
        catch (Exception e)
        {
        errorMessage = e.Message.ToString();
        }
        return null;

    }

    IEnumerator PlayButtons()
    {
        while(true)
        {
            foreach(ColumnRow cr in ButtonsToPlay)
            {
                SFXPage remotePlayTab = rtc.pageParent.GetComponent<SFXPage>();
                int buttonId = (cr.row * 7) + cr.column;
                print(buttonId);
                foreach (GameObject mb in remotePlayTab.buttons)
                {
                    if (mb.GetComponent<SFXButton>().id == (cr.row * 7) + cr.column)
                    {
                        mb.GetComponent<SFXButton>().Clicked();
                    }
                }
            }
            ButtonsToPlay.Clear();
            yield return null;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class NetworkSender : MonoBehaviour
{
    private int port;  // define in init
    IPEndPoint remoteEndPoint;
    UdpClient client;

    public void Init(string destinationIp)
    {
        port = 65002;
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(destinationIp), port);
        client = new UdpClient();
    }

    // sendData
    internal void SendString(string message)
    {
        print("sending message: " + message);
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    internal void StopUDPSender()
    {
        if(client != null)
        {
            client.Close();
            client.Dispose();
        }
    }

    private void OnApplicationQuit()
    {
        StopUDPSender();
    }
}

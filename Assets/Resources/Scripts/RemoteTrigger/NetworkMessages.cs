using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMessages
{
    public class BasicMessage
    {
        public string MessageType;
        public string PlayerName;
        public string IPAddress;
    }

    public class S_PlayerInformationMessage : BasicMessage
    {
        public List<PlayerData.Character> characters;
    }

    public class S_RequestPlayMessage : BasicMessage
    {
        public string CharacterName;
        public int SfxId;
    }

}

using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class PlayerData
{
    public class Player
    {
        public List<Character> characters;
        public IPAddress localIP;

        public Player(List<Character> _characters)
        {
            characters = _characters;
        }
    }

    public class Character
    {
        public string characterName;
        public int soundsAllowed;

        public Character(string _characterName, int _soundsAllowed)
        {
            characterName = _characterName;
            soundsAllowed = _soundsAllowed;
        }
    }

    public Dictionary<string, Player> GeneratePlayerData()
    {
        Dictionary<string, Player> playerData = new Dictionary<string, Player>();

        Character char1 = new Character("Balug", 1);
        Character char2 = new Character("Wolfgang", 1);
        List<Character> list = new List<Character>()
        {
            char1, char2
        };
        Player stephenPlayer = new Player(list);
        playerData.Add("STEPHEN", stephenPlayer);

        char1 = new Character("Mort", 1);
        list = new List<Character>()
        {
            char1
        };
        Player adamPlayer = new Player(list);
        playerData.Add("ADAM", adamPlayer);

        char1 = new Character("Gobbert", 1);
        list = new List<Character>()
        {
            char1
        };
        Player derekPlayer = new Player(list);
        playerData.Add("DEREK", derekPlayer);

        return playerData;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerManager
{
    private Dictionary<ulong, Player> _playerDataDict = new Dictionary<ulong, Player>();
    public string name = "PlayerManager";

    // Event, um Änderungen an Spielerinformationen zu melden
    public event Action<ulong, int> OnPlayerScoreUpdated;

    public void AddNewPlayer(ulong clientId)
    {
        Debug.Log("Ich will einen neuen Spieler hinzufügen");

        if (!_playerDataDict.ContainsKey(clientId))
        {
            _playerDataDict[clientId] = new Player(clientId, new List<int>(), "Player " + clientId, 0);
            Debug.Log("Player " + clientId + " hat sich verbunden");
        }
    }

    public Dictionary<ulong, Player> GetPlayerDataDict()
    {
        return _playerDataDict;
    }

    public List<ulong> GetConnectedClientIds()
    {
        return new List<ulong>(_playerDataDict.Keys);
    }

    public Player[] GetAllPlayers()
    {
        List<Player> player = new List<Player>(_playerDataDict.Values);
        return player.ToArray(); // Konvertiert die Liste in ein Array
    }

    public void UpdatePlayerScore(ulong clientId, int addToScore)
    {
        if (_playerDataDict.TryGetValue(clientId, out Player player))
        {
            player.score += addToScore;
            OnPlayerScoreUpdated?.Invoke(clientId, addToScore); // Event auslösen
            Debug.Log($"Score von Spieler {clientId} aktualisiert: {addToScore}");
        }
        else
        {
            Debug.LogWarning($"Spieler mit ID {clientId} nicht gefunden!");
        }
    }
}

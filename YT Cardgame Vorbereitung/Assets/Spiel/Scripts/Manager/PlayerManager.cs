using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager
{
    private Dictionary<ulong, Player> _playerDataDict = new Dictionary<ulong, Player>();

    public void AddNewPlayer(ulong clientId)
    {
        Debug.Log("Ich will einen neuen Spieler hinzufügen");

        if (!_playerDataDict.ContainsKey(clientId))
        {
            _playerDataDict[clientId] = new Player(clientId, new List<int>(), "Player " + clientId, 0);
            Debug.Log("Player " + clientId + " hat sich verbunden");
        }

    }

    public void PrintPlayerDictionary()
    {
        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            ulong id = playerData.Key;
            Player player = playerData.Value;

            Debug.Log("ID: " + id + ", Name: " + player.name + ", Level: " + player.score);
            Debug.Log("Neue Kartenliste im PlayerManager: " + string.Join(", ", player.cards));
        }
    }

    public List<ulong> GetConnectedClientIds()
    {
        return new List<ulong>(_playerDataDict.Keys);
    }

    public Dictionary<ulong, Player> GetPlayerDataDict()
    {
        return _playerDataDict;
    }

    public Player[] GetAllPlayers()
    {
        List<Player> player = new List<Player>(_playerDataDict.Values);
        return player.ToArray(); // Konvertiert die Liste in ein Array
    }

    public List<int> GetPlayerCards(ulong clientId)
    {
        return _playerDataDict[clientId].cards;
    }

    public void SetPlayerCards(ulong clientId, List<int> cards)
    {
        // Statt die Referenz der übergebenen Liste direkt zu verwenden, wird new List<int>(cards) erstellt.
        // Das verhindert unbeabsichtigte Änderungen an der übergebenen Liste, weil List ein Referenztyp ist
        _playerDataDict[clientId].cards = new List<int>(cards);

        Debug.Log("ID: " + clientId);
        Debug.Log("Neue Kartenliste im PlayerManager: " + string.Join(", ", _playerDataDict[clientId].cards));
    }

    /// <summary>
    /// Diese Methode berechnet und aktualisiert den Score eines jeden Spielers
    /// </summary>
    public void CalculatePlayerScores(ulong gameEndingPlayerId)
    {
        // Finden Sie den Spieler mit der niedrigsten Punktzahl
        List<Player> playerList = new List<Player>(_playerDataDict.Values);
        int lowestScore = playerList.Min(player => player.cards.Sum());
        List<Player> playersWithLowestScore = playerList.Where(player => player.cards.Sum() == lowestScore).ToList();

        // Überprüfen Sie, ob einer der Spieler mit der niedrigsten Punktzahl "Cabo" gerufen hat
        Player playerWhoCalledCabo = playersWithLowestScore.Find(player => player.id == gameEndingPlayerId);

        // Wenn ein Spieler "Cabo" gerufen hat, erhält nur dieser Spieler keine zusätzlichen Punkte
        if (playerWhoCalledCabo != null)
        {
            foreach (Player player in playerList)
            {
                if (player != playerWhoCalledCabo)
                {
                    player.score += player.cards.Sum();
                }
            }
        }
        // Wenn kein Spieler "Cabo" gerufen hat, erhalten alle Spieler mit der niedrigsten Punktzahl keine zusätzlichen Punkte
        else
        {
            foreach (Player player in playerList)
            {
                if (!playersWithLowestScore.Contains(player))
                {
                    player.score += player.cards.Sum();
                }
            }
        }

        PrintPlayerDictionary();
    }
}

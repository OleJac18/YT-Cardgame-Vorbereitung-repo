using JetBrains.Annotations;
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
            _playerDataDict[clientId] = new Player(clientId, new List<int>(), "Player " + clientId, 0, 0);
            Debug.Log("Player " + clientId + " hat sich verbunden");
        }

    }

    public void DeletePlayerData()
    {
        Debug.Log("Alle Spieler-Daten werden gelöscht");
        _playerDataDict.Clear();
    }

    public void PrintPlayerDictionary()
    {
        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            ulong id = playerData.Key;
            Player player = playerData.Value;

            Debug.Log("ID: " + id + ", Name: " + player.name + ", Level: " + player.totalScore);
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

    public int GetPlayerCount()
    {
        return _playerDataDict.Count;
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
    public Player CalculatePlayerScores(ulong gameEndingPlayerId)
    {
        // Finden Sie den Spieler mit der niedrigsten Punktzahl
        List<Player> playerList = new List<Player>(_playerDataDict.Values);
        int lowestScore = playerList.Min(player => player.cards.Sum());
        List<Player> playersWithLowestScore = playerList.Where(player => player.cards.Sum() == lowestScore).ToList();

        // Überprüfen Sie, ob einer der Spieler mit der niedrigsten Punktzahl "Cabo" gerufen hat
        Player playerWhoCalledCabo = playersWithLowestScore.Find(player => player.id == gameEndingPlayerId);

        // Wenn der Spieler "Cabo" gerufen hat, erhält nur dieser Spieler keine zusätzlichen Punkte
        if (playerWhoCalledCabo != null)
        {
            foreach (Player player in playerList)
            {
                if (player != playerWhoCalledCabo)
                {
                    player.totalScore += player.cards.Sum();
                    player.roundScore = player.cards.Sum();
                } 
                else
                {
                    player.roundScore = 0;
                }
            }
        }
        // Wenn der/die Spieler mit der niedrigsten Punktzahl nicht "Cabo" gesagt hat,
        // erhalten alle dieser Spieler keine zusätzlichen Punkte
        else
        {
            foreach (Player player in playerList)
            {
                if (!playersWithLowestScore.Contains(player))
                {
                    player.totalScore += player.cards.Sum();
                    player.roundScore = player.cards.Sum();
                }
                else
                {
                    player.roundScore = 0;
                }
            }
        }

        PrintPlayerDictionary();

        return playersWithLowestScore[0];
    }

    public bool CheckScoreOfPlayersForEndOfCompleteGame()
    {
        // Finden Sie den Spieler mit der höchsten Punktzahl
        List<Player> playerList = new List<Player>(_playerDataDict.Values);
        int highestScore = playerList.Max(player => player.totalScore);
        Player player = playerList.Find(player => player.totalScore == highestScore);

        if (highestScore == 100)
        {
            player.totalScore = 50;
            return false;
        }
        else if (highestScore > 100)
        {
            return true;
        } 
        else
        {
            return false;
        }
    }

    public void ResetPlayerTotalCount()
    {
        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            ulong id = playerData.Key;
            Player player = playerData.Value;

            player.totalScore = 0;
            player.roundScore = 0;
            Debug.Log("ID: " + id + ", Name: " + player.name + ", Level: " + player.totalScore);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TurnManager
{
    private ulong _currentPlayerId;
    private List<ulong> _playerOrder;
    public ulong? gameEndingPlayerId;

    public void SetStartPlayer(PlayerManager playerManager)
    {
        // Spielerreihenfolge aus PlayerManager abrufen
        _playerOrder = playerManager.GetConnectedClientIds();

        _currentPlayerId = _playerOrder[0]; // Der erste Spieler wird als Startspieler festgelegt
        gameEndingPlayerId = null;

        Debug.Log("GameEndingPlayerId: " + gameEndingPlayerId);
    }

    public ulong GetCurrentPlayer()
    {
        return _currentPlayerId;
    }

    public void NextTurn()
    {
        if (_playerOrder.Count == 0)
        {
            Debug.LogError("Spielerreihenfolge ist leer!");
        }

        // Zyklisches Iterieren durch die Spielerreihenfolge
        int currentIndex = _playerOrder.IndexOf(_currentPlayerId);
        int nextIndex = (currentIndex + 1) % _playerOrder.Count;
        _currentPlayerId = _playerOrder[nextIndex];

        Debug.Log($"Nächster Spieler: {_currentPlayerId}");

        if (gameEndingPlayerId == _currentPlayerId)
        {
            Debug.Log("!!! DAS SPIEL IST BEENDET!!!");
            GameManager.Instance.EndGame();
        }
    }

    public void OnGameEndButtonPressed(ulong clientId)
    {
        Debug.Log("!!! Das Ende des Spiels wurde eingeläutet !!!");
        if (gameEndingPlayerId == null)
        {
            gameEndingPlayerId = clientId;
        }
    }
}

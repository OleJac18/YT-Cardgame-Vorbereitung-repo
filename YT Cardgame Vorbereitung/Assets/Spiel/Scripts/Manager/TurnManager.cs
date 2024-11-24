using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TurnManager
{
    [SerializeField] private ulong _currentPlayerId;
    [SerializeField] private List<ulong> _playerOrder;

    public void SetStartPlayer(PlayerManager playerManager)
    {
        // Spielerreihenfolge aus PlayerManager abrufen
        _playerOrder = playerManager.GetConnectedClientIds();

        _currentPlayerId = _playerOrder[0]; // Der erste Spieler wird als Startspieler festgelegt
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
    }
}

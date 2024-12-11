using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerUIManager
{
    public static event Action<PlayerNr, Player, bool> InitializePlayerUIEvent;
    public static event Action<ulong> UpdatePlayerUIEvent;

    public void InitializePlayerUI(Player[] players, ulong currentPlayerId)
    {
        int startIndex = 0;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].id == NetworkManager.Singleton.LocalClientId)
            {
                startIndex = i;
                break;
            }
        }

        for (int i = 0; i < players.Length; i++)
        {
            int playerIndex = (i + startIndex) % players.Length;
            bool isCurrentPlayer = currentPlayerId == players[playerIndex].id;

            if (Enum.IsDefined(typeof(PlayerNr), i))
            {
                PlayerNr currentPlayerNr = (PlayerNr)i;
                InitializePlayerUIEvent?.Invoke(currentPlayerNr, players[playerIndex], isCurrentPlayer);
            }
            else
            {
                Debug.LogWarning($"Ungültiger PlayerNr-Wert: {i}");
            }
        }
    }

    public void UpdatePlayerUI(ulong currentPlayerId)
    {
        bool isCurrentPlayer = currentPlayerId == NetworkManager.Singleton.LocalClientId;

        //Updated die PlayerUI beim Spieler
        UpdatePlayerUIEvent?.Invoke(currentPlayerId);
    }
}

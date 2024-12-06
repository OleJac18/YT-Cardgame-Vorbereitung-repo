using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerUIManager : NetworkBehaviour
{
    private readonly PlayerUIManager _playerUIManager;

    public NetworkPlayerUIManager()
    {
        _playerUIManager = new PlayerUIManager();
    }

    public void HandlePlayerAction(PlayerAction action, ulong currentPlayerId, PlayerManager playerManager)
    {
        switch (action)
        {
            case PlayerAction.Initialize:
                Player[] players = playerManager.GetAllPlayers();
                InitializePlayerUIClientsAndHostRpc(players, currentPlayerId);

                break;

            case PlayerAction.ChangeCurrentPlayer:
                ChangeCurrentPlayerClientsAndHostRpc(currentPlayerId);
                break;

            default:
                Debug.LogWarning("Unhandled PlayerAction: " + action);
                break;
        }
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void InitializePlayerUIClientsAndHostRpc(Player[] players, ulong currentPlayerId)
    {
        _playerUIManager.InitializePlayerUI(players, currentPlayerId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChangeCurrentPlayerClientsAndHostRpc(ulong currentPlayerId)
    {
        _playerUIManager.UpdatePlayerUI(currentPlayerId);
    }
}

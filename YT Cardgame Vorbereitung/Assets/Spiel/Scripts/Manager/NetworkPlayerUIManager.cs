using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerUIManager : NetworkBehaviour
{
    private readonly PlayerUIManager _playerUIManager;
    private PlayerManager _playerManager;

    public NetworkPlayerUIManager()
    {
        _playerUIManager = new PlayerUIManager();

    }

    public void InitializePlayerUI(ulong currentPlayerId, PlayerManager playerManager)
    {
        Player[] players = playerManager.GetAllPlayers();

        InitalizePlayerUIManagerClientsAndHostRpc(players, currentPlayerId);
    }


    //////////////////////////////////////////////////////////////////////

    [Rpc(SendTo.ClientsAndHost)]
    private void InitalizePlayerUIManagerClientsAndHostRpc(Player[] players, ulong currentPlayerId)
    {
        _playerUIManager.InitializePlayerUI(players, currentPlayerId);
    }
}

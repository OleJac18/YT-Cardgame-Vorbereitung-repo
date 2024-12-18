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

    void Start()
    {
        GameManager.Instance.currentPlayerId.OnValueChanged += ChangeCurrentPlayer;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        GameManager.Instance.currentPlayerId.OnValueChanged -= ChangeCurrentPlayer;
    }

    public void SetPlayerManager(PlayerManager playerManager)
    {
        _playerManager = playerManager;
    }

    public void HandlePlayerAction(PlayerAction action, ulong currentPlayerId)
    {
        switch (action)
        {
            case PlayerAction.Initialize:
                Player[] players = _playerManager.GetAllPlayers();
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

    /// <summary>
    /// //Updated die PlayerUI beim Spieler
    /// </summary>
    /// <param name="previousPlayerId"></param>
    /// <param name="currentPlayerId"></param>
    private void ChangeCurrentPlayer(ulong previousPlayerId, ulong currentPlayerId)
    {
        HandlePlayerAction(PlayerAction.ChangeCurrentPlayer, currentPlayerId);
    }


    //////////////////////////////////////////////////////////////////////

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

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkCardManager : NetworkBehaviour
{
    private PlayerManager _playerManager;
    private CardManager _cardManager;

    private void Start()
    {
        // Suche zur Laufzeit nach den Instanzen
        _playerManager = FindObjectOfType<PlayerManager>();
        _cardManager = FindObjectOfType<CardManager>();

        ConnectionManager.ClientConnectedEvent += CheckAllClientsConnected;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ConnectionManager.ClientConnectedEvent -= CheckAllClientsConnected;
    }

    private void CheckAllClientsConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            List<ulong> clientIds = _playerManager.GetConnectedClientIds();
            if (clientIds.Count < 2) return;

            DistributeCardsToPlayers(clientIds);
        }
    }

    private void DistributeCardsToPlayers(List<ulong> clientIds)
    {
        foreach (ulong clientId in clientIds)
        {
            List<int> playerCards = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                int drawnCard = _cardManager.DrawTopCard();

                if (drawnCard != 100)
                {
                    playerCards.Add(drawnCard);
                }
                else
                {
                    Debug.Log("Kartenstapel ist leer.");
                    return;
                }
            }

            SpawnCardsClientRpc(playerCards.ToArray(), RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
    }


    [Rpc(SendTo.SpecifiedInParams)]
    private void SpawnCardsClientRpc(int[] playerCards, RpcParams rpcParams = default)
    {
        //if (NetworkManager.Singleton.LocalClientId != clientId) return;
        Debug.Log("Ich bin in der SpawnCardsClientRpc Methode");
        _cardManager.ServFirstCards(playerCards);
    }
}

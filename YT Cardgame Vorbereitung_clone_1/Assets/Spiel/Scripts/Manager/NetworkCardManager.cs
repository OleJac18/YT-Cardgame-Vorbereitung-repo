using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkCardManager : NetworkBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _playerDrawnCardPos;
    [SerializeField] private GameObject _enemyDrawnCardPos;

    private PlayerManager _playerManager;
    private CardManager _cardManager;

    private void Start()
    {
        // Suche zur Laufzeit nach den Instanzen
        _playerManager = FindObjectOfType<PlayerManager>();
        _cardManager = FindObjectOfType<CardManager>();

        ConnectionManager.ClientConnectedEvent += CheckAllClientsConnected;
        CardDeckUI.OnCardDeckClicked += HandleCardDeckClicked;
        CardController.OnCardHovered += SetEnemyCardHoverEffectClientRpc;
        CardController.OnCardClicked += SetEnemyCardClickClientRpc;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ConnectionManager.ClientConnectedEvent -= CheckAllClientsConnected;
        CardDeckUI.OnCardDeckClicked -= HandleCardDeckClicked;
        CardController.OnCardHovered -= SetEnemyCardHoverEffectClientRpc;
        CardController.OnCardClicked -= SetEnemyCardClickClientRpc;
    }

    private void HandleCardDeckClicked()
    {
        DrawAndSpawnTopCardServerRpc(NetworkManager.LocalClientId);
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

    //////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 
    /// </summary>
    /// <param name="playerCards"></param>
    /// <param name="rpcParams"></param>

    [Rpc(SendTo.SpecifiedInParams)]
    private void SpawnCardsClientRpc(int[] playerCards, RpcParams rpcParams = default)
    {
        //if (NetworkManager.Singleton.LocalClientId != clientId) return;
        Debug.Log("Ich bin in der SpawnCardsClientRpc Methode");
        _cardManager.ServFirstCards(playerCards);
    }

    /// <summary>
    /// Holt sich die oberste Karte vom CardDeck. Da aber nur der Server das Kartendeck hat, muss ein Rpc Call
    /// zum Server gemacht werden. Danach wird die oberste Karte vom CardDeck bei dem spezifischen Client
    /// gespawnt, bei dem auf das CardDeck geklickt wurde.
    /// </summary>
    /// <param name="clientId"></param>
    [Rpc(SendTo.Server)]
    public void DrawAndSpawnTopCardServerRpc(ulong clientId)
    {
        _cardManager.topCardNumber = _cardManager.DrawTopCard();

        if (_cardManager.topCardNumber != 100)
        {
            SpawnCardDeckCardSpecificClientRpc(_cardManager.topCardNumber, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
        else
        {
            Debug.Log("Kartenstapel ist leer.");
        }
    }

    /// <summary>
    /// Spawned die oberste Karte vom CardDeck bei dem spezifischen Client  bei dem auf das CardDeck geklickt wurde
    /// Im Anschluss wird bei allen anderen Clients auch die oberste Karte gespawnt aber mit einer -1 als Kartennummer
    /// </summary>
    /// <param name="cardNumber"></param>
    /// <param name="rpcParams"></param>
    [Rpc(SendTo.SpecifiedInParams)]
    public void SpawnCardDeckCardSpecificClientRpc(int cardNumber, RpcParams rpcParams = default)
    {
        Debug.Log("topCardNumber from ClientRpc Call: " + cardNumber);

        // Spawned eine Karte beim Spieler, der auf den Kartenstapel gedrückt hat
        _cardManager.SpawnAndMoveCardToDrawnCardPos(cardNumber, _playerDrawnCardPos.transform, true);

        // Spawned bei allen anderen Clients eine Karte vom Kartendeck
        SpawnCardDeckCardClientRpc();
    }

    /// <summary>
    /// Spawned eine CardDeck Karte bei allen Clients/Server, die nicht auf die CardDeck Karte geklickt haben
    /// </summary>
    [Rpc(SendTo.NotMe)]
    private void SpawnCardDeckCardClientRpc()
    {
        Debug.Log("Client Spawned a CardDeck Card!");

        _cardManager.SpawnAndMoveCardToDrawnCardPos(99, _enemyDrawnCardPos.transform, false);
    }

    /// <summary>
    /// Lässt bei allen Clients eine spezifische Enemycard gehovert aussehen.
    /// </summary>
    /// <param name="scaleby"></param>
    /// <param name="index"></param>
    [Rpc(SendTo.NotMe)]
    private void SetEnemyCardHoverEffectClientRpc(Vector3 scaleby, int index)
    {
        if (IsServer && !IsHost) return;
        _cardManager.SetEnemyCardHoverEffect(scaleby, index);
    }


    /// <summary>
    /// Lässt bei allen Clients eine spezifische Enemycard selektiert aussehen
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    [Rpc(SendTo.NotMe)]
    private void SetEnemyCardClickClientRpc(bool isSelected, int index)
    {
        if (IsServer && !IsHost) return;

        _cardManager.SetEnemyCardClick(isSelected, index);
    }
}

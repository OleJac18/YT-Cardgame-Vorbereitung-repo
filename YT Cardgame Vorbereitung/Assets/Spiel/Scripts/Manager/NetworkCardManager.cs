using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkCardManager : NetworkBehaviour
{
    public static event Action HidePlayerButtonEvent;

    [Header("UI Elements")]
    public GameObject _playerDrawnCardPos;
    public GameObject _enemyDrawnCardPos;

    private CardManager _cardManager;

    // Start is called before the first frame update
    void Start()
    {
        _cardManager = FindObjectOfType<CardManager>();

        CardDeckUI.OnCardDeckClicked += HandleCardDeckClicked;
        CardController.OnCardHoveredEvent += SetEnemyCardHoverEffectClientRpc;
        CardController.OnCardClickedEvent += SetEnemyCardClickedClientRpc;
        CardController.OnGraveyardCardClickedEvent += MoveGraveyardCardToEnemyDrawnPosClientRpc;
        CardManager.DiscardCardEvent += MoveEnemyCardToGraveyardPos;
        GameManager.ServFirstCardEvent += ServFirstCards;
        GameManager.ProcessSelectedCardsEvent += ProcessSelectedCards;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CardDeckUI.OnCardDeckClicked -= HandleCardDeckClicked;
        CardController.OnCardHoveredEvent -= SetEnemyCardHoverEffectClientRpc;
        CardController.OnCardClickedEvent -= SetEnemyCardClickedClientRpc;
        CardController.OnGraveyardCardClickedEvent -= MoveGraveyardCardToEnemyDrawnPosClientRpc;
        CardManager.DiscardCardEvent -= MoveEnemyCardToGraveyardPos;
        GameManager.ServFirstCardEvent -= ServFirstCards;
        GameManager.ProcessSelectedCardsEvent -= ProcessSelectedCards;
    }

    private void HandleCardDeckClicked()
    {
        DrawAndSpawnTopCardServerRpc(NetworkManager.LocalClientId);
    }

    private void ServFirstCards(PlayerManager playerManager, ulong currentPlayerId)
    {
        DistributeCardsToPlayers(playerManager);

        int drawnCard = _cardManager.DrawTopCard();
        Debug.Log("Ich habe die Karte " + drawnCard + " für das Graveyard gezogen.");
        SpawnGraveyardCardClientAndHostRpc(drawnCard, currentPlayerId);
    }

    private void DistributeCardsToPlayers(PlayerManager playerManager)
    {
        Dictionary<ulong, Player> _playerDataDict = playerManager.GetPlayerDataDict();

        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            ulong id = playerData.Key;
            Player player = playerData.Value;

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

            GameManager.Instance.SetPlayerCards(id, playerCards);
            SpawnCardsClientRpc(playerCards.ToArray(), RpcTarget.Single(player.id, RpcTargetUse.Temp));
        }
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////


    public void MoveEnemyCardToGraveyardPos()
    {
        int drawnCardNumber = _cardManager.GetDrawnCardNumber();
        MoveEnemyCardToGraveyardPosClientRpc(drawnCardNumber);
    }

    public void ExchangeButtonClicked()
    {
        if (_cardManager.IsAnyCardSelected())
        {
            HidePlayerButtonEvent?.Invoke();
            HandleCardExchangeClickedServerRpc(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            Debug.Log("Es wurde keine Karte zum Tauschen angeklickt!");
        }
    }

    private void ProcessSelectedCards(int[] cards)
    {
        if (_cardManager.AreSelectedCardsEqual(cards))
        {
            // Updated die aktuellen Karten für den GameManager
            int[] newPlayerCards = _cardManager.UpdatePlayerCards(cards);
            UpdatePlayerCardsServerRpc(NetworkManager.Singleton.LocalClientId, newPlayerCards);

            // Tauscht die angeklickten Karten vom Spieler mit der gezogenen Karte
            _cardManager.ExchangePlayerCards(cards);
            bool[] clickedCards = _cardManager.GetClickedCards();
            MoveDrawnCardToEnemyClientRpc(clickedCards, cards);
        }
        else
        {
            int drawnCardNumber = _cardManager.GetDrawnCardNumber();

            // Legt die gezogene Karte auf den Ablagestapel ab
            _cardManager.ResetOutlinePlayerCards();
            _cardManager.MovePlayerDrawnCardToGraveyardPos();
            MoveEnemyCardToGraveyardPosClientRpc(drawnCardNumber);
        }
    }


    //////////////////////////////////////////////////////////////////////////////////////

    [Rpc(SendTo.SpecifiedInParams)]
    private void SpawnCardsClientRpc(int[] playerCards, RpcParams rpcParams = default)
    {
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
        // Spawned eine Karte beim Spieler, der auf den Kartenstapel gedrückt hat
        _cardManager.SpawnAndMoveCardDeckCardToDrawnCardPos(cardNumber, _playerDrawnCardPos.transform, true);

        // Spawned bei allen anderen Clients eine Karte vom Kartendeck
        SpawnCardDeckCardClientRpc();
    }

    /// <summary>
    /// Spawned eine CardDeck Karte bei allen Clients/Server, die nicht auf die CardDeck Karte geklickt haben
    /// </summary>
    [Rpc(SendTo.NotMe)]
    private void SpawnCardDeckCardClientRpc()
    {
        _cardManager.SpawnAndMoveCardDeckCardToDrawnCardPos(99, _enemyDrawnCardPos.transform, false);
    }

    /// <summary>
    /// Lässt bei allen Clients eine spezifische Enemycard gehovert aussehen.
    /// </summary>
    /// <param name="scaleby"></param>
    /// <param name="index"></param>
    [Rpc(SendTo.NotMe)]
    private void SetEnemyCardHoverEffectClientRpc(Vector3 scaleBy, int index)
    {
        if (IsServer && !IsHost) return;
        _cardManager.SetEnemyCardHoverEffect(scaleBy, index);
    }


    /// <summary>
    /// Lässt bei allen Clients eine spezifische Enemycard selektiert aussehen
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    [Rpc(SendTo.NotMe)]
    private void SetEnemyCardClickedClientRpc(bool isSelected, int index)
    {
        if (IsServer && !IsHost) return;
        _cardManager.SetEnemyCardClicked(isSelected, index);
        _cardManager.SetClickedCard(isSelected, index);
    }

    /// <summary>
    /// Spawned eine Graveyardkarte bei jedem Client
    /// </summary>
    /// <param name="cardNumber"></param>
    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnGraveyardCardClientAndHostRpc(int cardNumber, ulong currentPlayerId)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        bool isSelectable = currentPlayerId == localClientId;

        _cardManager.SpawnAndMoveGraveyardCard(cardNumber, isSelectable);
    }

    /// <summary>
    /// Bewegt die Graveyardkarte zum Enemy bei allen Clients, außer dem Client, der auf die Karte geklickt hat
    /// </summary>
    [Rpc(SendTo.NotMe)]
    private void MoveGraveyardCardToEnemyDrawnPosClientRpc()
    {
        _cardManager.MoveGraveyardCardToDrawnPos(_enemyDrawnCardPos.transform, false);
    }


    /// <summary>
    /// Bewegt die Enemy Karte zum Graveyard bei allen Clients, außer dem Client, der auf die Karte geklickt hat
    /// </summary>
    [Rpc(SendTo.NotMe)]
    private void MoveEnemyCardToGraveyardPosClientRpc(int cardNumber)
    {
        _cardManager.ResetOutlineEnemyCards();
        _cardManager.MoveEnemyDrawnCardToGraveyardPos(cardNumber);
    }

    /// <summary>
    /// Bewegt die gezogene Karte zu der ersten ausgewählten Karte beim Enemy
    /// </summary>
    [Rpc(SendTo.NotMe)]
    private void MoveDrawnCardToEnemyClientRpc(bool[] clickedCards, int[] cards)
    {
        _cardManager.ExchangeEnemyCards(clickedCards, cards);
    }

    [Rpc(SendTo.NotMe)]
    public void ResetOutlineEnemyCardsClientRpc()
    {
        _cardManager.ResetOutlineEnemyCards();
    }


    /////////////////////////////////////////////////////
    [Rpc(SendTo.Server)]
    private void UpdatePlayerCardsServerRpc(ulong clientId, int[] cards)
    {
        GameManager.Instance.SetPlayerCards(clientId, cards.ToList<int>());
    }

    [Rpc(SendTo.Server)]
    private void HandleCardExchangeClickedServerRpc(ulong clientId)
    {
        GameManager.Instance.GetPlayerCardsAndProcessSelectedCards(clientId);
    }
}

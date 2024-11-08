using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CardManager : NetworkBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private GameObject _spawnCardDeckPos;
    [SerializeField] private GameObject _spawnCardPlayerPos;
    [SerializeField] private GameObject _spawnCardEnemyPos;
    [SerializeField] private GameObject _playerDrawnCardPos;
    [SerializeField] private GameObject _enemyDrawnCardPos;

    [Header("Interne Variablen")]
    [SerializeField] private int _topCardNumber = -1;
    [SerializeField] private GameObject _cardDeckCard;

    [SerializeField] private CardStack _cardStack;

    // Start is called before the first frame update
    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            _cardStack = new CardStack();
            _cardStack.CreateDeck();
            _cardStack.ShuffleCards();
        };


        CardDeckUI.OnCardDeckClicked += HandleCardDeckClicked;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CardDeckUI.OnCardDeckClicked -= HandleCardDeckClicked;
    }

    private void HandleCardDeckClicked()
    {
        GetTopCardDeckCardServerRpc(NetworkManager.LocalClientId);
    }

    private void SpawnAndMoveCardToDrawnCardPos(int cardNumber, Transform target, bool flipAtDestination)
    {
        // Spawned eine neue Karte vom Kartenstapel
        _cardDeckCard = SpawnCard(cardNumber, _spawnCardDeckPos, _spawnCardDeckPos.transform.parent,
                                    Card.Stack.CARDDECK, true, false, false);

        Vector3 targetPos = GetCenteredPosition(target);

        // Bewegt die gespawnte Karte vom Kartenstapel zum Spieler, der diese gezogen hat
        LeanTween.move(_cardDeckCard, targetPos, 0.5f).setOnComplete(() =>
        {
            if (flipAtDestination)
            {
                CardController controller = _cardDeckCard.GetComponent<CardController>();
                controller.FlipCardAnimation();
            }

            _cardDeckCard.transform.SetParent(target);

        });
    }

    /// <summary>
    /// Berechne centerOffset:
    /// target.pivot.x und target.pivot.y geben an, wo der Pivot relativ zur Größe des RectTransform liegt.Ein Pivot von(0.5, 0.5) bedeutet, 
    /// dass der Mittelpunkt bereits in der Mitte liegt, bei(0, 0) liegt er unten links usw. Durch width * (0.5f - target.pivot.x) wird die 
    /// X-Verschiebung vom tatsächlichen Pivot zum gedachten Mittelpunkt berechnet.Dasselbe gilt für die Y-Achse mit height* (0.5f - target.pivot.y).
    /// 
    /// Konvertiere in Weltkoordinaten:
    /// Die Methode TransformPoint(centerOffset) wendet die berechnete Verschiebung auf die Weltposition des RectTransform an, 
    /// sodass targetPos das Ziel anzeigt, als wäre der Pivot in der Mitte.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private Vector3 GetCenteredPosition(Transform target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();

        // Berechne die Verschiebung basierend auf Anker und Pivot
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        Vector3 centerOffset = new Vector3(width * (0.5f - rectTransform.pivot.x), height * (0.5f - rectTransform.pivot.y), 0);

        // Berechne und gib die Zielposition in Weltkoordinaten zurück
        return rectTransform.TransformPoint(centerOffset);
    }

    private GameObject SpawnCard(int cardNumber, GameObject targetPos, Transform parent, Card.Stack corresDeck, bool backCardIsVisible, bool canHover, bool isSelectable)
    {
        GameObject spawnCard = Instantiate(_cardPrefab, targetPos.transform.position, targetPos.transform.rotation);

        spawnCard.transform.SetParent(parent);
        spawnCard.transform.localScale = Vector3.one;

        CardController controller = spawnCard.GetComponent<CardController>();
        controller.SetCorrespondingDeck(corresDeck);
        controller.SetCardBackImageVisibility(backCardIsVisible);
        controller.CardNumber = cardNumber;
        controller.canHover = canHover;
        controller.isSelectable = isSelectable;

        return spawnCard;
    }

    public int DrawTopCard()
    {
        return _cardStack.DrawTopCard();
    }

    public void ServFirstCards(int[] playerCards)
    {
        for (int i = 0; i < 4; i++)
        {
            // Spawned die Spielerkarten
            SpawnCard(playerCards[i], _spawnCardPlayerPos, _spawnCardPlayerPos.transform,
                        Card.Stack.PLAYERCARD, false, true, true);

            // Spawned die Gegnerkarten
            SpawnCard(99, _spawnCardEnemyPos, _spawnCardEnemyPos.transform,
                        Card.Stack.ENEMYCARD, true, false, false);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////


    /// <summary>
    /// Holt sich die oberste Karte vom CardDeck. Da aber nur der Server das Kartendeck hat, muss ein Rpc Call
    /// zum Server gemacht werden. Danach wird die oberste Karte vom CardDeck bei dem spezifischen Client
    /// gespawnt, bei dem auf das CardDeck geklickt wurde.
    /// </summary>
    /// <param name="clientId"></param>
    [Rpc(SendTo.Server)]
    public void GetTopCardDeckCardServerRpc(ulong clientId)
    {
        _topCardNumber = _cardStack.DrawTopCard();

        if (_topCardNumber != 100)
        {
            SpawnCardDeckCardSpecificClientRpc(_topCardNumber, RpcTarget.Single(clientId, RpcTargetUse.Temp));
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
        SpawnAndMoveCardToDrawnCardPos(cardNumber, _playerDrawnCardPos.transform, true);

        // Spawned bei allen anderen Clients eine Karte vom Kartendeck
        SpawnCardDeckCardClientRpc();
    }

    /// <summary>
    /// Spawned eine CardDeck Karte bei allen Clients/Server, die nicht auf die CardDeck Karte geklickt haben
    /// </summary>
    [Rpc(SendTo.NotMe)]
    public void SpawnCardDeckCardClientRpc()
    {
        Debug.Log("Client Spawned a CardDeck Card!");

        SpawnAndMoveCardToDrawnCardPos(99, _enemyDrawnCardPos.transform, false);
    }

}

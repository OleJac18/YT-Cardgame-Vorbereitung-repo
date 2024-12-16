using System;
using System.Collections;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class CardManager : MonoBehaviour
{
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private GameObject _spawnCardDeckPos;
    [SerializeField] private GameObject _spawnCardPlayerPos;
    [SerializeField] private GameObject _spawnCardEnemyPos;
    [SerializeField] private GameObject _showDrawnCardPos;
    [SerializeField] private GameObject _graveyardPos;
    [SerializeField] private GameObject _playerDrawnCardPos;

    public static event Action ShowButtonsEvent;
    public static event Action EndTurnEvent;

    public int topCardNumber = -1;

    [SerializeField] private CardStack _cardStack;

    [SerializeField] private GameObject _cardDeckCard;
    [SerializeField] private GameObject _graveyardCard;
    [SerializeField] private GameObject _drawnCard;

    [SerializeField] private bool[] _clickedCards;

    // Start is called before the first frame update
    void Start()
    {
        _clickedCards = new bool[4];

        if (NetworkManager.Singleton.IsServer)
        {
            _cardStack = new CardStack();
            _cardStack.CreateDeck();
            _cardStack.ShuffleCards();
        }

        CardController.OnGraveyardCardClickedEvent += MoveGraveyardCardToPlayerPos;
        ButtonController.DiscardCardEvent += MoveDrawnCardToGraveyardPos;
        CardController.OnCardClickedEvent += SetClickedCards;
        ButtonController.ExchangeCardEvent += ExchangePlayerCards;
    }

    private void OnDestroy()
    {
        CardController.OnGraveyardCardClickedEvent -= MoveGraveyardCardToPlayerPos;
        ButtonController.DiscardCardEvent -= MoveDrawnCardToGraveyardPos;
        CardController.OnCardClickedEvent -= SetClickedCards;
        ButtonController.ExchangeCardEvent -= ExchangePlayerCards;
    }

    public int DrawTopCard()
    {
        return _cardStack.DrawTopCard();
    }

    public void SetClickedCards(bool isSelected, int index)
    {
        _clickedCards[index] = isSelected;
    }

    public void ServFirstCards(int[] playerCards)
    {
        StartCoroutine(ServFirstCardsCoroutine(playerCards));
    }

    IEnumerator ServFirstCardsCoroutine(int[] playerCards)
    {
        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForSeconds(0.5f);
            // Spawned die Spielerkarten
            SpawnCard(playerCards[i], _spawnCardPlayerPos, _spawnCardPlayerPos.transform,
                        Card.Stack.PLAYERCARD, true, true, true);

            // Spawned die Gegnerkarten
            SpawnCard(99, _spawnCardEnemyPos, _spawnCardEnemyPos.transform,
                        Card.Stack.ENEMYCARD, true, false, false);
        }
    }

    /// <summary>
    /// Updated den Hoverzustand von einer Enemy Karte. Je nachdem ob der Mauszeiger auf der Karte ist oder nicht
    /// </summary>
    /// <param name="scaleby"></param>
    /// <param name="index"></param>
    public void SetEnemyCardHoverEffect(Vector3 scaleBy, int index)
    {
        GameObject card = _spawnCardEnemyPos.transform.GetChild(index).gameObject;
        card.transform.localScale = scaleBy;
    }

    /// <summary>
    /// Updated den Selektierzustand von einer Enemy Karte. Je nachdem ob der Mauszeiger auf der Karte ist oder nicht
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    public void SetEnemyCardClicked(bool isSelected, int index)
    {
        GameObject card = _spawnCardEnemyPos.transform.GetChild(index).gameObject;
        Outline outline = card.GetComponent<Outline>();

        if (outline == null)
        {
            Debug.Log("Das Objekt hat keine Outline");
        }
        else
        {
            outline.enabled = isSelected;
        }
    }

    /// <summary>
    /// Berechne centerOffset:
    /// target.pivot.x und target.pivot.y geben an, wo der Pivot relativ zur Gr��e des RectTransform liegt.Ein Pivot von(0.5, 0.5) bedeutet, 
    /// dass der Mittelpunkt bereits in der Mitte liegt, bei(0, 0) liegt er unten links usw. Durch width * (0.5f - target.pivot.x) wird die 
    /// X-Verschiebung vom tats�chlichen Pivot zum gedachten Mittelpunkt berechnet.Dasselbe gilt f�r die Y-Achse mit height* (0.5f - target.pivot.y).
    /// 
    /// Konvertiere in Weltkoordinaten:
    /// Die Methode TransformPoint(centerOffset) wendet die berechnete Verschiebung auf die Weltposition des RectTransform an, 
    /// sodass targetPos das Ziel anzeigt, als w�re der Pivot in der Mitte.
    /// 
    /// Der Ausdruck 0.5f - target.pivot.y funktioniert wie folgt:
    /// Wenn target.pivot.y bei 0.5 liegt (Mitte): 0.5f - 0.5f = 0. Kein Offset ist n�tig, weil der Pivot bereits in der Mitte liegt.
    /// Wenn target.pivot.y bei 0 liegt (unten): 0.5f - 0 = 0.5. Der Offset verschiebt die Position nach oben, um den unteren Pivot so anzupassen, dass es aussieht, als w�re er in der Mitte.
    /// Wenn target.pivot.y bei 1 liegt (oben): 0.5f - 1 = -0.5. Der Offset verschiebt die Position nach unten, um die obere Kante so anzupassen, dass es aussieht, als w�re der Pivot in der Mitte.
    /// Warum der Offset notwendig ist
    /// 
    /// Wenn wir die H�he des RectTransform ber�cksichtigen, indem wir sie mit diesem Offset multiplizieren (height* (0.5f - target.pivot.y)), erhalten wir den notwendigen Abstand in lokalen 
    /// Koordinaten:
    /// Ein Wert von +0.5 * height verschiebt das Objekt um die halbe H�he nach oben.
    /// Ein Wert von -0.5 * height verschiebt es um die halbe H�he nach unten.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private Vector3 GetCenteredPosition(Transform target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        Vector3 centerOffset = new Vector3(width * (0.5f - rectTransform.pivot.x), height * (0.5f - rectTransform.pivot.y), 0);

        return rectTransform.TransformPoint(centerOffset);
    }

    public void SpawnAndMoveGraveyardCard(int cardNumber, bool isSelectable)
    {
        // Spawned eine neue Karte vom Kartenstapel f�r das Graveyard
        _graveyardCard = SpawnCard(cardNumber, _spawnCardDeckPos, _spawnCardDeckPos.transform.parent,
                                    Card.Stack.GRAVEYARD, true, false, isSelectable);

        // Setzt den Parent zuerst vom Table, damit die Karte �ber dem Kartenstapel ist
        _graveyardCard.transform.SetParent(_graveyardPos.transform.parent);

        CardController controller = _graveyardCard.GetComponent<CardController>();

        Vector3 target = GetCenteredPosition(_graveyardPos.transform);

        // Bewegen der Karte vom Kartenstapel zum Graveyard
        LeanTween.move(_graveyardCard, target, 0.5f).setOnComplete(() =>
        {
            _graveyardCard.transform.SetParent(_graveyardPos.transform);
            controller.FlipCardAnimation(false);
        });
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


    /////////////////////////////////////////////////////////////////////////////
    // Bewegung der _cardDeckCard zur DrawnCardPos

    public void SpawnAndMoveCardDeckCardToDrawnCardPos(int cardNumber, Transform target, bool flipAtDestination)
    {
        // Spawned die oberste Karte vom Kartenstapel
        _cardDeckCard = SpawnCard(cardNumber, _spawnCardDeckPos, _spawnCardDeckPos.transform.parent, Card.Stack.CARDDECK, true, false, false);

        if (flipAtDestination)
        {
            FlipAndMoveCard(_cardDeckCard, target);
        }
        else
        {
            MoveToDrawnPosition(_cardDeckCard, target, false);
        }
    }

    private void FlipAndMoveCard(GameObject objectToMove, Transform target)
    {
        CardController controller = objectToMove.GetComponent<CardController>();

        LeanTween.move(objectToMove, _showDrawnCardPos.transform, 0.5f);
        LeanTween.scale(objectToMove, Vector3.one * 1.2f, 0.5f);

        LeanTween.rotateX(objectToMove, 90.0f, 0.25f).setOnComplete(() =>
        {
            controller.SetCardBackImageVisibility(false);
            LeanTween.rotateX(objectToMove, 0.0f, 0.25f).setOnComplete(() =>
            {
                LeanTween.delayedCall(0.5f, () =>
                {
                    MoveToDrawnPosition(objectToMove, target, true);
                });
            });
        });
    }

    /// <summary>
    /// Mit dieser Funktion wird eine Karte zu einem gew�nschten Ziel bewegt.
    /// </summary>
    /// <param name="objectToMove"></param>
    /// <param name="target"></param>
    private void MoveToDrawnPosition(GameObject objectToMove, Transform target, bool showButton)
    {
        Vector3 targetPos = GetCenteredPosition(target);

        LeanTween.scale(objectToMove, Vector3.one, 0.5f);

        LeanTween.move(objectToMove, targetPos, 0.5f).setOnComplete((Action)(() =>
        {
            objectToMove.transform.SetParent(target);

            SetCardToDrawnCard(objectToMove, showButton);
        }));
    }


    /// <summary>
    /// Ist f�r die 
    /// </summary>
    /// <param name="objectToMove"></param>
    /// <param name="newObject"></param>
    private void SetCardToDrawnCard(GameObject objectToMove, bool showButton)
    {
        // �berschreibt die bewegte Karte auf die _drawnCard
        _drawnCard = objectToMove;

        // Guckt welche Karte bewegt worden ist und l�scht diese im Anschluss
        CardController controllerObjToMove = objectToMove.GetComponent<CardController>();
        Card.Stack corresDeck = controllerObjToMove.GetCorrespondingDeck();


        if (corresDeck == Card.Stack.GRAVEYARD)
        {
            _graveyardCard = null;
        }
        else
        {
            _cardDeckCard = null;
        }

        // �ndert das correspondingDeck von der _drawnCard
        CardController controllerDrawnCard = _drawnCard.GetComponent<CardController>();
        controllerDrawnCard.SetCorrespondingDeck(Card.Stack.DRAWNCARD);

        // Guckt, ob die Buttons zum Abwerfen oder Tauschen angezeigt werden sollen
        if (showButton)
        {
            ShowButtonsEvent?.Invoke();
        }
    }

    private void MoveGraveyardCardToPlayerPos()
    {
        MoveGraveyardCardToDrawnPos(_playerDrawnCardPos.transform, true);
    }

    public void MoveGraveyardCardToDrawnPos(Transform target, bool showButton)
    {
        MoveToDrawnPosition(_graveyardCard, target, showButton);

        CardController controller = _graveyardCard.GetComponent<CardController>();
        controller.isSelectable = false;
    }


    /////////////////////////////////////////////////////////////////////////////
    // Bewegung der DrawnCard zum Graveyard


    public void MoveDrawnCardToGraveyardPos()
    {
        Vector3 targetPos = GetCenteredPosition(_graveyardPos.transform);

        // Bewegt die Karte zum Graveyard
        LeanTween.move(_drawnCard, targetPos, 0.5f).setOnComplete(() =>
        {
            _drawnCard.transform.SetParent(_graveyardPos.transform);

            SetCardToGraveyardCard(_drawnCard);
            _drawnCard = null;
            EndTurnEvent?.Invoke();
        });
    }

    /// <summary>
    /// �bertr�gt das _drawnCard GameObject auf das _graveyardCard GameObject und l�scht das _drawnCard GameObject
    /// �ndert das correspondingDeck auf GRAVEYARD
    /// </summary>
    private void SetCardToGraveyardCard(GameObject objectToMove)
    {
        _graveyardCard = objectToMove;

        CardController controllerDrawnCard = _graveyardCard.GetComponent<CardController>();
        controllerDrawnCard.SetCorrespondingDeck(Card.Stack.GRAVEYARD);


    }

    /////////////////////////////////////////////////////////////////////////////
    // Bewegung der DrawnCard zum Spieler

    public int FindFirstTrueIndex(bool[] boolArray)
    {
        for (int i = 0; i < boolArray.Length; i++)
        {
            if (boolArray[i])
            {
                return i; // Gibt den Index des ersten Elements zur�ck, das true ist
            }
        }
        return -1; // Gibt -1 zur�ck, wenn kein true gefunden wurde
    }

    public void ExchangeEnemyCards()
    {
        ExchangeCards(_spawnCardEnemyPos);
    }

    public void ExchangePlayerCards()
    {
        ExchangeCards(_spawnCardPlayerPos);
    }


    public void ExchangeCards(GameObject playerPanel)
    {
        MovePlayerCardsToGraveyardPos(playerPanel);
        MoveDrawnCardToTarget(playerPanel);
    }

    public void MovePlayerCardsToGraveyardPos(GameObject playerPanel)
    {
        // Erste Karte finden, die geklickt wurde
        int index = FindFirstTrueIndex(_clickedCards);
        GameObject _firstSelectedCard = playerPanel.transform.GetChild(index).gameObject;

        Vector3 targetPos = GetCenteredPosition(_graveyardPos.transform);

        // Bewegt die Karte zum Graveyard
        LeanTween.move(_firstSelectedCard, targetPos, 0.5f).setOnComplete(() =>
        {
            _firstSelectedCard.transform.SetParent(_graveyardPos.transform);

            SetCardToGraveyardCard(_firstSelectedCard);
        });
    }

    private void MoveDrawnCardToTarget(GameObject playerPanel)
    {
        // Erste Karte finden, die geklickt wurde
        int index = FindFirstTrueIndex(_clickedCards);
        GameObject _firstSelectedCard = playerPanel.transform.GetChild(index).gameObject;

        // Herausfinden, ob man der aktuelle Spieler ist, damit man sagen kann, wie der Bogen bei der Bewegung sein soll
        int rotation;
        bool isCurrentPlayer = GameManager.Instance.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId;

        rotation = isCurrentPlayer ? 1 : -1;

        Vector3[] points = MoveInCircle.CalculateCircle(8, _drawnCard.transform, _firstSelectedCard.transform, rotation, 100);
        LeanTween.moveSpline(_drawnCard, points, 0.5f).setOnComplete(() =>
        {
            CardController controller = _drawnCard.GetComponent<CardController>();
            controller.FlipCardAnimation(true);

            // Parent der bewegten Karte zu dem spezifischen Playerpanel setzen und an die richtige Position
            _drawnCard.transform.SetParent(playerPanel.transform);
            _drawnCard.transform.SetSiblingIndex(index);

            // Gezogene Karte intern l�schen und den Zug beenden
            _drawnCard = null;
            ResetClickedCards();
            EndTurnEvent?.Invoke();
        });
    }

    private void ResetClickedCards()
    {
        Array.Fill(_clickedCards, false);
    }

}

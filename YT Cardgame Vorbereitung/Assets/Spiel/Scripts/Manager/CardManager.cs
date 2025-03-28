using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum SpecialAction
{
    None,   // Keine spezielle Aktion
    Peak,   // Karte ist eine 7 oder 8
    Spy,    // Karte ist eine 9 oder 10
    Swap    // Karte ist eine 11 oder 12
}


public class CardManager : MonoBehaviour
{
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private GameObject _spawnCardDeckPos;
    [SerializeField] private GameObject _spawnCardPlayerPos;
    [SerializeField] private GameObject _spawnCardEnemyPos;
    [SerializeField] private GameObject _showDrawnCardPos;
    [SerializeField] private GameObject _graveyardPos;
    [SerializeField] private GameObject _playerDrawnCardPos;

    private NetworkCardManager _networkCardManager;

    //EnmeyPanel
    [SerializeField] private PlayerUIController _enemyUIController;


    public static event Action ShowDiscardAndExchangeButtonEvent;
    public static event Action HidePlayerButtonEvent;
    public static event Action EndTurnEvent;
    public static event Action AllCardsAreFlippedBackEvent;
    public static event Action<string> ShowActionsButtonEvent;
    public static event Action<Card.DeckType, bool> SetEnemyCardInteractableStateEvent;
    public static event Action ResetCardsStateEvent;

    public static int flippedCardCount;
    public static bool allCardsAreFlippedBack;


    public int topCardNumber = -1;

    [SerializeField] private CardStack _cardStack;

    [SerializeField] private GameObject _cardDeckCard;
    [SerializeField] private GameObject _graveyardCard;
    [SerializeField] private GameObject _drawnCard;

    [SerializeField] private bool[] _playerClickedCards;
    [SerializeField] private bool[] _enemyClickedCards;
    [SerializeField] private bool[] _flippedCards;

    public SpecialAction currentAction = SpecialAction.None; // Standardmäßig keine Aktion

    public AudioManager audioManager;


    // Start is called before the first frame update
    void Start()
    {
        _networkCardManager = FindObjectOfType<NetworkCardManager>();

        _playerClickedCards = new bool[4];
        _enemyClickedCards = new bool[4];
        _flippedCards = new bool[4];

        // Für den Start, wenn ein Spieler sich zwei seiner Karten angucken darf
        flippedCardCount = 0;
        allCardsAreFlippedBack = false;

        if (NetworkManager.Singleton.IsServer)
        {
            _cardStack = new CardStack();
            _cardStack.CreateDeck();
            _cardStack.ShuffleCards();
        }

        ButtonController.DiscardButtonClickedEvent += MovePlayerDrawnCardToGraveyardPos;

        CardController.OnGraveyardCardClickedEvent += MoveGraveyardCardToPlayerPos;
        CardController.OnPlayerCardClickedEvent += SetPlayerClickedCardIndexAndOutline;
        CardController.OnEnemyCardClickedEvent += SetEnemyClickedCardIndexAndOutline;
        CardController.OnCardFlippedEvent += SetFlippedCard;
        CardController.OnCardFlippedBackEvent += CardFlippedBack;
        GameManager.UpdateEnemyCardsEvent += UpdateEnemyCardNumbers;
    }

    private void OnDestroy()
    {
        ButtonController.DiscardButtonClickedEvent -= MovePlayerDrawnCardToGraveyardPos;

        CardController.OnGraveyardCardClickedEvent -= MoveGraveyardCardToPlayerPos;
        CardController.OnPlayerCardClickedEvent -= SetPlayerClickedCardIndexAndOutline;
        CardController.OnEnemyCardClickedEvent -= SetEnemyClickedCardIndexAndOutline;
        CardController.OnCardFlippedEvent -= SetFlippedCard;
        CardController.OnCardFlippedBackEvent -= CardFlippedBack;
        GameManager.UpdateEnemyCardsEvent -= UpdateEnemyCardNumbers;
    }

    public int DrawTopCard()
    {
        return _cardStack.DrawTopCard();
    }

    /// <summary>
    /// Setzt das Array _playerClickedCards und die Outline der Karte 
    /// am gleichen Index
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    public void SetPlayerClickedCardIndexAndOutline(bool isSelected, int index)
    {
        _playerClickedCards[index] = isSelected;

        SetPlayerCardOutline(isSelected, index);
    }


    /// <summary>
    /// Setzt das Array _enemyClickedCards und die Outline der Karte 
    /// am gleichen Index
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    public void SetEnemyClickedCardIndexAndOutline(bool isSelected, int index)
    {
        _enemyClickedCards[index] = isSelected;

        SetEnemyCardOutline(isSelected, index);
    }

    /// <summary>
    /// Updated den Selektierzustand von einer Player Karte. Je nachdem ob die Karte geklickt worden ist oder nicht
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    public void SetPlayerCardOutline(bool isSelected, int index)
    {
        GameObject card = _spawnCardPlayerPos.transform.GetChild(index).gameObject;
        CardController controller = card.GetComponent<CardController>();
        controller.SetOutlineForLocalPlayer(isSelected);
    }

    /// <summary>
    /// Updated den Selektierzustand von einer Enemy Karte. Je nachdem ob die Karte geklickt worden ist oder nicht
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    public void SetEnemyCardOutline(bool isSelected, int index)
    {
        GameObject card = _spawnCardEnemyPos.transform.GetChild(index).gameObject;
        CardController controller = card.GetComponent<CardController>();
        controller.SetOutlineForLocalPlayer(isSelected);
    }

    public bool[] GetPlayerClickedCards()
    {
        return _playerClickedCards;
    }

    public int GetDrawnCardNumber()
    {
        CardController controller = _drawnCard.GetComponent<CardController>();
        return controller.CardNumber;
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
                        Card.DeckType.PLAYERCARD, true, true, true);

            // Spawned die Gegnerkarten
            SpawnCard(99, _spawnCardEnemyPos, _spawnCardEnemyPos.transform,
                        Card.DeckType.ENEMYCARD, true, false, false);
        }
    }

    /// <summary>
    /// Updated den Hoverzustand von einer Enemy Karte. Je nachdem ob der Mauszeiger auf der Karte ist oder nicht
    /// </summary>
    /// <param name="scaleby"></param>
    /// <param name="index"></param>
    public void SetPlayerCardHoverEffect(Vector3 scaleBy, int index)
    {
        GameObject card = _spawnCardPlayerPos.transform.GetChild(index).gameObject;
        card.transform.localScale = scaleBy;
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
    /// Updated den Hoverzustand der Graveyard Karte. Je nachdem ob der Mauszeiger auf der Karte ist oder nicht
    /// </summary>
    /// <param name="scaleby"></param>
    public void SetGraveyardCardHoverEffect(Vector3 scaleBy)
    {
        GameObject card = _graveyardPos.transform.GetChild(0).gameObject;
        card.transform.localScale = scaleBy;
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
    /// 
    /// Der Ausdruck 0.5f - target.pivot.y funktioniert wie folgt:
    /// Wenn target.pivot.y bei 0.5 liegt (Mitte): 0.5f - 0.5f = 0. Kein Offset ist nötig, weil der Pivot bereits in der Mitte liegt.
    /// Wenn target.pivot.y bei 0 liegt (unten): 0.5f - 0 = 0.5. Der Offset verschiebt die Position nach oben, um den unteren Pivot so anzupassen, dass es aussieht, als wäre er in der Mitte.
    /// Wenn target.pivot.y bei 1 liegt (oben): 0.5f - 1 = -0.5. Der Offset verschiebt die Position nach unten, um die obere Kante so anzupassen, dass es aussieht, als wäre der Pivot in der Mitte.
    /// Warum der Offset notwendig ist
    /// 
    /// Wenn wir die Höhe des RectTransform berücksichtigen, indem wir sie mit diesem Offset multiplizieren (height* (0.5f - target.pivot.y)), erhalten wir den notwendigen Abstand in lokalen 
    /// Koordinaten:
    /// Ein Wert von +0.5 * height verschiebt das Objekt um die halbe Höhe nach oben.
    /// Ein Wert von -0.5 * height verschiebt es um die halbe Höhe nach unten.
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
        // Spawned eine neue Karte vom Kartenstapel für das Graveyard
        _graveyardCard = SpawnCard(cardNumber, _spawnCardDeckPos, _spawnCardDeckPos.transform.parent,
                                    Card.DeckType.GRAVEYARD, true, true, isSelectable);

        // Setzt den Parent zuerst vom Table, damit die Karte über dem Kartenstapel ist
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

    private GameObject SpawnCard(int cardNumber, GameObject targetPos, Transform parent, Card.DeckType corresDeck, bool backCardIsVisible, bool canHover, bool isSelectable)
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
        _cardDeckCard = SpawnCard(cardNumber, _spawnCardDeckPos, _spawnCardDeckPos.transform.parent, Card.DeckType.CARDDECK, true, false, false);

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
    /// Mit dieser Funktion wird eine Karte zu einem gewünschten Ziel bewegt.
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
    /// Ist für die 
    /// </summary>
    /// <param name="objectToMove"></param>
    /// <param name="newObject"></param>
    private void SetCardToDrawnCard(GameObject objectToMove, bool showButton)
    {
        // Überschreibt die bewegte Karte auf die _drawnCard
        _drawnCard = objectToMove;

        // Guckt welche Karte bewegt worden ist und löscht diese im Anschluss
        CardController controllerDrawnCard = _drawnCard.GetComponent<CardController>();
        Card.DeckType oldCorresDeck = controllerDrawnCard.GetCorrespondingDeck();

        // Löscht das GameObjekt ab, welches vorher bewegt worden ist
        if (oldCorresDeck == Card.DeckType.GRAVEYARD)
        {
            _graveyardCard = null;
        }
        else
        {
            _cardDeckCard = null;
        }

        // Ändert das correspondingDeck von der _drawnCard
        controllerDrawnCard.SetCorrespondingDeck(Card.DeckType.DRAWNCARD);

        CheckForCardActionButtons(showButton, controllerDrawnCard, oldCorresDeck);
    }


    private void CheckForCardActionButtons(bool showButton, CardController controllerDrawnCard, Card.DeckType oldCorresDeck)
    {
        // Guckt, ob die Buttons zum Abwerfen oder Tauschen angezeigt werden sollen
        if (showButton)
        {
            ShowDiscardAndExchangeButtonEvent?.Invoke();

            int cardNumber = controllerDrawnCard.CardNumber;

            if (oldCorresDeck == Card.DeckType.CARDDECK)
            {
                // Überprüft ob die neu gespawnte Karte eine 7 oder 8 ist, weil dann eine spezielle 
                // Aktion ausgeführt werden kann
                if (cardNumber == 7 || cardNumber == 8)
                {
                    currentAction = SpecialAction.Peak;
                    ShowActionsButtonEvent?.Invoke("Peak");
                }
                else if (cardNumber == 9 || cardNumber == 10)
                {
                    currentAction = SpecialAction.Spy;
                    ShowActionsButtonEvent?.Invoke("Spy");
                    SetEnemyCardInteractableStateEvent?.Invoke(Card.DeckType.ENEMYCARD, true);
                }
                else if (cardNumber == 11 || cardNumber == 12)
                {
                    currentAction = SpecialAction.Swap;
                    ShowActionsButtonEvent?.Invoke("Swap");
                    SetEnemyCardInteractableStateEvent?.Invoke(Card.DeckType.ENEMYCARD, true);
                }
                else
                {
                    currentAction = SpecialAction.None; // Keine spezielle Aktion für andere Karten
                }
            }
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
        controller.SetInteractableState(false);
    }


    /////////////////////////////////////////////////////////////////////////////
    // Bewegung der DrawnCard zum Graveyard

    public void MovePlayerDrawnCardToGraveyardPos()
    {
        CardController controller = _drawnCard.GetComponent<CardController>();
        MoveDrawnCardToGraveyardPos(controller.CardNumber);
    }

    public void MoveEnemyDrawnCardToGraveyardPos(int cardNumber)
    {
        MoveDrawnCardToGraveyardPos(cardNumber);
    }


    public void MoveDrawnCardToGraveyardPos(int cardNumber)
    {
        ResetCardsStateEvent?.Invoke();

        // Reseted die Array für die angeklickten Karten des Spielers und des Enemys
        ResetClickedCards(_playerClickedCards);
        ResetClickedCards(_enemyClickedCards);

        Vector3 targetPos = GetCenteredPosition(_graveyardPos.transform);

        // Bewegt die Karte zum Graveyard
        LeanTween.move(_drawnCard, targetPos, 0.5f).setOnComplete(() =>
        {
            _drawnCard.transform.SetParent(_graveyardPos.transform);
            CardController controller = _drawnCard.GetComponent<CardController>();
            controller.CardNumber = cardNumber;
            if (controller.GetCardBackImageVisibility())
            {
                controller.FlipCardAnimation(false);
            }

            SetCardToGraveyardCard(_drawnCard);
            _drawnCard = null;
            EndTurnEvent?.Invoke();
        });
    }

    /// <summary>
    /// Überträgt das _drawnCard GameObject auf das _graveyardCard GameObject
    /// Darüber hinaus stellt es einen Grundzustand von der Karte her. Heißt sie ist nicht
    /// selektiert, sie ist umgedreht (man sieht die Zahl) und das correspondingDeck
    /// ist GRAVEYARD
    /// </summary>
    private void SetCardToGraveyardCard(GameObject objectToMove)
    {
        _graveyardCard = objectToMove;

        CardController controller = _graveyardCard.GetComponent<CardController>();
        controller.SetCorrespondingDeck(Card.DeckType.GRAVEYARD);
    }

    /////////////////////////////////////////////////////////////////////////////
    // Bewegung der DrawnCard zum Spieler

    /// <summary>
    /// Überprüft, ob eine Karte geklickt worden ist
    /// </summary>
    /// <returns></returns>
    public bool IsAnyCardSelected()
    {
        foreach (bool value in _playerClickedCards)
        {
            if (value)
            {
                return true; // Sofort beenden, wenn ein true gefunden wurde
            }
        }
        return false; // Kein true gefunden
    }

    /// <summary>
    /// Überprüft, ob alle angeklickten Karten gleich sind
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    public bool AreSelectedCardsEqual(int[] cards)
    {
        int? referenceValue = null;

        for (int i = 0; i < cards.Length; i++)
        {
            if (_playerClickedCards[i])
            {
                if (referenceValue == null)
                {
                    referenceValue = cards[i];
                }
                else if (cards[i] != referenceValue)
                {
                    Debug.Log("Die angeklickten Karten sind nicht gleich. Gezogene Karte wird abgelegt.");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Findet den ersten Index, der true ist
    /// </summary>
    /// <param name="clickedCards"></param>
    /// <returns></returns>
    private int FindFirstTrueIndex(bool[] _clickedCards)
    {
        for (int i = 0; i < _clickedCards.Length; i++)
        {
            if (_clickedCards[i])
            {
                return i; // Gibt den Index des ersten Elements zurück, das true ist
            }
        }
        return -1; // Gibt -1 zurück, wenn kein true gefunden wurde
    }

    /// <summary>
    /// Aktualisiert die abgespeicherte Liste der Karten im GameManager wenn Karten 
    /// ausgetauscht werden. Vor allem wichtig, wenn mehr als eine Karte getauscht wird
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    public int[] UpdatePlayerCards(int[] cards)
    {
        List<int> newCardsList = new List<int>();
        bool addedClickedCard = false; // Verfolgt, ob bereits eine geklickte Karte hinzugefügt wurde

        for (int i = 0; i < cards.Length; i++)
        {
            if (!_playerClickedCards[i])
            {
                // Füge alle nicht geklickten Karten hinzu
                newCardsList.Add(cards[i]);
            }
            else if (!addedClickedCard)
            {
                // Füge die gezogene Karte hinzu, wenn noch keine hinzugefügt wurde
                CardController controller = _drawnCard.GetComponent<CardController>();
                newCardsList.Add(controller.CardNumber);
                addedClickedCard = true;
            }
        }

        return newCardsList.ToArray();
    }

    public void ExchangeEnemyCards(bool[] clickedCards, int[] cards)
    {
        ExchangeCards(_spawnCardEnemyPos, clickedCards, cards);
    }

    public void ExchangePlayerCards(int[] cards)
    {
        ExchangeCards(_spawnCardPlayerPos, _playerClickedCards, cards);
    }


    public void ExchangeCards(GameObject playerPanel, bool[] clickedCards, int[] cards)
    {
        ResetCardsStateEvent?.Invoke();
        MovePlayerCardsToGraveyardPos(playerPanel, clickedCards, cards);
        MoveDrawnCardToTarget(playerPanel, clickedCards);
    }

    public void MovePlayerCardsToGraveyardPos(GameObject playerPanel, bool[] clickedCards, int[] cards)
    {
        // Erste Karte finden, die geklickt wurde
        int index = FindFirstTrueIndex(clickedCards);
        int cardNumber = cards[index];

        Vector3 targetPos = GetCenteredPosition(_graveyardPos.transform);

        for (int i = 0; i < clickedCards.Length; i++)
        {
            // Wenn die Karte nicht angeklickt worden ist, gehe zum nächsten Schleifenelement
            if (!clickedCards[i]) { continue; }

            GameObject _selectedCard = playerPanel.transform.GetChild(i).gameObject;
            CardController controller = _selectedCard.GetComponent<CardController>();
            controller.SetOutlineForLocalPlayer(false);
            controller.CardNumber = cardNumber;

            // Bewegt die Karte zum Graveyard
            LeanTween.move(_selectedCard, targetPos, 0.5f).setOnComplete(() =>
            {
                _selectedCard.transform.SetParent(_graveyardPos.transform);

                if (controller.GetCardBackImageVisibility())
                {
                    controller.FlipCardAnimation(false);
                }

                SetCardToGraveyardCard(_selectedCard);
            });
        }
    }

    private void MoveDrawnCardToTarget(GameObject playerPanel, bool[] clickedCards)
    {
        // Erste Karte finden, die geklickt wurde
        int index = FindFirstTrueIndex(clickedCards);
        GameObject _firstSelectedCard = playerPanel.transform.GetChild(index).gameObject;

        // Herausfinden, ob man der aktuelle Spieler ist, damit man sagen kann, wie der Bogen bei der Bewegung sein soll
        int rotation;
        bool isCurrentPlayer = GameManager.Instance.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId;

        rotation = isCurrentPlayer ? 1 : -1;

        Vector3[] points = MoveInCircle.CalculateCircle(8, _drawnCard.transform, _firstSelectedCard.transform, rotation, 100);
        LeanTween.moveSpline(_drawnCard, points, 0.5f).setOnComplete(() =>
        {
            CardController controller = _drawnCard.GetComponent<CardController>();
            Card.DeckType corresDeck = isCurrentPlayer ? Card.DeckType.PLAYERCARD : Card.DeckType.ENEMYCARD;

            controller.SetCorrespondingDeck(corresDeck);
            controller.FlipCardAnimation(true);

            // Parent der bewegten Karte zu dem spezifischen Playerpanel setzen und an die richtige Position
            _drawnCard.transform.SetParent(playerPanel.transform);
            _drawnCard.transform.SetSiblingIndex(index);

            // Gezogene Karte intern löschen und den Zug beenden
            _drawnCard = null;

            // Reseted die Array für die angeklickten Karten des Spielers und des Enemys
            ResetClickedCards(_playerClickedCards);
            ResetClickedCards(_enemyClickedCards);

            LeanTween.delayedCall(0.6f, () =>
            {
                EndTurnEvent?.Invoke();
            });
        });
    }

    private void ResetClickedCards(bool[] clickedCards)
    {
        Array.Fill(clickedCards, false);
    }

    ////////////////////////////////////////////////////////////////////////////

    private void UpdateEnemyCardNumbers(Player player)
    {
        if (player.id != _enemyUIController.GetLocalPlayerId()) return;

        int cardsCount = _spawnCardEnemyPos.transform.childCount;
        for (int i = 0; i < cardsCount; i++)
        {
            GameObject card = _spawnCardEnemyPos.transform.GetChild(i).gameObject;
            CardController controller = card.GetComponent<CardController>();
            controller.CardNumber = player.cards[i];
        }
    }

    ////////////////////////////////////////////////////////////////////////////

    private void CardFlippedBack(int flippedCardCount, bool isFlipped, int index)
    {
        SetFlippedCard(isFlipped, index);
        CheckIfAllCardsAreFlippedBack(flippedCardCount);
    }

    private void CheckIfAllCardsAreFlippedBack(int flippedCardCount)
    {
        bool hasFlippedCards = false;
        foreach (var card in _flippedCards)
        {
            if (card)
            {
                hasFlippedCards = true;
            }
        }

        if (!hasFlippedCards && flippedCardCount == 2)
        {
            // Speichert, dass alle Karten angeguckt und wieder umgedreht worden sind
            allCardsAreFlippedBack = true;

            AllCardsAreFlippedBackEvent?.Invoke();
        }
    }

    private void SetFlippedCard(bool isFlipped, int index)
    {
        _flippedCards[index] = isFlipped;
    }

    ////////////////////////////////////////////////////////////////////////////
    // Spezielle Aktionen wie Peak, Spy oder Swap
    public void ActionButtonClicked()
    {

        switch (currentAction)
        {
            case SpecialAction.Peak:
                // Peak-Aktion durchführen (Karte 7 oder 8)
                HandlePeakAction();
                break;

            case SpecialAction.Spy:
                // Spy-Aktion durchführen (Karte 9 oder 10)
                Debug.Log("Spy Button geklickt");

                (int clickedCardIndex, bool isSingleCardSelected) = CheckClickedCards(_enemyClickedCards);

                if (isSingleCardSelected)
                {
                    _networkCardManager.OnSpyButtonClickedServerRpc(NetworkManager.Singleton.LocalClientId,
                        _enemyUIController.GetLocalPlayerId(), clickedCardIndex);
                }

                break;

            case SpecialAction.Swap:
                // Swap-Aktion durchführen (Karte 11 oder 12)
                (int playerClickedCardIndex, bool isSinglePlayerCardSelected) = CheckClickedCards(_playerClickedCards);
                (int enemyClickedCardIndex, bool isSingleEnemyCardSelected) = CheckClickedCards(_enemyClickedCards);

                if (isSinglePlayerCardSelected && isSingleEnemyCardSelected)
                {
                    _networkCardManager.OnSwapButtonClicked(NetworkManager.Singleton.LocalClientId,
                         _enemyUIController.GetLocalPlayerId(), playerClickedCardIndex, enemyClickedCardIndex);
                }
                break;

            case SpecialAction.None:
                // Keine Aktion ausführen
                break;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // Peak Aktion

    private void HandlePeakAction()
    {
        (int clickedCardIndex, bool isSingleCardSelected) = CheckClickedCards(_playerClickedCards);

        if (isSingleCardSelected)
        {
            HidePlayerButtonEvent?.Invoke();
            ResetCardsStateEvent?.Invoke();

            currentAction = SpecialAction.None;

            // Dreht die Karte angeklickte Karte um lässt sie zwei Sekunden umgedreht und dreht sie 
            // im Anschluss wieder um und beendet den Zug
            GameObject card = _spawnCardPlayerPos.transform.GetChild(clickedCardIndex).gameObject;

            audioManager.PlayPeakSound();

            StartCoroutine(DoPeakOrSpyMoving(card, clickedCardIndex,"Peak", false));
        }
    }

    public void SetSpecialActionImageAndText(int index, string specialActionText, bool visibility, bool isSpyAction)
    {
        GameObject card;

        if (isSpyAction)
        {
            card = _spawnCardPlayerPos.transform.GetChild(index).gameObject;
            if (visibility)
            {
                audioManager.PlaySpySound();
            } 
        }
        else
        {
            card = _spawnCardEnemyPos.transform.GetChild(index).gameObject;
            if (visibility)
            {
                audioManager.PlayPeakSound();
            }
        }

        CardController controller = card.GetComponent<CardController>();
        controller.SetSpecialActionImageVisibility(visibility);
        controller.SetSpecialActionText(specialActionText);
    }

    ////////////////////////////////////////////////////////////////////////////
    // Spy Aktion

    public void HandleSpyAction(int cardNumber)
    {
        (int clickedCardIndex, bool isSingleCardSelected) = CheckClickedCards(_enemyClickedCards);

        if (isSingleCardSelected)
        {
            HidePlayerButtonEvent?.Invoke();
            ResetCardsStateEvent?.Invoke();

            currentAction = SpecialAction.None;

            // Dreht die Karte angeklickte Karte um lässt sie zwei Sekunden umgedreht und dreht sie 
            // im Anschluss wieder um und beendet den Zug
            GameObject card = _spawnCardEnemyPos.transform.GetChild(clickedCardIndex).gameObject;
            CardController controller = card.GetComponent<CardController>();
            controller.CardNumber = cardNumber;

            audioManager.PlaySpySound();

            StartCoroutine(DoPeakOrSpyMoving(card, clickedCardIndex,"Spy", true));
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // Swap Aktion

    public void HandleSwapAction(int cardNumber, bool enableReturnToGraveyardEvent)
    {
        (int playerClickedCardIndex, bool isSinglePlayerCardSelected) = CheckClickedCards(_playerClickedCards);
        (int enemyClickedCardIndex, bool isSingleEnemyCardSelected) = CheckClickedCards(_enemyClickedCards);

        if (isSinglePlayerCardSelected && isSingleEnemyCardSelected)
        {
            HidePlayerButtonEvent?.Invoke();
            ResetCardsStateEvent?.Invoke();

            currentAction = SpecialAction.None;

            // Updated die Kartennummer der neuen Enemykarte
            GameObject playerCard = _spawnCardPlayerPos.transform.GetChild(playerClickedCardIndex).gameObject;
            CardController playerController = playerCard.GetComponent<CardController>();
            playerController.CardNumber = 99;
            playerController.SetCorrespondingDeck(Card.DeckType.ENEMYCARD);

            // Updated die Kartennummer der neuen Spielerkarte
            GameObject enemyCard = _spawnCardEnemyPos.transform.GetChild(enemyClickedCardIndex).gameObject;
            CardController enemyController = enemyCard.GetComponent<CardController>();
            enemyController.CardNumber = cardNumber;
            enemyController.SetCorrespondingDeck(Card.DeckType.PLAYERCARD);

            audioManager.PlaySwapSound();

            StartCoroutine(DoSwapMoving(playerCard, enemyCard, enableReturnToGraveyardEvent));
        }
    }

    ////////////////////////////////////////////////////////////////////////////

    private (int clickedCardIndex, bool isSingleCardSelected) CheckClickedCards(bool[] clickedCards)
    {
        int trueCount = 0;
        int clickedCardIndex = 0;
        bool isSingleCardSelected;

        // Zählt wie viele Karten angeklickt worden sind und merkt sich die letzte
        // angeklickte Karte. Diese ist nur wichtig, wenn nur eine Karte angeklickt
        // worden ist
        for (int i = 0; i < clickedCards.Length; i++)
        {
            if (clickedCards[i])
            {
                clickedCardIndex = i;
                trueCount++;
                Debug.Log("TrueCount: " + trueCount + "; Index: " + i);
            }
        }

        // Wertet aus, wie viele Karten angeklickt wurden und führt dementsprechend 
        // eine Aktion aus
        if (trueCount == 0)
        {
            Debug.Log("Du musst eine Karte selektieren");
            isSingleCardSelected = false;
        }
        else if (trueCount > 1)
        {
            Debug.Log("Du darfst nur eine Karte selektieren");
            isSingleCardSelected = false;
        }
        else
        {
            isSingleCardSelected = true;
        }

        return (clickedCardIndex, isSingleCardSelected);
    }

    // Bewegung, um die angeklickte Karte umzudrehen, wieder zurück zu drehen
    // und anschließend die gezogene Karte abzulegen
    IEnumerator DoPeakOrSpyMoving(GameObject card, int clickedCardIndex, string specialActionText, bool isSpyAction)
    {
        _networkCardManager.HightlightPeakedCardForEnemyClientRpc(clickedCardIndex, specialActionText, true, isSpyAction);

        CardController controller = card.GetComponent<CardController>();

        LeanTween.rotateY(card, 90.0f, 0.25f);
        yield return new WaitForSeconds(0.25f);

        controller.SetCardBackImageVisibility(false);
        LeanTween.rotateY(card, 0.0f, 0.25f);
        yield return new WaitForSeconds(2.25f);

        LeanTween.rotateY(card, 90.0f, 0.25f);
        yield return new WaitForSeconds(0.25f);

        controller.SetCardBackImageVisibility(true);
        LeanTween.rotateY(card, 0.0f, 0.25f);
        yield return new WaitForSeconds(0.25f);

        MovePlayerDrawnCardToGraveyardPos();
        _networkCardManager.MoveEnemyCardToGraveyardPos();

        _networkCardManager.HightlightPeakedCardForEnemyClientRpc(clickedCardIndex, specialActionText, false, isSpyAction);

        if (isSpyAction)
        {
            controller.CardNumber = 99;
        }
    }


    IEnumerator DoSwapMoving(GameObject playerCard, GameObject enemyCard, bool enableReturnToGraveyardEvent)
    {
        Vector3[] playerPoints = MoveInCircle.CalculateCircle(8, playerCard.transform, enemyCard.transform, 1, 100);
        Vector3[] enemyPoints = MoveInCircle.CalculateCircle(8, enemyCard.transform, playerCard.transform, 1, 100);

        GameObject placeholderPlayerCard = SpawnPlaceholder(playerCard);
        GameObject placeholderEnemyCard = SpawnPlaceholder(enemyCard);

        playerCard.transform.SetParent(playerCard.transform.root);
        enemyCard.transform.SetParent(enemyCard.transform.root);

        LeanTween.moveSpline(playerCard, playerPoints, 1f);
        LeanTween.moveSpline(enemyCard, enemyPoints, 1f);

        yield return new WaitForSeconds(1f);

        playerCard.transform.SetParent(placeholderEnemyCard.transform.parent);
        playerCard.transform.SetSiblingIndex(placeholderEnemyCard.transform.GetSiblingIndex());

        enemyCard.transform.SetParent(placeholderPlayerCard.transform.parent);
        enemyCard.transform.SetSiblingIndex(placeholderPlayerCard.transform.GetSiblingIndex());

        Destroy(placeholderPlayerCard);
        Destroy(placeholderEnemyCard);

        yield return new WaitForSeconds(1f);

        if (enableReturnToGraveyardEvent)
        {
            MovePlayerDrawnCardToGraveyardPos();
            _networkCardManager.MoveEnemyCardToGraveyardPos();
        }
    }

    public GameObject SpawnPlaceholder(GameObject original)
    {
        // Erstelle eine Kopie des Original-GameObjects
        GameObject placeholder = Instantiate(original, original.transform.position, original.transform.rotation);

        placeholder.transform.SetParent(original.transform.parent);
        placeholder.transform.SetSiblingIndex(original.transform.GetSiblingIndex());
        placeholder.transform.localScale = Vector3.one;

        // Mache es unsichtbar, indem du das Alpha der CavasGroup auf 0 setzt
        if (placeholder.TryGetComponent<CanvasGroup>(out CanvasGroup cavasGroup))
        {
            cavasGroup.alpha = 0;
        }

        return placeholder; // Gibt das neue Platzhalter-Objekt zurück
    }


    
    ////////////////////////////////////////////////////////////////////
    
    public void ShakePlayerCardOnInvalidCardMatch()
    {
        ShakeCardOnInvalidCardMatch(_spawnCardPlayerPos, _playerClickedCards);
    }

    public void ShakeEnemyCardOnInvalidCardMatch()
    {
        ShakeCardOnInvalidCardMatch(_spawnCardEnemyPos, _enemyClickedCards);
    }


    private void ShakeCardOnInvalidCardMatch(GameObject playerPanel, bool[] clickedCards)
    {
        for (int i = 0; i < clickedCards.Length; i++)
        {
            // Wenn die Karte nicht angeklickt worden ist, gehe zum nächsten Schleifenelement
            if (!clickedCards[i]) { continue; }

            GameObject _selectedCard = playerPanel.transform.GetChild(i).gameObject;
            CardController controller = _selectedCard.GetComponent<CardController>();

            controller.ShakeOnInvalidCardMatch();
        }
    }

}

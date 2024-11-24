using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class CardManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private GameObject _spawnCardDeckPos;
    [SerializeField] private GameObject _spawnCardPlayerPos;
    [SerializeField] private GameObject _spawnCardEnemyPos;
    [SerializeField] private GameObject _showDrawnCardPos;
    [SerializeField] private GameObject _graveyardPos;
    [SerializeField] private GameObject _playerDrawnCardPos;

    [Header("Interne Variablen")]
    [SerializeField] private GameObject _cardDeckCard;
    [SerializeField] private GameObject _graveyardCard;

    public int topCardNumber = -1;

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

        CardController.OnGraveyardCardClicked += MoveGraveyardCardToPlayerPos;
    }

    private void OnDestroy()
    {
        CardController.OnGraveyardCardClicked -= MoveGraveyardCardToPlayerPos;
    }

    public void SpawnAndMoveCardToDrawnCardPos(int cardNumber, Transform target, bool flipAtDestination)
    {
        // Spawned eine neue Karte vom Kartenstapel
        _cardDeckCard = SpawnCard(cardNumber, _spawnCardDeckPos, _spawnCardDeckPos.transform.parent,
                                    Card.Stack.CARDDECK, true, false, false);

        if (flipAtDestination)
        {
            // Schritt 1: Karte anheben, leicht vergr��ern und umdrehen
            FlipAndMoveCard(_cardDeckCard, target);
        }
        else
        {
            MoveToTargetPosition(_cardDeckCard, target);
        }
    }

    private void FlipAndMoveCard(GameObject objectToMove, Transform target)
    {
        CardController controller = objectToMove.GetComponent<CardController>();

        LeanTween.move(objectToMove, _showDrawnCardPos.transform, 0.5f);
        LeanTween.scale(objectToMove, Vector3.one * 1.2f, 0.5f).setEase(LeanTweenType.easeOutQuad);

        LeanTween.rotateX(objectToMove, 90.0f, 0.25f).setOnComplete(() =>
        {
            controller.SetCardBackImageVisibility(false);
            LeanTween.rotateX(objectToMove, 0.0f, 0.25f).setOnComplete(() =>
            {
                LeanTween.delayedCall(0.5f, () =>
                {
                    MoveToTargetPosition(objectToMove, target);
                });
            });
        });
    }

    private void MoveToTargetPosition(GameObject objectToMove, Transform target)
    {
        // Schritt 2: Karte zum Ziel bewegen und zur�ck auf Originalgr��e skalieren
        Vector3 targetPos = GetCenteredPosition(target);

        LeanTween.move(objectToMove, targetPos, 0.5f).setEase(LeanTweenType.easeInOutQuad);
        LeanTween.scale(objectToMove, Vector3.one, 0.5f).setEase(LeanTweenType.easeInOutQuad).setOnComplete(() =>
        {
            objectToMove.transform.SetParent(target);
        });
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

        // Berechne die Verschiebung basierend auf Anker und Pivot
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        Vector3 centerOffset = new Vector3(width * (0.5f - rectTransform.pivot.x), height * (0.5f - rectTransform.pivot.y), 0);

        // Berechne und gib die Zielposition in Weltkoordinaten zur�ck
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
    public void SetEnemyCardHoverEffect(Vector3 scaleby, int index)
    {
        GameObject card = _spawnCardEnemyPos.transform.GetChild(index).gameObject;
        card.transform.localScale = scaleby;
    }

    /// <summary>
    /// Updated den Selektierzustand von einer Enemy Karte. Je nachdem ob der Mauszeiger auf der Karte ist oder nicht
    /// </summary>
    /// <param name="isSelected"></param>
    /// <param name="index"></param>
    public void SetEnemyCardClick(bool isSelected, int index)
    {
        GameObject card = _spawnCardEnemyPos.transform.GetChild(index).gameObject;

        Outline outline = card.GetComponent<Outline>();

        if (outline == null)
        {
            Debug.Log("Das Object " + name + " hat keine Komponente Outline");
            return;
        }

        outline.enabled = isSelected;
    }

    public void SpawnAndMoveGraveyardCard(int cardNumber, bool isSelectable)
    {
        Debug.Log("Ich will eine Graveyard Karte spawnen");

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

    /// <summary>
    /// Bewegt die Graveyardkarte zum Spieler, der auch auf die Karte gedr�ckt hat
    /// </summary>
    private void MoveGraveyardCardToPlayerPos()
    {
        MoveGraveyardCardToDrawnPos(_playerDrawnCardPos.transform);
    }

    /// <summary>
    /// Allgemeine Funktion zum Bewegen der Graveyardkarte zu einer bestimmten Position
    /// </summary>
    /// <param name="target"></param>
    public void MoveGraveyardCardToDrawnPos(Transform target)
    {
        MoveToTargetPosition(_graveyardCard, target);

        CardController controller = _graveyardCard.GetComponent<CardController>();
        controller.isSelectable = false;
    }
}

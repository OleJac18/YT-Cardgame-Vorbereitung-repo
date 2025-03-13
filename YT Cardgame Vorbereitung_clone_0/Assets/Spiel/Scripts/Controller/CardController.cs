using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI numberTextTopLeft;
    [SerializeField] private TextMeshProUGUI numberTextBottomRight;
    [SerializeField] private GameObject cardBackImage;
    [SerializeField] private Card _card;

    public static event Action<Vector3, int, Card.DeckType> OnPlayerOrEnemyCardHoveredEvent;
    public static event Action<Vector3> OnGraveyardHoveredEvent;
    public static event Action<bool, int> OnPlayerCardClickedEvent;
    public static event Action<bool, int> OnEnemyCardClickedEvent;
    public static event Action OnGraveyardCardClickedEvent;
    public static event Action<bool, int> OnCardFlippedEvent;
    public static event Action<int, bool, int> OnCardFlippedBackEvent;

    public bool canHover = false;
    public bool isSelectable = false;

    public bool isFirstRound = true;

    private Outline _outline;
    private Vector3 _originalScale;
    private Vector3 _hoverScale;

    private const int MAXFLIPPEDCARDS = 2;


    private bool _isFlipped;
    [SerializeField]

    private void Awake()
    {
        _outline = this.GetComponent<Outline>();
        _originalScale = Vector3.one;
        _hoverScale = new Vector3(1.1f, 1.1f, 1f);
        _card = new Card(13, Card.DeckType.NONE);
        _isFlipped = false;
    }

    private void Start()
    {
        GameManager.Instance.currentPlayerId.OnValueChanged += OnCurrentPlayerChanged;
        GameManager.FlipAllCardsEvent += FlipCardIfNotFlippedAtGameEnd;
        CardManager.AllCardsAreFlippedBackEvent += SetAllCardsAreFlippedBack;
        CardManager.ResetCardsStateEvent += ResetCardState;
        CardManager.SetEnemyCardInteractableStateEvent += SetInteractableStateForSpecificDeckType;
    }

    private void OnDestroy()
    {
        GameManager.Instance.currentPlayerId.OnValueChanged -= OnCurrentPlayerChanged;
        GameManager.FlipAllCardsEvent -= FlipCardIfNotFlippedAtGameEnd;
        CardManager.AllCardsAreFlippedBackEvent -= SetAllCardsAreFlippedBack;
        CardManager.ResetCardsStateEvent -= ResetCardState;
        CardManager.SetEnemyCardInteractableStateEvent -= SetInteractableStateForSpecificDeckType;

    }

    public int CardNumber
    {
        get { return _card.number; }
        set
        {
            if (_card.number != value)
            {
                _card.number = value;
                UpdateCardNumber();
            }
        }
    }

    private void UpdateCardNumber()
    {
        string cardNumber = _card.number.ToString();
        numberTextTopLeft.text = cardNumber;
        numberTextBottomRight.text = cardNumber;
    }

    public void SetCorrespondingDeck(Card.DeckType decktype)
    {
        _card.correspondingDeck = decktype;
    }

    public Card.DeckType GetCorrespondingDeck()
    {
        return _card.correspondingDeck;
    }

    public void SetCardBackImageVisibility(bool visible)
    {
        cardBackImage.SetActive(visible);
    }

    public bool GetCardBackImageVisibility()
    {
        return cardBackImage.activeSelf;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Wenn nicht gehovert werden darf, return
        if (!canHover) return;

        SetHoverState(_hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Wenn nicht gehovert werden darf, return
        if (!canHover) return;

        SetHoverState(_originalScale);
    }

    private void SetHoverState(Vector3 scale)
    {
        this.transform.localScale = scale;
        int index = this.transform.GetSiblingIndex();

        if (_card.correspondingDeck == Card.DeckType.GRAVEYARD)
        {
            OnGraveyardHoveredEvent?.Invoke(scale);
        }
        else if (_card.correspondingDeck == Card.DeckType.PLAYERCARD)
        {
            OnPlayerOrEnemyCardHoveredEvent?.Invoke(scale, index, Card.DeckType.ENEMYCARD);
        }
        else if (_card.correspondingDeck == Card.DeckType.ENEMYCARD)
        {
            OnPlayerOrEnemyCardHoveredEvent?.Invoke(scale, index, Card.DeckType.PLAYERCARD);
        }

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable) return;

        if (_card.correspondingDeck == Card.DeckType.GRAVEYARD)
        {
            OnGraveyardCardClickedEvent?.Invoke();
        }
        else
        {
            if (CardManager.flippedCardCount < MAXFLIPPEDCARDS && !_isFlipped && _card.correspondingDeck == Card.DeckType.PLAYERCARD)
            {
                FlipCardAnimation(_isFlipped);
                CardManager.flippedCardCount++;
                _isFlipped = true;

                // Speichert im CardManager ab, welche Karten umgedreht worden sind
                // Damit vom CardManager angegeben werden kann, ob die Karten weiter
                // hin umgedreht oder selektiert werden können
                int index = this.transform.GetSiblingIndex();
                OnCardFlippedEvent?.Invoke(true, index);
            }
            else if (_isFlipped && _card.correspondingDeck == Card.DeckType.PLAYERCARD)
            {
                FlipCardAnimation(_isFlipped);
                _isFlipped = false;

                // Speichert im CardManager ab, welche Karten umgedreht worden sind
                // Damit vom CardManager angegeben werden kann, ob die Karten weiter
                // hin umgedreht oder selektiert werden können
                int index = this.transform.GetSiblingIndex();
                OnCardFlippedBackEvent?.Invoke(CardManager.flippedCardCount, false, index);
            }
            else if (CardManager.flippedCardCount == 2 && CardManager.allCardsAreFlippedBack)
            {
                SelectionAnimation();
            }
        }
    }

    /// <summary>
    /// Reseted die Outline, den HoverState und den InteractableState 
    /// Outline = false; HoverState = _originalState; InteractableState = false
    /// </summary>
    private void ResetCardState()
    {
        // Setzt die Outline zurück
        SetOutlineForLocalPlayer(false);

        // Setzt den Scale zurück
        SetHoverState(_originalScale);

        // Setzt die Interactability zurück
        SetInteractableState(false);
    }

    private void SetAllCardsAreFlippedBack()
    {
        // Setzt den Selectable State der Karte, je nachdem ob der
        // Spieler am Zug ist oder nicht
        ulong currentPlayerId = GameManager.Instance.currentPlayerId.Value;
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        bool interactable = currentPlayerId == localClientId;

        SetInteractableState(interactable);

        SetHoverState(_originalScale);
    }

    private void SelectionAnimation()
    {
        if (_outline == null)
        {
            Debug.Log("Das Object " + name + " hat keine Komponente Outline");
            return;
        }

        _outline.enabled = !_outline.enabled;

        InvokeCardClickedEvent(_outline.enabled);
    }

    public void FlipCardAnimation(bool showCardBack)
    {
        LeanTween.rotateY(this.gameObject, 90.0f, 0.25f).setOnComplete(() =>
        {
            cardBackImage.SetActive(showCardBack);
            LeanTween.rotateY(this.gameObject, 0.0f, 0.25f);
        });
    }



    ///////////////////////////////////////////////////////////////////

    public void SetOutlineForLocalPlayer(bool visible)
    {
        _outline.enabled = visible;
    }

    public void SetOutlineForAllPlayers(bool visible)
    {
        _outline.enabled = visible;

        InvokeCardClickedEvent(_outline.enabled);
    }

    private void InvokeCardClickedEvent(bool isEnabled)
    {
        int index = this.transform.GetSiblingIndex();

        switch (_card.correspondingDeck)
        {
            case Card.DeckType.PLAYERCARD:
                OnPlayerCardClickedEvent?.Invoke(isEnabled, index);
                break;

            case Card.DeckType.ENEMYCARD:
                OnEnemyCardClickedEvent?.Invoke(isEnabled, index);
                break;
        }
    }

    private void OnCurrentPlayerChanged(ulong previousPlayerId, ulong currentPlayerId)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        bool interactable = currentPlayerId == localClientId;

        SetInteractableStateForSpecificDeckType(Card.DeckType.PLAYERCARD, interactable);
    }

    public void SetInteractableStateForSpecificDeckType(Card.DeckType cardDeckType, bool interactable)
    {
        if (_card.correspondingDeck != cardDeckType) return;
        SetInteractableState(interactable);
    }

    public void SetInteractableState(bool interactable)
    {
        isSelectable = interactable;
        canHover = interactable;
    }

    private void FlipCardIfNotFlippedAtGameEnd()
    {
        if (cardBackImage.activeSelf) // Nur umdrehen, wenn Rückseite sichtbar
        {
            FlipCardAnimation(false); // Hier den Parameter für "showCardBack" auf "false" setzen
        }
    }
}

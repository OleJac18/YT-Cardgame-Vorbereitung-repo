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

    public static event Action<Vector3, int> OnCardHovered; // Event für Hover-Aktionen
    public static event Action<bool, int> OnCardClicked;    // Event für Klick-Aktionen
    public static event Action OnGraveyardCardClicked;

    public bool canHover = false;
    public bool isSelectable = false;

    private Outline _outline;
    private Vector3 _originalScale;
    private Vector3 _hoverScale;

    private void Awake()
    {
        _outline = this.GetComponent<Outline>();
        _originalScale = Vector3.one;
        _hoverScale = new Vector3(1.1f, 1.1f, 1f);
        _card = new Card(13, Card.Stack.NONE);
    }

    private void OnEnable()
    {
        GameManager.SetStartSettingsEvent += SetSelectableStateOfGraveyardCard;
        GameManager.FlipAllCardsAtGameEndEvent += FlipCardIfNotFlippedAtGameEnd;
        NetworkCardManager.UpdateInteractionStateEvent += SetInteractivity;
    }

    private void OnDisable()
    {
        GameManager.SetStartSettingsEvent -= SetSelectableStateOfGraveyardCard;
        GameManager.FlipAllCardsAtGameEndEvent -= FlipCardIfNotFlippedAtGameEnd;
        NetworkCardManager.UpdateInteractionStateEvent -= SetInteractivity;
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

    public void SetCorrespondingDeck(Card.Stack decktype)
    {
        _card.correspondingDeck = decktype;
    }

    public void SetCardBackImageVisibility(bool visible)
    {
        cardBackImage.SetActive(visible);
    }

    // Nur für die Graveyard Karte
    // Entscheidet, ob von dem lokalen Spieler die Graveyard Karte angeklickt werden kann
    private void SetSelectableStateOfGraveyardCard(ulong currentPlayerId)
    {
        if (_card.correspondingDeck != Card.Stack.GRAVEYARD) return;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        isSelectable = currentPlayerId == localClientId;

        SetInteractivity(isSelectable);
    }

    public void SetInteractivity(bool isActive)
    {
        if (_card.correspondingDeck == Card.Stack.ENEMYCARD) return;

        canHover = isActive;
        isSelectable = isActive;

        // Optionale visuelle Indikatoren
        Debug.Log($"Interaktivität der Karte '{name}' auf {isActive} gesetzt.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Wenn nicht gehovert werden darf, return
        if (!canHover) return;

        this.transform.localScale = _hoverScale;

        int index = this.transform.GetSiblingIndex();
        // Event für Hover starten und SiblingIndex als Identifikator weitergeben
        OnCardHovered?.Invoke(_hoverScale, index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Wenn nicht gehovert werden darf, return
        if (!canHover) return;

        this.transform.localScale = _originalScale;

        int index = this.transform.GetSiblingIndex();
        // Event für Hover starten und SiblingIndex als Identifikator weitergeben
        OnCardHovered?.Invoke(_originalScale, index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Wenn nicht gehovert werden darf, return
        if (!isSelectable) return;

        //SelectionAnimation();

        if (_card.correspondingDeck == Card.Stack.GRAVEYARD)
        {
            OnGraveyardCardClicked?.Invoke();
        }
        else
        {
            FlipCardAnimation(!cardBackImage.activeSelf);
        }
    }

    private void SelectionAnimation()
    {
        if (_outline == null)
        {
            Debug.Log("Das Object " + name + " hat keine Komponente Outline");
            return;
        }

        _outline.enabled = !_outline.enabled;

        int index = this.transform.GetSiblingIndex();
        // Event für Selektieren starten und SiblingIndex als Identifikator weitergeben
        OnCardClicked?.Invoke(_outline.enabled, index);
    }

    public void FlipCardAnimation(bool showCardBack)
    {
        LeanTween.rotateY(this.gameObject, 90.0f, 0.25f).setOnComplete(() =>
        {
            cardBackImage.SetActive(showCardBack);
            LeanTween.rotateY(this.gameObject, 0.0f, 0.25f);
        });
    }

    // Methode, die das Event am Spielende verarbeitet
    public void FlipCardIfNotFlippedAtGameEnd()
    {
        if (cardBackImage.activeSelf) // Nur umdrehen, wenn Rückseite sichtbar
        {
            FlipCardAnimation(false); // Hier den Parameter für "showCardBack" auf "false" setzen
        }
    }
}

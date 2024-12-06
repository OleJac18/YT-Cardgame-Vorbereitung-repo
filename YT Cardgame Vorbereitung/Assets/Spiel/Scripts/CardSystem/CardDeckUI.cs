using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDeckUI : MonoBehaviour, IPointerClickHandler
{
    public static event Action OnCardDeckClicked;
    public bool isSelectable = false;

    private void Start()
    {
        GameManager.SetStartSettingsEvent += SetSelectableState;
        NetworkCardManager.UpdateInteractionStateEvent += SetSelectableState;
    }

    private void OnDestroy()
    {
        GameManager.SetStartSettingsEvent -= SetSelectableState;
        NetworkCardManager.UpdateInteractionStateEvent -= SetSelectableState;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable) return;

        OnCardDeckClicked?.Invoke();
        Debug.Log("CardDeck geklickt.");
    }

    private void SetSelectableState(ulong currentPlayerId)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        isSelectable = currentPlayerId == localClientId;

        Debug.Log("Meine localClientId ist: " + localClientId + " und der Status von isSelectable im CardDeckUI ist: " + isSelectable);
    }

    private void SetSelectableState(bool isSelectable)
    {
        this.isSelectable = isSelectable;
    }
}

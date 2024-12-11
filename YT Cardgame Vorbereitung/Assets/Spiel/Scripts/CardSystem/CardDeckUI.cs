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
        //GameManager.SetStartSettingsEvent += SetStartSelectableState;
        GameManager.Instance.currentPlayerId.OnValueChanged += SetSelectableState;
    }

    public void OnNetworkSpawn()
    {
        GameManager.Instance.currentPlayerId.OnValueChanged += SetSelectableState;
    }

    private void OnDestroy()
    {
        //GameManager.SetStartSettingsEvent -= SetStartSelectableState;
        GameManager.Instance.currentPlayerId.OnValueChanged -= SetSelectableState;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable) return;

        OnCardDeckClicked?.Invoke();
        Debug.Log("CardDeck geklickt.");
    }

    private void SetStartSelectableState(ulong currentPlayerId)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        isSelectable = currentPlayerId == localClientId;

        Debug.Log("Meine localClientId ist: " + localClientId + " und der Status von isSelectable im CardDeckUI ist: " + isSelectable);
    }

    private void SetSelectableState(ulong previousPlayerId, ulong currentPlayerId)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        isSelectable = currentPlayerId == localClientId;

        Debug.Log("Meine localClientId ist: " + localClientId + " und der Status von isSelectable im CardDeckUI ist: " + isSelectable);
    }
}

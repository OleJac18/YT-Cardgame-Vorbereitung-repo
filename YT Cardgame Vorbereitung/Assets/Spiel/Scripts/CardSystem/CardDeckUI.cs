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
        GameManager.Instance.currentPlayerId.OnValueChanged += SetSelectableState;
    }

    private void OnDestroy()
    {
        GameManager.Instance.currentPlayerId.OnValueChanged -= SetSelectableState;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable) return;

        isSelectable = false;

        OnCardDeckClicked?.Invoke();
    }

    private void SetSelectableState(ulong previousPlayerId, ulong currentPlayerId)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        isSelectable = currentPlayerId == localClientId;
    }
}

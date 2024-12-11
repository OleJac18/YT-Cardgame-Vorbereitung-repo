using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public static event Action DiscardCardEvent;

    public Button discardButton;
    public Button exchangeButton;

    // Start is called before the first frame update
    void Start()
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);

        CardManager.ShowButtonsEvent += ShowPlayerButton;
        GameManager.Instance.currentPlayerId.OnValueChanged += DisablePlayerButton;
    }

    public void OnDestroy()
    {
        CardManager.ShowButtonsEvent -= ShowPlayerButton;
        GameManager.Instance.currentPlayerId.OnValueChanged -= DisablePlayerButton;
    }

    private void DisablePlayerButton(ulong previousPlayerId, ulong currentPlayerId)
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);
    }

    private void ShowPlayerButton()
    {
        if (GameManager.Instance.currentPlayerId.Value == NetworkManager.Singleton.LocalClientId)
        {
            discardButton.gameObject.SetActive(true);
            exchangeButton.gameObject.SetActive(true);
        }
        else
        {
            discardButton.gameObject.SetActive(false);
            exchangeButton.gameObject.SetActive(false);
        }
    }

    public void DiscardButtonClicked()
    {
        Debug.Log("Ich möchte die Karte wieder abgeben.");
        DiscardCardEvent?.Invoke();
    }

    public void ExchangeButtonClicked()
    {
        Debug.Log("Ich möchte die Karte mit einer anderen Karte tauschen.");
    }
}

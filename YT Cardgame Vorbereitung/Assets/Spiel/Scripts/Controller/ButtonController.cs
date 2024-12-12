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
    }

    public void OnDestroy()
    {
        CardManager.ShowButtonsEvent -= ShowPlayerButton;
    }

    private void ShowPlayerButton()
    {
        if (NetworkManager.Singleton.LocalClientId != GameManager.Instance.currentPlayerId.Value) return;

        discardButton.gameObject.SetActive(true);
        exchangeButton.gameObject.SetActive(true);
    }

    private void HidePlayerButton()
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);
    }

    public void DiscardButtonClicked()
    {
        Debug.Log("Ich möchte die Karte wieder abgeben.");
        HidePlayerButton();
        DiscardCardEvent?.Invoke();
    }

    public void ExchangeButtonClicked()
    {
        Debug.Log("Ich möchte die Karte mit einer anderen Karte tauschen.");
        HidePlayerButton();
    }
}

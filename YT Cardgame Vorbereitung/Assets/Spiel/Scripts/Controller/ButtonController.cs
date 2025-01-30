using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public static event Action DiscardCardEvent;
    public static event Action ExchangeCardEvent;
    public static event Action<ulong> EndGameStartedEvent;

    public Button discardButton;
    public Button exchangeButton;
    public Button endGameButton;

    // Start is called before the first frame update
    void Start()
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);
        endGameButton.gameObject.SetActive(false);

        CardManager.ShowButtonsEvent += ShowPlayerButton;
        NetworkCardManager.HidePlayerButtonEvent += HidePlayerButton;
        CardDeckUI.OnCardDeckClicked += HideEndGameButton;
        CardController.OnGraveyardCardClickedEvent += HideEndGameButton;
        GameManager.Instance.currentPlayerId.OnValueChanged += ShowEndGameButton;
    }


    public void OnDestroy()
    {
        CardManager.ShowButtonsEvent -= ShowPlayerButton;
        NetworkCardManager.HidePlayerButtonEvent -= HidePlayerButton;
        CardDeckUI.OnCardDeckClicked -= HideEndGameButton;
        CardController.OnGraveyardCardClickedEvent -= HideEndGameButton;
        GameManager.Instance.currentPlayerId.OnValueChanged -= ShowEndGameButton;
    }

    private void ShowEndGameButton(ulong previousValue, ulong newValue)
    {
        if (NetworkManager.Singleton.LocalClientId != newValue) return;
        endGameButton.gameObject.SetActive(true);
    }

    private void HideEndGameButton()
    {
        endGameButton.gameObject.SetActive(false);
    }

    private void ShowPlayerButton()
    {
        if (NetworkManager.Singleton.LocalClientId != GameManager.Instance.currentPlayerId.Value) return;

        discardButton.gameObject.SetActive(true);
        exchangeButton.gameObject.SetActive(true);
    }

    public void HidePlayerButton()
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);
        endGameButton.gameObject.SetActive(false);
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
        ExchangeCardEvent?.Invoke();
    }

    public void EndGameButtonClicked()
    {
        Debug.Log("Ich möchte das Spiel beenden.");
        EndGameStartedEvent?.Invoke(NetworkManager.Singleton.LocalClientId);
    }
}

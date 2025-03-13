using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public static event Action<ulong> EndGameStartedEvent;
    public static event Action DiscardButtonClickedEvent;


    public Button discardButton;
    public Button exchangeButton;
    public Button actionsButton;
    public Button endGameButton;

    public TextMeshProUGUI actionsButtonText;

    // Start is called before the first frame update
    void Start()
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);
        actionsButton.gameObject.SetActive(false);
        endGameButton.gameObject.SetActive(false);

        CardManager.ShowDiscardAndExchangeButtonEvent += ShowDiscardAndExchangeButton;
        NetworkCardManager.HidePlayerButtonEvent += HidePlayerButton;
        CardDeckUI.OnCardDeckClicked += HideEndGameButton;
        CardController.OnGraveyardCardClickedEvent += HideEndGameButton;
        GameManager.Instance.currentPlayerId.OnValueChanged += ShowEndGameButton;
        CardManager.ShowActionsButtonEvent += ShowActionsButton;
        CardManager.HidePlayerButtonEvent += HidePlayerButton;
    }


    public void OnDestroy()
    {
        CardManager.ShowDiscardAndExchangeButtonEvent -= ShowDiscardAndExchangeButton;
        NetworkCardManager.HidePlayerButtonEvent -= HidePlayerButton;
        CardDeckUI.OnCardDeckClicked -= HideEndGameButton;
        CardController.OnGraveyardCardClickedEvent -= HideEndGameButton;
        GameManager.Instance.currentPlayerId.OnValueChanged -= ShowEndGameButton;
        CardManager.ShowActionsButtonEvent -= ShowActionsButton;
        CardManager.HidePlayerButtonEvent -= HidePlayerButton;
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

    private void ShowDiscardAndExchangeButton()
    {
        if (NetworkManager.Singleton.LocalClientId != GameManager.Instance.currentPlayerId.Value) return;

        discardButton.gameObject.SetActive(true);
        exchangeButton.gameObject.SetActive(true);
    }

    public void HideDiscardAndExchangeButton()
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);
    }

    private void ShowActionsButton(string buttonText)
    {
        actionsButton.gameObject.SetActive(true);
        actionsButtonText.text = buttonText;
    }

    public void HidePlayerButton()
    {
        discardButton.gameObject.SetActive(false);
        exchangeButton.gameObject.SetActive(false);
        actionsButton.gameObject.SetActive(false);
        endGameButton.gameObject.SetActive(false);
    }

    public void DiscardButtonClicked()
    {
        HidePlayerButton();
        DiscardButtonClickedEvent?.Invoke();
    }

    public void ActionsButtonClicked()
    {
        HidePlayerButton();
    }

    public void EndGameButtonClicked()
    {
        HideEndGameButton();
        EndGameStartedEvent?.Invoke(NetworkManager.Singleton.LocalClientId);
    }
}
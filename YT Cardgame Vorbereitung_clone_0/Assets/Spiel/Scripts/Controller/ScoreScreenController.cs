using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreScreenController : MonoBehaviour
{

    public GameObject scoreScreenUI;
    public TextMeshProUGUI winnerText;

    public Button restartButton;
    public TextMeshProUGUI statusText;

    [Header("Player Panel")]
    public TextMeshProUGUI[] playerName;
    public TextMeshProUGUI[] playerScore;

    public static event Action OnReadyButtonClickedEvent;


    public void Start()
    {
        GameManager.UpdateScoreScreenEvent += UpdateScoreScreen;
    }

    public void OnDestroy()
    {
        GameManager.UpdateScoreScreenEvent -= UpdateScoreScreen;
    }

    private void UpdateScoreScreen(Player[] players, Player winningPlayer)
    {
        ShowScoreScreen();
        UpdatePlayerPanels(players);
        UpdateWinnerText(winningPlayer);
    }


    private void ShowScoreScreen()
    {
        LeanTween.alphaCanvas(scoreScreenUI.GetComponent<CanvasGroup>(), 1.0f, 1.0f);
        scoreScreenUI.GetComponent<CanvasGroup>().interactable = true;
        scoreScreenUI.GetComponent<CanvasGroup>().blocksRaycasts = true;
    }

    private void UpdatePlayerPanels(Player[] players)
    {
        for (int i = 0; i < players.Length; i++)
        {
            Debug.Log("i: " + i + ", PlayerName: " + players[i].name + ", PlayerScore: " + players[i].score);
            playerName[i].text = players[i].name;
            playerScore[i].text = players[i].score.ToString();
        }
    }

    private void UpdateWinnerText(Player winningPlayer)
    {
        winnerText.text = $"{winningPlayer.name} Gewinnt";
    }

    public void OnReadyButtonClicked()
    {
        restartButton.interactable = false;
        statusText.gameObject.SetActive(true);

        // Sende Ready-Status an den Server
        OnReadyButtonClickedEvent?.Invoke();
    }


}

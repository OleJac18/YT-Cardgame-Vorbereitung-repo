using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ScoreScreenController : MonoBehaviour
{

    public GameObject scoreScreenUI;
    public TextMeshProUGUI winnerText;

    [Header("Buttons")]
    public Button restartButton;
    public TextMeshProUGUI restartButtonText;

    public Button exitButton;

    public TextMeshProUGUI statusText;

    [Header("Player Panel")]
    public TextMeshProUGUI[] playerName;
    public TextMeshProUGUI[] playerScore;

    public static event Action OnReadyButtonClickedEvent;
    public bool isCompleteEndOfGame;


    public void Start()
    {
        GameManager.UpdateScoreScreenEvent += UpdateScoreScreen;
    }

    public void OnDestroy()
    {
        GameManager.UpdateScoreScreenEvent -= UpdateScoreScreen;
    }

    private void UpdateScoreScreen(Player[] players, Player winningPlayer, bool isCompleteEndOfGame)
    {
        ShowScoreScreen();
        UpdatePlayerPanels(players);
        UpdateWinnerText(winningPlayer);

        this.isCompleteEndOfGame = isCompleteEndOfGame;

        if (isCompleteEndOfGame)
        {
            UpdateButton("Restart Game");
            exitButton.gameObject.SetActive(true);
        }
        else
        {
            UpdateButton("Next Round");
            exitButton.gameObject.SetActive(false);
        }
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
            Debug.Log("i: " + i + ", PlayerName: " + players[i].name + ", PlayerScore: " + players[i].totalScore);
            playerName[i].text = players[i].name;
            if(GameManager.Instance.roundCounter.Value == 1)
            {
                playerScore[i].text = players[i].totalScore.ToString();
            }
            else
            {
                playerScore[i].text = players[i].totalScore.ToString() + " (+" + players[i].roundScore.ToString() + ")";
            }    
        }
    }

    private void UpdateWinnerText(Player winningPlayer)
    {
        winnerText.text = $"{winningPlayer.name} Gewinnt";
    }

    public void OnReadyButtonClicked()
    {
        restartButton.interactable = false;
        exitButton.interactable = false;

        statusText.gameObject.SetActive(true);

        // Sende Ready-Status an den Server
        OnReadyButtonClickedEvent?.Invoke();
    }

    private void UpdateButton(string buttonText)
    {
        restartButtonText.text = buttonText;
    }

}

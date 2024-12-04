using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerNr
{
    Player1 = 0,
    Player2 = 1,
    Player3 = 2,
    Player4 = 3
}

public class PlayerUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private Image activePlayerImage;
    [SerializeField] private PlayerNr _playerNr; // PlayerNr wird im Editor gesetzt

    private ulong _localPlayerId;

    private void Start()
    {
        GameManager.InitializePlayerUIEvent += Initialize;
        GameManager.UpdatePlayerUIEvent += SetActivePlayer;
    }

    private void OnDestroy()
    {
        GameManager.InitializePlayerUIEvent -= Initialize;
        GameManager.UpdatePlayerUIEvent -= SetActivePlayer;
    }

    public void Initialize(PlayerNr playerNr, Player player, bool isCurrentPlayer)
    {
        if (_playerNr == playerNr)
        {
            _localPlayerId = player.id;
            playerNameText.text = player.name;
            UpdateScore(player.score);
            SetActivePlayer(isCurrentPlayer);
        }
    }

    public void UpdateScore(int score)
    {
        playerScoreText.text = $"Score: {score}";
    }

    public void SetActivePlayer(ulong currentPlayerId)
    {
        bool isActive = currentPlayerId == _localPlayerId;

        activePlayerImage.color = isActive ? Color.green : Color.grey; // Gr�n f�r aktiven Spieler
    }

    public void SetActivePlayer(bool isActive)
    {
        activePlayerImage.color = isActive ? Color.green : Color.grey; // Gr�n f�r aktiven Spieler
    }
}
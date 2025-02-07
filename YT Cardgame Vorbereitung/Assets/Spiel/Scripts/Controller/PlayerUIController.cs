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
    [SerializeField] private TextMeshProUGUI CaboText;
    [SerializeField] private Image activePlayerImage;
    [SerializeField] private PlayerNr _playerNr; // PlayerNr wird im Editor gesetzt

    private ulong _localPlayerId;
    public static bool _wasGameClosedBefore = false;

    private void Start()
    {
        PlayerUIManager.InitializePlayerUIEvent += Initialize;
        PlayerUIManager.UpdatePlayerUIEvent += SetActivePlayer;
        GameManager.OnUpdateScoreUIEvent += UpdateScore;
        GameManager.ShowCaboTextEvent += ShowCaboText;
    }

    private void OnDestroy()
    {
        PlayerUIManager.InitializePlayerUIEvent -= Initialize;
        PlayerUIManager.UpdatePlayerUIEvent -= SetActivePlayer;
        GameManager.OnUpdateScoreUIEvent -= UpdateScore;
        GameManager.ShowCaboTextEvent -= ShowCaboText;
    }

    private void Initialize(PlayerNr playerNr, Player player, bool isCurrentPlayer)
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

    public void UpdateScore(ulong clientId, int score)
    {
        if (clientId != _localPlayerId) return;

        playerScoreText.text = $"Score: {score}";
    }

    public void SetActivePlayer(ulong currentPlayerId)
    {
        bool isActive = currentPlayerId == _localPlayerId;

        activePlayerImage.color = isActive ? Color.green : Color.grey; // Grün für aktiven Spieler
    }

    public void SetActivePlayer(bool isActive)
    {
        activePlayerImage.color = isActive ? Color.green : Color.grey; // Grün für aktiven Spieler
    }

    public ulong GetLocalPlayerId()
    {
        return _localPlayerId;
    }

    public void ShowCaboText(ulong clientId)
    {
        if (clientId != _localPlayerId || _wasGameClosedBefore) return;

        _wasGameClosedBefore = true;
        CaboText.enabled = true;
    }
}

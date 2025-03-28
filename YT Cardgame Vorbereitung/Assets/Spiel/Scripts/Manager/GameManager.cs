using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum PlayerAction
{
    Initialize,
    ChangeCurrentPlayer
}


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>();
    public NetworkVariable<int> roundCounter = new NetworkVariable<int>();

    public static event Action<PlayerManager, ulong> ServFirstCardEvent;
    public static event Action<int[]> ProcessSelectedCardsEvent;
    public static event Action<ulong, int> OnUpdateScoreUIEvent;
    public static event Action RestartGameEvent;
    public static event Action StartTransitionEvent;
    public static event Action FlipAllCardsEvent;
    public static event Action<Player[], Player, bool> UpdateScoreScreenEvent;
    public static event Action<Player> UpdateEnemyCardsEvent;
    public static event Action<ulong> ShowCaboTextEvent;
    public static event Action<int> SendSpiedCardNumberEvent;
    public static event Action<int, bool> SendSwappedCardNumberEvent;

    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private NetworkPlayerUIManager _networkPlayerUIManager;
    [SerializeField] private AudioManager _audioManager;

    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private int readyPlayers;
    [SerializeField] private bool isEndOfCompleteGame;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;

        _playerManager = new PlayerManager();
        _turnManager = new TurnManager();

        readyPlayers = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        CardManager.EndTurnEvent += EndTurn;
        ButtonController.EndGameStartedEvent += OnGameEndButtonPressed;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        ConnectionManager.ServerDisconnectedEvent += DeletePlayerData;
        ScoreScreenController.OnReadyButtonClickedEvent += OnReadyButtonClickedServerRpc;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        base.OnNetworkSpawn();

        // Grundwert für den roundCounter festlegen
        roundCounter.Value = 1;

        // Muss in der OnNetworkSpawn Function sein, weil es sonst zu Problemen mit der NetworkVariablen currentPlayerId
        // in der Initialize Function kommt, da diese Variable noch nicht im Netzwerk verfügbar ist, aber schon benutzt wird
        ConnectionManager.AllClientsConnectedAndSceneLoadedEvent += InitializeGame;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        CardManager.EndTurnEvent -= EndTurn;
        ButtonController.EndGameStartedEvent -= OnGameEndButtonPressed;
        ConnectionManager.ServerDisconnectedEvent -= DeletePlayerData;
        ScoreScreenController.OnReadyButtonClickedEvent -= OnReadyButtonClickedServerRpc;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        ConnectionManager.AllClientsConnectedAndSceneLoadedEvent -= InitializeGame;
    }

    private void EndTurn()
    {
        _audioManager.PlayNextTurnSound();

        if (IsServer && _turnManager != null)
        {
            _turnManager.NextTurn();
            currentPlayerId.Value = _turnManager.GetCurrentPlayer();
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintPlayerDictionary();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        _playerManager.AddNewPlayer(clientId);
    }


    private void DeletePlayerData()
    {
        _playerManager.DeletePlayerData();
    }

    private void InitializeGame()
    {
        if (!IsServer) return;

        currentPlayerId.Value = 50;             // Der Variablen wird zu Beginn ein Wert zugewiesen, der nie erreicht werden kann, damit man mitbekommt,
                                                // wenn man das erste Mal den currentPlayer aus TurnManager holt um einen Change in der Variablen mitzubekommen

        GetAudioManagerClientsAndHostRpc();
        _networkPlayerUIManager = FindObjectOfType<NetworkPlayerUIManager>();
        _networkPlayerUIManager.SetPlayerManager(_playerManager);

        _turnManager.SetStartPlayer(_playerManager);
        currentPlayerId.Value = _turnManager.GetCurrentPlayer();

        // Wirft ein Event, damit die ersten Karten ausgegeben werden
        ServFirstCardEvent?.Invoke(_playerManager, currentPlayerId.Value);

        // Wirft ein Event, in dem die PlayerUI auf einen Grundzustand gesetzt wird
        _networkPlayerUIManager.HandlePlayerAction(PlayerAction.Initialize, currentPlayerId.Value);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void GetAudioManagerClientsAndHostRpc()
    {
        _audioManager = FindObjectOfType<AudioManager>();
    }

    private void PrintPlayerDictionary()
    {
        _playerManager.PrintPlayerDictionary();
    }

    [Rpc(SendTo.Server)]
    private void OnReadyButtonClickedServerRpc()
    {
        readyPlayers++;

        int totalPlayers = _playerManager.GetPlayerCount();

        if (readyPlayers >= totalPlayers)
        {
            readyPlayers = 0;
            roundCounter.Value++;
            StartTransitionClientsAndHostRpc();
            StartCoroutine(Restart());
        }
    }

    private void OnGameEndButtonPressed(ulong clientId)
    {
        ShowCaboTextAndPlayCaboSoundClientAndHostRpc(clientId);
        EndCurrentTurnAndSavePlayerWhoPressedServerRpc(clientId);
    }

    ////////////////////////////////////////////////////////////////////////

    public void ProcessOnSpyButtonClicked(ulong clientId, ulong enemyClientId, int cardNumberIndex)
    {
        List<int> cards = _playerManager.GetPlayerCards(enemyClientId);
        SendSpiedCardNumberClientRpc(cards[cardNumberIndex], RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    ////////////////////////////////////////////////////////////////////////

    public void ProcessOnSwapButtonClicked(ulong playerClientId, ulong enemyClientId, int playerClickedCardIndex, int enemyClickedCardIndex)
    {
        List<int> playerCards = _playerManager.GetPlayerCards(playerClientId);
        List<int> enemyCards = _playerManager.GetPlayerCards(enemyClientId);

        int playerCard = playerCards[playerClickedCardIndex];
        int enemyCard = enemyCards[enemyClickedCardIndex];

        playerCards[playerClickedCardIndex] = enemyCard;
        enemyCards[enemyClickedCardIndex] = playerCard;

        _playerManager.SetPlayerCards(playerClientId, playerCards);
        _playerManager.SetPlayerCards(enemyClientId, enemyCards);


        SendSwappedCardNumberClientRpc(enemyCard, true, RpcTarget.Single(playerClientId, RpcTargetUse.Temp));
        SendSwappedCardNumberClientRpc(playerCard, false, RpcTarget.Single(enemyClientId, RpcTargetUse.Temp));
    }


    ////////////////////////////////////////////////////////////////////////

    public void SetPlayerCards(ulong clientId, List<int> cards)
    {
        _playerManager.SetPlayerCards(clientId, cards);
    }

    public void GetPlayerCardsAndProcessSelectedCards(ulong clientId)
    {
        List<int> cards = _playerManager.GetPlayerCards(clientId);
        ProcessSelectedCardsClientRpc(cards.ToArray(), RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void ProcessSelectedCardsClientRpc(int[] cards, RpcParams rpcParams = default)
    {
        ProcessSelectedCardsEvent?.Invoke(cards);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowCaboTextAndPlayCaboSoundClientAndHostRpc(ulong clientId)
    {
        ShowCaboTextEvent?.Invoke(clientId);
        _audioManager.PlayCaboSound();
    }

    [Rpc(SendTo.Server)]
    private void EndCurrentTurnAndSavePlayerWhoPressedServerRpc(ulong clientId)
    {
        _turnManager.OnGameEndButtonPressed(clientId);
        EndTurn();
    }

    /// <summary>
    /// Beendet das Spiel
    /// Es wird ermittelt, welcher Spieler die geringsten Punkte hat und somit dann auch der Gewinner
    /// Der Score aller Spieler wird auf der UI geupdated
    /// Die Karten des Enemys werden mit den tatsächlichen Zahlen gefüllt
    /// Die Karten aller Spieler werden umgedreht 
    /// Der ScoreScreen wird eingeblendet 
    /// </summary>
    public void EndGame()
    {
        Player[] players = _playerManager.GetAllPlayers();

        Player winningPlayer = _playerManager.CalculatePlayerScores((ulong)_turnManager.gameEndingPlayerId);

        isEndOfCompleteGame = _playerManager.CheckScoreOfPlayersForEndOfCompleteGame();

        UpdateScoreAndEnemyCardsForAllPlayer();
        FlipCardsAndDisplayScoreScreenClientsAndHostRpc(players, winningPlayer, isEndOfCompleteGame);
    }

    /// <summary>
    /// Der Score aller Spieler wird auf der UI geupdated
    /// Die Karten des Enemys werden mit den tatsächlichen Zahlen gefüllt
    /// </summary>
    private void UpdateScoreAndEnemyCardsForAllPlayer()
    {
        Dictionary<ulong, Player> _playerDataDict = _playerManager.GetPlayerDataDict();
        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            Player player = playerData.Value;
            UpdateScoreAndEnemyCardsClientsAndHostRpc(player);
        }
    }

    /// <summary>
    /// Der Score aller Spieler wird auf der UI geupdated
    /// Die Karten des Enemys werden mit den tatsächlichen Zahlen gefüllt
    /// </summary>
    /// <param name="player"></param>
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateScoreAndEnemyCardsClientsAndHostRpc(Player player)
    {
        OnUpdateScoreUIEvent?.Invoke(player.id, player.totalScore);
        UpdateEnemyCardsEvent?.Invoke(player);
    }

    /// <summary>
    /// Das Spiel wird neugestartet
    /// </summary>
    /// <returns></returns>
    private IEnumerator Restart()
    {
        yield return new WaitForSeconds(1f);

        if (isEndOfCompleteGame)
        {
            roundCounter.Value = 1;
            _playerManager.ResetPlayerTotalCount();
        }

        yield return new WaitForSeconds(1f);

        // Wechseln Sie die Szene auf dem Server und allen Clients
        RestartGameEvent?.Invoke();
    }


    /// <summary>
    /// Die Karten aller Spieler werden umgedreht 
    /// Der ScoreScreen wird eingeblendet 
    /// </summary>
    /// <param name="players"></param>
    /// <param name="winningPlayer"></param>
    [Rpc(SendTo.ClientsAndHost)]
    private void FlipCardsAndDisplayScoreScreenClientsAndHostRpc(Player[] players, Player winningPlayer, bool isEndOfCompleteGame)
    {
        FlipAllCardsEvent?.Invoke();
        UpdateScoreScreenEvent?.Invoke(players, winningPlayer, isEndOfCompleteGame);
    }

    /// <summary>
    /// Startet die Transition. Heißt der Bildschirm wird dunkel s
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void StartTransitionClientsAndHostRpc()
    {
        StartTransitionEvent?.Invoke();
    }

    ////////////////////////////////////////////////

    [Rpc(SendTo.SpecifiedInParams)]
    public void SendSpiedCardNumberClientRpc(int cardNumber, RpcParams rpcParams = default)
    {
        SendSpiedCardNumberEvent?.Invoke(cardNumber);
    }

    ////////////////////////////////////////////////

    [Rpc(SendTo.SpecifiedInParams)]
    public void SendSwappedCardNumberClientRpc(int cardNumber, bool enableReturnToGraveyardEvent, RpcParams rpcParams = default)
    {
        SendSwappedCardNumberEvent?.Invoke(cardNumber, enableReturnToGraveyardEvent);
    }
}

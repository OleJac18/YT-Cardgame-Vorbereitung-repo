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

    public static event Action<PlayerManager, ulong> ServFirstCardEvent;
    public static event Action<int[]> ProcessSelectedCardsEvent;
    public static event Action<ulong, int> OnUpdateScoreUIEvent;
    public static event Action RestartGameEvent;
    public static event Action StartTransitionEvent;
    public static event Action FlipAllCardsEvent;
    public static event Action<Player[], Player> UpdateScoreScreenEvent;
    public static event Action<Player> UpdateEnemyCardsEvent;
    public static event Action<ulong> ShowCaboTextEvent;

    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private NetworkPlayerUIManager _networkPlayerUIManager;

    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private int readyPlayers;

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
        if (!IsServer) return;

        base.OnNetworkDespawn();

        ConnectionManager.AllClientsConnectedAndSceneLoadedEvent -= InitializeGame;
    }

    private void EndTurn()
    {
        if (IsServer && _turnManager != null)
        {
            _turnManager.NextTurn();
            currentPlayerId.Value = _turnManager.GetCurrentPlayer();
            Debug.Log("currentPlayerId: " + currentPlayerId.Value);
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Ich will etwas ausgeben.");
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
        currentPlayerId.Value = 50;             // Der Variablen wird zu Beginn ein Wert zugewiesen, der nie erreicht werden kann, damit man mitbekommt,
                                                // wenn man das erste Mal den currentPlayer aus TurnManager holt um einen Change in der Variablen mitzubekommen

        _networkPlayerUIManager = FindObjectOfType<NetworkPlayerUIManager>();
        _networkPlayerUIManager.SetPlayerManager(_playerManager);

        _turnManager.SetStartPlayer(_playerManager);
        Debug.Log("currentPlayerId before: " + currentPlayerId.Value);
        currentPlayerId.Value = _turnManager.GetCurrentPlayer();
        Debug.Log("currentPlayerId after: " + currentPlayerId.Value);

        // Wirft ein Event, damit die ersten Karten ausgegeben werden
        //List<ulong> clientIds = _playerManager.GetConnectedClientIds();
        ServFirstCardEvent?.Invoke(_playerManager, currentPlayerId.Value);

        // Wirft ein Event, in dem die PlayerUI auf einen Grundzustand gesetzt wird
        _networkPlayerUIManager.HandlePlayerAction(PlayerAction.Initialize, currentPlayerId.Value);
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
            StartTransitionClientsAndHostRpc();
            StartCoroutine(Restart());
        }
    }

    private void OnGameEndButtonPressed(ulong clientId)
    {
        ShowCaboTextClientAndHostRpc(clientId);
        EndCurrentTurnAndSavePlayerWhoPressedServerRpc(clientId);
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
    private void ShowCaboTextClientAndHostRpc(ulong clientId)
    {
        ShowCaboTextEvent?.Invoke(clientId);
    }

    [Rpc(SendTo.Server)]
    private void EndCurrentTurnAndSavePlayerWhoPressedServerRpc(ulong clientId)
    {
        _turnManager.OnGameEndButtonPressed(clientId);
        EndTurn();
    }

    public void EndGame()
    {
        Player[] players = _playerManager.GetAllPlayers();

        Player winningPlayer = _playerManager.CalculatePlayerScores((ulong)_turnManager.gameEndingPlayerId);
        UpdateScoreAndEnemyCardsForAllPlayer();
        FlipCardsAndDisplayScoreScreenClientsAndHostRpc(players, winningPlayer);
    }

    private void UpdateScoreAndEnemyCardsForAllPlayer()
    {
        Dictionary<ulong, Player> _playerDataDict = _playerManager.GetPlayerDataDict();
        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            Player player = playerData.Value;
            UpdateScoreAndEnemyCardsClientsAndHostRpc(player);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateScoreAndEnemyCardsClientsAndHostRpc(Player player)
    {
        OnUpdateScoreUIEvent?.Invoke(player.id, player.score);
        UpdateEnemyCardsEvent?.Invoke(player);
    }

    private IEnumerator Restart()
    {
        yield return new WaitForSeconds(2f);

        // Wechseln Sie die Szene auf dem Server und allen Clients
        RestartGameEvent?.Invoke();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FlipCardsAndDisplayScoreScreenClientsAndHostRpc(Player[] players, Player winningPlayer)
    {
        FlipAllCardsEvent?.Invoke();
        UpdateScoreScreenEvent?.Invoke(players, winningPlayer);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StartTransitionClientsAndHostRpc()
    {
        StartTransitionEvent?.Invoke();
    }
}

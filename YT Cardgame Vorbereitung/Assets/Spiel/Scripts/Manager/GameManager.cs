using System;
using Unity.Netcode;
using UnityEngine;

public enum PlayerAction
{
    Initialize,
    ChangeCurrentPlayer
}

public enum GameState
{
    WaitingForPlayers,
    TurnStart,
    DrawingCard,
    ChoosingAction,
    SwappingCards,
    EndOfTurn
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>();

    public static event Action<PlayerManager, ulong> ServFirstCardEvent;
    public static event Action<ulong> SetStartSettingsEvent;

    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private NetworkPlayerUIManager _networkPlayerUIManager;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;

        _playerManager = new PlayerManager();
        _turnManager = new TurnManager();
   }

    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        ConnectionManager.AllClientsConnectedAndSceneLoaded += InitializeGame;

    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        base.OnNetworkSpawn();

        currentPlayerId.Value = 50;             // Der Variablen wird zu Beginn ein Wert zugewiesen, der nie erreicht werden kann, damit man mitbekommt, wenn man das erste Mal den currentPlayer aus TurnManager holt um einen Change in der Variablen mitzubekommen
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        ConnectionManager.AllClientsConnectedAndSceneLoaded -= InitializeGame;
    }


    private void Update()
    {
        // Beispiel: Nächster Spieler bei Tastendruck
        if (IsServer && Input.GetKeyDown(KeyCode.N) && _turnManager != null)
        {
            _turnManager.NextTurn();
            currentPlayerId.Value = _turnManager.GetCurrentPlayer();
            Debug.Log("currentPlayerId: " + currentPlayerId.Value);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Ich will etwas ausgeben.");
            PrintPlayerDictionary();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        _playerManager.AddNewPlayer(clientId);

        //CheckAllClientsConnected();
    }

    private void CheckAllClientsConnected()
    {
        if (AllClientsConnected())
        {
            //_networkPlayerUIManager = FindObjectOfType<NetworkPlayerUIManager>();
            //_networkPlayerUIManager.SetPlayerManager(_playerManager);
            InitializeGame();
        }
    }

    private bool AllClientsConnected()
    {
        return _playerManager.GetConnectedClientIds().Count == 2;
    }

    private void InitializeGame()
    {
        _networkPlayerUIManager = FindObjectOfType<NetworkPlayerUIManager>();
        _networkPlayerUIManager.SetPlayerManager(_playerManager);

        _turnManager.SetStartPlayer(_playerManager);
        Debug.Log("currentPlayerId before: " + currentPlayerId.Value);
        currentPlayerId.Value = _turnManager.GetCurrentPlayer();
        Debug.Log("currentPlayerId after: " + currentPlayerId.Value);

        // Übergibt die Id vom Spieler, der am Zug ist, damit entschieden werden kann,
        // ob der Client das Kartendeck und die Graveyard Karte anklicken können soll
        SetStartSettingsEvent?.Invoke(currentPlayerId.Value);

        // Wirft ein Event, damit die ersten Karten ausgegeben werden
        //List<ulong> clientIds = _playerManager.GetConnectedClientIds();
        ServFirstCardEvent?.Invoke(_playerManager, currentPlayerId.Value);

        // Wirft ein Event, in dem die PlayerUI auf einen Grundzustand gesetzt wird
        _networkPlayerUIManager.HandlePlayerAction(PlayerAction.Initialize, currentPlayerId.Value);
    }

    public void PrintPlayerDictionary()
    {
        _playerManager.PrintPlayerDictionary();
    }
}

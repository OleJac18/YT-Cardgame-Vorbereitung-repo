using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum PlayerAction
{
    Initialize,
    ChangeCurrentPlayer,
    PrintPlayerDict
}

public class GameManager : MonoBehaviour
{
    public static event Action FlipAllCardsAtGameEndEvent;


    //public static event Action<List<ulong>, ulong> ServFirstCardEvent;
    public static event Action<PlayerManager, ulong> ServFirstCardEvent;


    public static event Action<ulong> SetStartSettingsEvent;
    public static event Action<ulong> ChangeCurrentPlayerEvent;

    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private NetworkPlayerUIManager _networkPlayerUIManager;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _playerManager = new PlayerManager();
        _turnManager = new TurnManager();
        Debug.Log($"TurnManager initialisiert. Startspieler: {_turnManager.GetCurrentPlayer()}");
    }

    private void Start()
    {
        ConnectionManager.ClientConnectedEvent += OnClientConnected;
    }

    private void OnDestroy()
    {
        ConnectionManager.ClientConnectedEvent -= OnClientConnected;
    }

    private void Update()
    {
        // Beispiel: Nächster Spieler bei Tastendruck
        if (Input.GetKeyDown(KeyCode.N) && _turnManager != null)
        {
            _turnManager.NextTurn();
            ulong currentPlayerId = _turnManager.GetCurrentPlayer();
            Debug.Log("currentPlayerId: " + currentPlayerId);
            //HandlePlayerAction(PlayerAction.ChangeCurrentPlayer, currentPlayerId);

            // Updated die Interaktionsmöglichkeit mit den Spielerkarten, Kartenstapel und Graveyard
            ChangeCurrentPlayerEvent?.Invoke(currentPlayerId);

            //Updated die PlayerUI beim Spieler
            _networkPlayerUIManager.HandlePlayerAction(PlayerAction.ChangeCurrentPlayer, currentPlayerId, _playerManager);
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

        Debug.Log($"Client {clientId} connected.");
        _playerManager.AddNewPlayer(clientId);

        CheckAllClientsConnected();
    }

    private void CheckAllClientsConnected()
    {
        if (AllClientsConnected())
        {
            _networkPlayerUIManager = FindObjectOfType<NetworkPlayerUIManager>();
            InitializeGame();
        }
    }

    private bool AllClientsConnected()
    {
        return _playerManager.GetConnectedClientIds().Count == 2;
    }

    private void InitializeGame()
    {
        _turnManager.SetStartPlayer(_playerManager);
        ulong currentPlayerId = _turnManager.GetCurrentPlayer();

        // Übergibt die Id vom Spieler, der am Zug ist, damit entschieden werden kann,
        // ob der Client das Kartendeck und die Graveyard Karte anklicken können soll
        SetStartSettingsEvent?.Invoke(currentPlayerId);

        // Wirft ein Event, damit die ersten Karten ausgegeben werden
        //List<ulong> clientIds = _playerManager.GetConnectedClientIds();
        ServFirstCardEvent?.Invoke(_playerManager, currentPlayerId);

        // Wirft ein Event, in dem die PlayerUI auf einen Grundzustand gesetzt wird
        _networkPlayerUIManager.HandlePlayerAction(PlayerAction.Initialize, currentPlayerId, _playerManager);
    }

    public void PrintPlayerDictionary()
    {
        Dictionary<ulong, Player> _playerDataDict = _playerManager.GetPlayerDataDict();

        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            ulong id = playerData.Key;
            Player player = playerData.Value;

            Debug.Log("ID: " + id + ", Name: " + player.name + ", Score: " + player.score);
            for (int i = 0; i < player.cards.Count; i++)
            {
                Debug.Log("Karte " + i + ": " + player.cards[i]);
            }
        }
    }

    public void TriggerFlipAllCardsAtGameEnd()
    {
        FlipAllCardsAtGameEndEvent?.Invoke();
    }
}

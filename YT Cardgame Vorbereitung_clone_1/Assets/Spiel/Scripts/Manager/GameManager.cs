using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerAction
{
    Initialize,
    ChangeCurrentPlayer,
    PrintPlayerDict
}

public class GameManager : NetworkBehaviour
{
    public static event Action FlipAllCardsAtGameEndEvent;
    public static event Action<List<ulong>, ulong> ServFirstCardEvent;
    public static event Action<PlayerNr, Player, bool> InitializePlayerUIEvent;
    public static event Action<bool> InitializeInteractionStateEvent;
    public static event Action<ulong> SetStartSettingsEvent;
    public static event Action<ulong> UpdatePlayerUIEvent;
    public static event Action<bool> ChangeCurrentPlayerEvent;

    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private TurnManager _turnManager;

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

    public override void OnDestroy()
    {
        base.OnDestroy();

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
            HandlePlayerAction(PlayerAction.ChangeCurrentPlayer, currentPlayerId);
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
        List<ulong> clientIds = _playerManager.GetConnectedClientIds();
        ServFirstCardEvent?.Invoke(clientIds, currentPlayerId);

        // Wirft ein Event, in dem die PlayerUI auf einen Grundzustand gesetzt wird
        HandlePlayerAction(PlayerAction.Initialize, currentPlayerId);
    }


    private void HandlePlayerAction(PlayerAction action, ulong currentPlayerId)
    {
        Dictionary<ulong, Player> playerDataDict = _playerManager.GetPlayerDataDict();

        foreach (var playerData in playerDataDict)
        {
            Player player = playerData.Value;

            switch (action)
            {
                case PlayerAction.Initialize:
                    Player[] players = _playerManager.GetAllPlayers();
                    InitializePlayerUIClientRpc(players, currentPlayerId, RpcTarget.Single(player.id, RpcTargetUse.Temp));

                    break;

                case PlayerAction.ChangeCurrentPlayer:
                    ChangeCurrentPlayerClientRpc(currentPlayerId, RpcTarget.Single(player.id, RpcTargetUse.Temp));
                    break;

                case PlayerAction.PrintPlayerDict:
                    Debug.Log($"ID: {player.id}, Name: {player.name}, Score: {player.score}");
                    break;

                default:
                    Debug.LogWarning("Unhandled PlayerAction: " + action);
                    break;
            }
        }
    }

    public void PrintPlayerDictionary()
    {
        Dictionary<ulong, Player> _playerDataDict = _playerManager.GetPlayerDataDict();

        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            ulong id = playerData.Key;
            Player player = playerData.Value;

            Debug.Log("ID: " + id + ", Name: " + player.name + ", Score: " + player.score);
        }
    }

    public void TriggerFlipAllCardsAtGameEnd()
    {
        FlipAllCardsAtGameEndEvent?.Invoke();
    }

    // Initialisiert die PlayerUI beim Spieler
    [Rpc(SendTo.SpecifiedInParams)]
    private void InitializePlayerUIClientRpc(Player[] players, ulong currentPlayerId, RpcParams rpcParams = default)
    {
        int startIndex = 0;

        // Findet heraus an welchem Index der lokale Spieler in dem Array ist
        // Damit bei allen die gleiche Reihenfolge von Spielern visuell dargestellt wird. 
        // Also beispielsweise: Links von Thomas sitzt Ulla, Rechts von Thomas sitzt Theo
        //      Dann muss bei Ulla es folgend sein: Links Theo, Rechts Thomas
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].id == NetworkManager.Singleton.LocalClientId)
            {
                startIndex = i;
                break;
            }
        }

        // Initialisiert jeden PlayerUIController im Spiel
        for (int i = 0; i < players.Length; i++)
        {
            int playerIndex = (i + startIndex) % players.Length;
            bool isCurrentPlayer = currentPlayerId == players[playerIndex].id;

            if (Enum.IsDefined(typeof(PlayerNr), i))
            {
                PlayerNr currentPlayerNr = (PlayerNr)i;
                InitializePlayerUIEvent?.Invoke(currentPlayerNr, players[playerIndex], isCurrentPlayer);
            }
            else
            {
                Debug.LogWarning($"Ungültiger PlayerNr-Wert: {i}");
            }
        }
    }


    // Updated die PlayerUI beim Spieler
    // Updated die Interaktionsmöglichkeit mit den Spielerkarten, Kartenstapel und Graveyard
    [Rpc(SendTo.SpecifiedInParams)]
    private void ChangeCurrentPlayerClientRpc(ulong currentPlayerId, RpcParams rpcParams = default)
    {
        bool isCurrentPlayer = currentPlayerId == NetworkManager.Singleton.LocalClientId;

        // Updated die Interaktionsmöglichkeit mit den Spielerkarten, Kartenstapel und Graveyard
        ChangeCurrentPlayerEvent?.Invoke(isCurrentPlayer);

        //Updated die PlayerUI beim Spieler
        UpdatePlayerUIEvent?.Invoke(currentPlayerId);
    }
}

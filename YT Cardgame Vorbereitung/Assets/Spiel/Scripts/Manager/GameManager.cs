using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static event Action FlipAllCardsAtGameEndEvent;
    public static event Action<List<ulong>, ulong> StartGameEvent;
    public static event Action<ulong> SetStartSettingsEvent;
    public static event Action<ulong> ChangeCurrentPlayerEvent;

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
            ChangeCurrentPlayerEvent?.Invoke(currentPlayerId);
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
        List<ulong> clientIds = _playerManager.GetConnectedClientIds();
        if (clientIds.Count < 2) return;

        _turnManager.SetStartPlayer(_playerManager);
        ulong currentPlayerId = _turnManager.GetCurrentPlayer();

        // Übergibt die Id vom Spieler, der am Zug ist, damit entschieden werden kann,
        // ob der Client das Kartendeck und die Graveyard Karte anklicken können soll
        SetStartSettingsEvent?.Invoke(currentPlayerId);

        // Wirft ein Event, damit die ersten Karten ausgegeben werden
        StartGameEvent?.Invoke(clientIds, currentPlayerId);
    }

    public void PrintPlayerDictionary()
    {
        Dictionary<ulong, Player> _playerDataDict = _playerManager.GetPlayerDataDict();

        foreach (KeyValuePair<ulong, Player> playerData in _playerDataDict)
        {
            ulong id = playerData.Key;
            Player player = playerData.Value;

            Debug.Log("ID: " + id + ", Name: " + player.name + ", Level: " + player.score);
        }
    }

    public void TriggerFlipAllCardsAtGameEnd()
    {
        FlipAllCardsAtGameEndEvent?.Invoke();
    }
}

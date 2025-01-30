using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public static event Action AllClientsConnectedAndSceneLoadedEvent;
    public static event Action ServerDisconnectedEvent;

    private string gameplaySceneName = "Gameplay";
    private int connectedClients = 0;
    private int requiredClients = 2; // Mindestanzahl an Spielern

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        MainMenu.HostSuccessfullyStartedEvent += SubscribeToSceneEvent;
        GameManager.RestartGameEvent += RestartGame;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
            MainMenu.HostSuccessfullyStartedEvent -= SubscribeToSceneEvent;
            GameManager.RestartGameEvent -= RestartGame;
        }
    }

    private void SubscribeToSceneEvent()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected.");
        if (NetworkManager.Singleton.IsServer)
        {
            //connectedClients++;
            connectedClients = NetworkManager.Singleton.ConnectedClients.Count;
            Debug.Log("Connected Clients: " + connectedClients);

            if (connectedClients >= requiredClients && NetworkManager.Singleton.IsServer)
            {
                Debug.Log("Genügend Clients verbunden. Starte Szenenwechsel...");
                LoadGameplayScene();
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("Client" + clientId + "disconnected");

        if (!NetworkManager.Singleton.IsServer)
        {
            //connectedClients--;
            SceneManager.LoadScene("MainMenu");
        }

    }

    private void OnServerStopped(bool wasClient)
    {
        Debug.Log("Server stopped");
        SceneManager.LoadScene("MainMenu");
        ServerDisconnectedEvent?.Invoke();
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneName == gameplaySceneName && sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            Debug.Log("Gameplay-Szene erfolgreich geladen.");
            // Wenn wir das Event hier nicht deabonnieren, wird die Initalisierung im GameManager zweimal durchgeführt
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            StartCoroutine(StartInitializationDelayed());
        }
    }

    private void LoadGameplayScene()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private IEnumerator StartInitializationDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        AllClientsConnectedAndSceneLoadedEvent?.Invoke();
    }

    private void RestartGame()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        LoadGameplayScene();
    }
}

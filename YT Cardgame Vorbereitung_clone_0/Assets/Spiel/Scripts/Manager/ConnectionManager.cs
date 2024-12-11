using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public static event Action AllClientsConnectedAndSceneLoaded;

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
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
            MainMenu.HostSuccessfullyStartedEvent += SubscribeToSceneEvent;
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
            connectedClients++;
            Debug.Log("Connected Clients: " +  connectedClients);

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
            connectedClients--;
            SceneManager.LoadScene("MainMenu");
        }

    }

    private void OnServerStopped(bool wasClient)
    {
        Debug.Log("Server stopped");
        SceneManager.LoadScene("MainMenu");
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneName == gameplaySceneName && sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            Debug.Log("Gameplay-Szene erfolgreich geladen.");
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
        AllClientsConnectedAndSceneLoaded?.Invoke();
    }
}



/*using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public static event Action<ulong> ClientConnectedEvent;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log("Client" + clientId + "connected");

        if (!NetworkManager.Singleton.IsServer) return;
        ClientConnectedEvent?.Invoke(clientId);
    }

    private void OnClientDisconnectedCallback(ulong clientId)
    {
        Debug.Log("Client" + clientId + "disconnected");

        if (!NetworkManager.Singleton.IsServer)
        {
            SceneManager.LoadScene("MainMenu");
        }

    }

    private void OnServerStopped(bool wasClient)
    {
        Debug.Log("Server stopped");
        SceneManager.LoadScene("MainMenu");
    }

}*/

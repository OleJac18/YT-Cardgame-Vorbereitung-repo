using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Only attach this example component to the NetworkManager GameObject.
/// This will provide you with a single location to register for client
/// connect and disconnect events.  
/// </summary>
public class ConnectionNotificationManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            throw new Exception($"There is no {nameof(NetworkManager)} for the {nameof(ConnectionNotificationManager)} to do stuff with! " +
                $"Please add a {nameof(NetworkManager)} to the scene.");
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;  // Wird auf einem Client ausgelöst, sobald er die Verbindung zum Server verloren hat. Entweder weil der Host das Spiel verlassen hat oder weil der Client selber das Spiel verlassen hat
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;                        // Wird nur auf dem Server/Host ausgeführt, da dieses für die Clients irrelevant ist. Die Clients reagieren auf das Event OnClientDisconnectCallback
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;  
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;                        
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log("Client " + clientId + " connected");
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log("Client " + clientId + " disconnected");
        if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Client verlässt das Spiel (wenn dieser Client nicht der Server ist)
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnServerStopped(bool wasClient)
    {
        Debug.Log("Server stopped.");

        // Wenn der Server stoppt, alle Clients ins Hauptmenü zurückbringen.
        SceneManager.LoadScene("MainMenu");
    }
}

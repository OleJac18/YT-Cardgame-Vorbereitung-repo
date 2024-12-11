using System;
using Unity.Netcode;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static event Action HostSuccessfullyStartedEvent;
    public void StartHost()
    {
        bool success = false;
        success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }

    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server gestartet");
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client gestartet");
    }
}

using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static event Action HostSuccessfullyStartedEvent;

    public void StartHost()
    {
        bool success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }

    }

    public void StartServer()
    {
        bool success = NetworkManager.Singleton.StartServer();
        Debug.Log("Server gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    public void StartClient()
    {
        bool success = NetworkManager.Singleton.StartClient();
        Debug.Log("Client gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }
}

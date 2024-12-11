using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static event Action HostSuccessfullyStartedEvent;
    public static event Action<ulong, string> NewPlayerConnectedEvent;

    [SerializeField] private TMP_InputField _nameInputField;

    private void Start()
    {
        SendInputToGameManager();
    }

    public void StartHost()
    {
        bool success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
            NewPlayerConnectedEvent?.Invoke(NetworkManager.Singleton.LocalClientId, _nameInputField.text);
        }

    }

    public void StartServer()
    {
        bool success = NetworkManager.Singleton.StartServer();
        Debug.Log("Server gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
            NewPlayerConnectedEvent?.Invoke(NetworkManager.Singleton.LocalClientId, _nameInputField.text);
        }
    }

    public void StartClient()
    {
        bool success = NetworkManager.Singleton.StartClient();
        Debug.Log("Client gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
            NewPlayerConnectedEvent?.Invoke(NetworkManager.Singleton.LocalClientId, _nameInputField.text);
        }
    }

    public void SendInputToGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInputField(_nameInputField);
        }
        else
        {
            Debug.LogWarning("GameManager Instance not found!");
        }
    }
}

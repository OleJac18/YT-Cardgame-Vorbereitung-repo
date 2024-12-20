using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static event Action HostSuccessfullyStartedEvent;
    [SerializeField] private TMP_InputField ipInputField; // Eingabefeld für die IP-Adresse des Servers
    [SerializeField] private ushort serverPort = 7777; // Der Standard-Port für den Server
    public string ipAdress;

    public void StartHost()
    {
        // Lokale IP-Adresse vom Host
        ConfigureTransport("10.10.21.43", serverPort);

        bool success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
           HostSuccessfullyStartedEvent?.Invoke();
        }

    }

    public void StartServer()
    {
        // Lokale IP-Adresse vom Host
        ConfigureTransport("10.10.21.43", serverPort);

        bool success = NetworkManager.Singleton.StartServer();
        Debug.Log("Server gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    public void StartClient()
    {
        string serverIp = ipInputField.text; // Die IP-Adresse des Servers aus dem Eingabefeld lesen
        ipAdress = serverIp;

        // IP-Adresse des Servers konfigurieren
        ConfigureTransport(serverIp, serverPort);

        bool success = NetworkManager.Singleton.StartClient();
        Debug.Log("Client gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    private void ConfigureTransport(string ip, ushort port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ip;
            transport.ConnectionData.Port = port;
            transport.ConnectionData.ServerListenAddress = "0.0.0.0";
        }
    }
}

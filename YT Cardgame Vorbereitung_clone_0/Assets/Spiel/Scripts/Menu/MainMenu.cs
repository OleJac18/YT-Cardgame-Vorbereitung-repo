using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput; // Eingabefeld für den Code des Servers
    [SerializeField] private TextMeshProUGUI joinCodeText;

    public static event Action HostSuccessfullyStartedEvent;

    [SerializeField] private RelayManager _relayManager;
    [SerializeField] private bool _useRelay;


    //[SerializeField] private ushort serverPort = 7777; // Der Standard-Port für den Server
    //public string ipAdress;

    private void Start()
    {
        _useRelay = false;
    }

    public void SetUseRelay(bool toggleState)
    {
        _useRelay = toggleState;
    }

    public void OnDisconnectButtonPressed()
    {
        if (!_useRelay)
            DisconnectLocal();
        else
            DisconnectFromRelay();

    }

    public void StartHost()
    {
        if (!_useRelay)
            StartLocalHost();
        else
            StartOnlineHost();
    }

    private void StartLocalHost()
    {
        // Lokale IP-Adresse vom Host
        //ConfigureTransport("10.10.21.43", serverPort);

        _relayManager.SignOut(); // Spieler abmelden

        // Hol den UnityTransport und konfiguriere ihn ohne Relay
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7777);

        bool success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    public async void StartOnlineHost()
    {
        bool initialized = await _relayManager.Initialize();

        if (!initialized)
        {
            Debug.LogError("StartOnlineHost abgebrochen: Relay konnte nicht initialisiert werden.");
            return;
        }

        string joinCode = await _relayManager.CreateRelay();
        joinCodeText.text = "Join Code: " + joinCode;
    }

    public void StartServer()
    {
        // Lokale IP-Adresse vom Host
        //ConfigureTransport("10.10.21.43", serverPort);

        bool success = NetworkManager.Singleton.StartServer();
        Debug.Log("Server gestartet");

        if (success)
        {
            //HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    public void StartClient()
    {
        

        if (!_useRelay)
        {
            //string serverIp = ipInputField.text; // Die IP-Adresse des Servers aus dem Eingabefeld lesen
            //ipAdress = serverIp;

            // IP-Adresse des Servers konfigurieren
            //ConfigureTransport(serverIp, serverPort);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", 7777);

            bool success = NetworkManager.Singleton.StartClient();
            Debug.Log("Client gestartet");

            if (success)
            {
                HostSuccessfullyStartedEvent?.Invoke();
            }
        }
        else
        {
            string code = joinCodeInput.text;
            _relayManager.JoinRelay(code);
        }
    }

    public void DisconnectLocal()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Host/Server wird heruntergefahren...");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Client trennt Verbindung...");
        }

        NetworkManager.Singleton.Shutdown(); // Beende das Netzwerk
    }

    public void DisconnectFromRelay()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Host/Server wird heruntergefahren...");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Client trennt Verbindung...");
        }

        NetworkManager.Singleton.Shutdown(); // Beende das Netzwerk

        _relayManager.SignOut(); // Spieler abmelden

        Debug.Log("Netzwerk wurde beendet.");
    }



    /*private void ConfigureTransport(string ip, ushort port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ip;
            transport.ConnectionData.Port = port;
            transport.ConnectionData.ServerListenAddress = "0.0.0.0";
        }
    }*/

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}

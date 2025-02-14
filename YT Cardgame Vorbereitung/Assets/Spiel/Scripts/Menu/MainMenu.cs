using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput; // Eingabefeld für den Code des Servers
    [SerializeField] private TextMeshProUGUI joinCodeText;

    [Header("Offline Input")]
    [SerializeField] private TMP_InputField OfflineHostGameIPInput; // Eingabefeld für die IP Adresse des Host
    [SerializeField] private TMP_InputField OfflineHostGamePortInput; // Eingabefeld für die Port Adresse des Host

    [SerializeField] private TMP_InputField OfflineJoinGameIPInput; // Eingabefeld für die IP Adresse zum Joinen
    [SerializeField] private TMP_InputField OfflineJoinGamePortInput; // Eingabefeld für die Port Adresse zum Joinen

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

    public void StartLocalHost()
    {
        // Lokale IP-Adresse vom Host
        //ConfigureTransport("10.10.21.43", serverPort);

        _relayManager.SignOut(); // Spieler abmelden


        if (ConvertInputToInt(OfflineHostGamePortInput.text) == null) return;
        
        ushort port = (ushort)ConvertInputToInt(OfflineHostGamePortInput.text);

        // Hol den UnityTransport und konfiguriere ihn ohne Relay
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        //transport.SetConnectionData("127.0.0.1", 7777);
        transport.SetConnectionData(OfflineHostGameIPInput.text, port);

        bool success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    public ushort? ConvertInputToInt(string input)
    {
        ushort convertedValue;

        // Den Text aus dem InputField holen
        string inputText = input;

        // Versuchen, den Text in einen Integer zu konvertieren
        if (ushort.TryParse(inputText, out convertedValue))
        {
            // Wenn die Umwandlung erfolgreich war, gebe den Wert aus
            Debug.Log("Der konvertierte Wert ist: " + convertedValue);
            return convertedValue;
        }
        else
        {
            // Wenn die Umwandlung nicht erfolgreich war, gib eine Fehlermeldung aus
            Debug.Log("Ungültiger Wert! Bitte gib eine gültige Zahl ein.");
            return null;
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

    public void StartLocalClient()
    {
        //string serverIp = ipInputField.text; // Die IP-Adresse des Servers aus dem Eingabefeld lesen
        //ipAdress = serverIp;

        // IP-Adresse des Servers konfigurieren
        //ConfigureTransport(serverIp, serverPort);

        if (ConvertInputToInt(OfflineJoinGamePortInput.text) == null) return;

        ushort port = (ushort)ConvertInputToInt(OfflineJoinGamePortInput.text);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(OfflineJoinGameIPInput.text, port);

        bool success = NetworkManager.Singleton.StartClient();
        Debug.Log("Client gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
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

            if (ConvertInputToInt(OfflineJoinGamePortInput.text) == null) return;

            ushort port = (ushort)ConvertInputToInt(OfflineJoinGamePortInput.text);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(OfflineJoinGameIPInput.text, port);

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

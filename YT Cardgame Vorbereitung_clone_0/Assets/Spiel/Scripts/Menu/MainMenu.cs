using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput; // Eingabefeld für den Code des Servers
    [SerializeField] private TextMeshProUGUI joinCodeText;

    [Header("Offline Input")]
    [SerializeField] private TMP_InputField OfflineHostGameIPInput; // Eingabefeld für die IP Adresse des Host
    [SerializeField] private TMP_InputField OfflineHostGamePortInput; // Eingabefeld für die Port Adresse des Host

    [SerializeField] private TMP_InputField OfflineJoinGameIPInput; // Eingabefeld für die IP Adresse zum Joinen
    [SerializeField] private TMP_InputField OfflineJoinGamePortInput; // Eingabefeld für die Port Adresse zum Joinen

    [Header("Wlan Input")]
    [SerializeField] private TMP_InputField WlanHostGameIPInput; // Eingabefeld für die IP Adresse des Host
    [SerializeField] private TMP_InputField WlanHostGamePortInput; // Eingabefeld für die Port Adresse des Host

    [SerializeField] private TMP_InputField WlanJoinGameIPInput; // Eingabefeld für die IP Adresse zum Joinen
    [SerializeField] private TMP_InputField WlanJoinGamePortInput; // Eingabefeld für die Port Adresse zum Joinen

    [Header("Online Input")]
    [SerializeField] private TMP_InputField OnlineJoinCodeOutput; // Ausgabe des erstellten Joincodes
    [SerializeField] private TMP_InputField OnlineJoinCodeInput; // Eingabefeld für die Joincode zum Joinen

    public static event Action HostSuccessfullyStartedEvent;

    [SerializeField] private RelayManager _relayManager;
    [SerializeField] private bool _useRelay;

    private bool initialized;

    private void Start()
    {
        initialized = false;
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

    /////////////////////////////////////////////////////////////
    // Local Connection Management

    public void StartLocalHost()
    {
        SetUpLocalTransport(OfflineHostGameIPInput.text, OfflineHostGamePortInput.text);

        StartLocalOrWlanHost();
    }

    public void StartLocalClient()
    {
        SetUpLocalTransport(OfflineJoinGameIPInput.text, OfflineJoinGamePortInput.text);

        StartLocalOrWlanClient();
    }

    /////////////////////////////////////////////////////////////
    // Wlan Connection Management

    public void StartWlanHost()
    {
        SetUpLocalTransport(WlanJoinGameIPInput.text, WlanHostGamePortInput.text);

        StartLocalOrWlanHost();
    }

    public void StartWlanClient()
    {
        SetUpLocalTransport(WlanHostGameIPInput.text, WlanJoinGamePortInput.text);

        StartLocalOrWlanClient();
    }

    /////////////////////////////////////////////////////////////
    // Connection Preperations

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

    private void SetUpLocalTransport(string ipAdressString, string portString)
    {
        _relayManager.SignOut(); // Spieler abmelden

        if (ConvertInputToInt(portString) == null) return;

        ushort port = (ushort)ConvertInputToInt(portString);

        // Hol den UnityTransport und konfiguriere ihn ohne Relay
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        //transport.SetConnectionData(OfflineHostGameIPInput.text, port);
        transport.SetConnectionData(ipAdressString, port);
    }

    public void StartLocalOrWlanHost()
    {
        bool success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    public void StartLocalOrWlanClient()
    {
        bool success = NetworkManager.Singleton.StartClient();
        Debug.Log("Client gestartet");

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

    //////////////////////////////////////////////////////////////////////////
    // Online Connection Management

    public async void StartOnlineHost()
    {
        if (!initialized)
        {
            initialized = await _relayManager.Initialize();

            if (!initialized)
            {
                Debug.LogError("StartOnlineHost abgebrochen: Relay konnte nicht initialisiert werden.");
                return;
            }
        }

        string joinCode = await _relayManager.CreateRelay();
        OnlineJoinCodeOutput.text = joinCode;
    }

    public async void StartOnlineClient()
    {
        if (!initialized)
        {
            initialized = await _relayManager.Initialize();

            if (!initialized)
            {
                Debug.LogError("StartOnlineClient abgebrochen: Relay konnte nicht initialisiert werden.");
                return;
            }
        }

        string code = OnlineJoinCodeInput.text;
        _relayManager.JoinRelay(code);
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

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}

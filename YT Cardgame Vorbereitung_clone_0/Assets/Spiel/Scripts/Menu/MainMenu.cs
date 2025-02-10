using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static event Action HostSuccessfullyStartedEvent;
    [SerializeField] private TMP_InputField joinCodeInput; // Eingabefeld für den Code des Servers
    [SerializeField] private TextMeshProUGUI joinCodeText;


    //[SerializeField] private ushort serverPort = 7777; // Der Standard-Port für den Server
    //public string ipAdress;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void StartHost()
    {
        // Lokale IP-Adresse vom Host
        //ConfigureTransport("10.10.21.43", serverPort);

        /*bool success = NetworkManager.Singleton.StartHost();
        Debug.Log("Host gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }*/

        CreateRelay();

    }

    public void StartServer()
    {
        // Lokale IP-Adresse vom Host
        //ConfigureTransport("10.10.21.43", serverPort);

        bool success = NetworkManager.Singleton.StartServer();
        Debug.Log("Server gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }
    }

    public void StartClient()
    {
        //string serverIp = ipInputField.text; // Die IP-Adresse des Servers aus dem Eingabefeld lesen
        //ipAdress = serverIp;

        // IP-Adresse des Servers konfigurieren
        //ConfigureTransport(serverIp, serverPort);

        /*bool success = NetworkManager.Singleton.StartClient();
        Debug.Log("Client gestartet");

        if (success)
        {
            HostSuccessfullyStartedEvent?.Invoke();
        }*/


        string code = joinCodeInput.text;
        JoinRelay(code);
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

    ////////////////////////////////////////////////////////////////////////
    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            // der Joincode ist der Code, den man den Freunden schicken kann, damit sie sich mit dem selben Relay verbinden können
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Relay Join Code: " + joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            bool success = NetworkManager.Singleton.StartHost();
            Debug.Log("Host gestartet");

            if (success)
            {
                HostSuccessfullyStartedEvent?.Invoke();
            }

            joinCodeText.text = "Join Code: " + joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay error: " + e.Message);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay join error: " + e.Message);
        }
    }
}

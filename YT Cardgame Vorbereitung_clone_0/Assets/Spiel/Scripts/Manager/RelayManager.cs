using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Unity.Networking.Transport.Relay;
using System;

public class RelayManager : MonoBehaviour
{
    private bool isInitialized = false;
    public static event Action HostSuccessfullyStartedEvent;

    public async Task<bool> Initialize()  // Rückgabe: true = Erfolg, false = Fehler
    {
        try
        {
            await UnityServices.InitializeAsync();
            isInitialized = true;

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log("Unity Relay erfolgreich initialisiert!");
            return true;  // Erfolgreiche Initialisierung
        }
        catch (Exception e)
        {
            Debug.LogError("Fehler bei der Unity Relay-Initialisierung: " + e.Message);
            return false; // Initialisierung fehlgeschlagen
        }
    }

    public void SignOut()
    {
        if (isInitialized && AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("User wurde erfolgreich abgemeldet.");
        }
    }


    public async Task<string> CreateRelay()
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

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay error: " + e.Message);
            return null;
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

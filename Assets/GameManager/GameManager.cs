using System;
using System.Collections;
using System.Threading.Tasks;
using Debugger;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private const int MaxConnections = 4;
    [SerializeField]
    private string relayJoinCode = "";
    private const string ClassName = "[GameManager]";
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        DebugConsole.AddButton("login using unity", OnLoginUsingUnity);
        DebugConsole.AddButton("start as host relay", OnStartAsHostUsingRelay);
        DebugConsole.AddButton("start as client relay", OnStartAsClientRelay);
    }

    private void OnClientDisconnected(ulong clientNetworkId)
    {
        Debug.Log($"{ClassName} OnClientDisconnected clientNetworkId:{clientNetworkId} " +
                  $"IsServer:{NetworkManager.Singleton.IsServer} IsHost:{NetworkManager.Singleton.IsHost}" +
                  $" IsClient:{NetworkManager.Singleton.IsClient}");
    }

    private void OnStartAsClientRelay()
    {
        StartCoroutine(ConfigureTransportAndStartNgoAsConnectingPlayer(relayJoinCode));
    }

    private IEnumerator ConfigureTransportAndStartNgoAsConnectingPlayer(string relayJoinCode)
    {
        // Populate RelayJoinCode beforehand through the UI
        var clientRelayUtilityTask = JoinRelayServerFromJoinCode(relayJoinCode);

        while (!clientRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }

        if (clientRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception: " + clientRelayUtilityTask.Exception.Message);
            yield break;
        }

        var relayServerData = clientRelayUtilityTask.Result;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();
        yield return null;
    }
    
    public static async Task<RelayServerData> JoinRelayServerFromJoinCode(string joinCode)
    {
        JoinAllocation allocation;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        return new RelayServerData(allocation, "dtls");
    }

    private void OnStartAsHostUsingRelay()
    {
        StartCoroutine(ConfigureTransportAndStartNgoAsHost());
    }
    private async void OnLoginUsingUnity()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            var playerID = AuthenticationService.Instance.PlayerId;
            Debug.Log($"{ClassName} logged in using unity, player id: {playerID}");
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    private void OnClientConnected(ulong clientNetworkId)
    {
        Debug.Log($"{ClassName} OnClientConnected clientNetworkId:{clientNetworkId} " +
                  $"IsServer:{NetworkManager.Singleton.IsServer} IsHost:{NetworkManager.Singleton.IsHost}" +
                  $" IsClient:{NetworkManager.Singleton.IsClient}");
    }
    IEnumerator ConfigureTransportAndStartNgoAsHost()
    {
        var serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(MaxConnections);
        while (!serverRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }
        if (serverRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
            yield break;
        }

        var relayServerData = serverRelayUtilityTask.Result;

        // Display the joinCode to the user.

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();
        yield return null;
    }
    private static async Task<RelayServerData> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
    {
        Allocation allocation;
        string createJoinCode;
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed {e.Message}");
            throw;
        }

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            createJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"{ClassName} createJoinCode: {createJoinCode}");
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        return new RelayServerData(allocation, "dtls");
    }
}

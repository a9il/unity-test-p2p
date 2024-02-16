using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Debugger;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.PlayerAccounts;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class P2PRelayMenuCanvas : MonoBehaviour
{
    [SerializeField] private Button loginAnonymousBtn;
    [SerializeField] private Button loginWithUnityBtn;
    [SerializeField] private Button startAsHostBtn;
    [SerializeField] private Button startAsClientBtn;
    [SerializeField] private Button sendDataBtn;
    [SerializeField] private TMPro.TMP_InputField relayCodeInput;
    private static NetworkManager _networkManager;
    private const int MaxConnections = 4;
    private const string ClassName = "[P2PRelayMenuCanvas]";
    [SerializeField]
    private UnityTransport _unityTransport;
    async void Start()
    {
        _networkManager = NetworkManager.Singleton;
        _networkManager.OnClientConnectedCallback += OnClientConnected;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        loginAnonymousBtn.onClick.AddListener(OnLoginAnonymousBtnClicked);
        loginWithUnityBtn.onClick.AddListener(OnLoginWithUnityBtnClicked);
        startAsHostBtn.onClick.AddListener(OnStartHostBtnClicked);
        startAsClientBtn.onClick.AddListener(OnStartAsClientBtnClicked);
        sendDataBtn.onClick.AddListener(OnSendDataBtnClicked);
        SetP2pBtnInteractable(false);
        sendDataBtn.interactable = false;
        await UnityServices.InitializeAsync();
        PlayerAccountService.Instance.SignedIn += SignInWithUnity;
    }

    private async void SignInWithUnity()
    {
        DebugConsole.Log($"access token: {PlayerAccountService.Instance.AccessToken}");
        try
        {
           
            await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
            var playerId = AuthenticationService.Instance.PlayerId;
            DebugConsole.Log($"{ClassName} SignIn with unity is successful playerId:{playerId}.");
            SetP2pBtnInteractable(true);
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
            SetLoginBtnInteractable(true);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
            SetLoginBtnInteractable(true);
        }
    }

    private void OnSendDataBtnClicked()
    {
        SendGeneratedData();
    }

    private void OnStartAsClientBtnClicked()
    {
        SetP2pBtnInteractable(false);
        StartCoroutine(ConfigureTransportAndStartNgoAsConnectingPlayer(relayCodeInput.text));
    }

    private void OnStartHostBtnClicked()
    {
        SetP2pBtnInteractable(false);
        StartCoroutine(ConfigureTransportAndStartNgoAsHost());
    }

    private void OnLoginWithUnityBtnClicked()
    {
        SetLoginBtnInteractable(false);
        PlayerAccountService.Instance.StartSignInAsync();
    }

    private async void OnLoginAnonymousBtnClicked()
    {
        SetLoginBtnInteractable(false);
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            var playerID = AuthenticationService.Instance.PlayerId;
            DebugConsole.Log($"{ClassName} logged in anonymously using unity, player id: {playerID}");
            SetP2pBtnInteractable(true);
        }
        catch (Exception e)
        {
            SetLoginBtnInteractable(true);
            Debug.Log(e);
        }
    }

    private void SetLoginBtnInteractable(bool isActive)
    {
        loginAnonymousBtn.interactable = isActive;
        loginWithUnityBtn.interactable = isActive;
    }

    private void OnClientDisconnected(ulong clientNetworkId)
    {
        DebugConsole.Log($"{ClassName} OnClientDisconnected ClientID:{clientNetworkId}");
    }

    private void OnClientConnected(ulong clientNetworkId)
    {
        DebugConsole.Log($"{ClassName} OnClientConnected ClientID:{clientNetworkId}");
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

        _unityTransport.SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();
        sendDataBtn.interactable = true;
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
            DebugConsole.Log($"{ClassName} createJoinCode: {createJoinCode}");
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        return new RelayServerData(allocation, "dtls");
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
    private void SendGeneratedData()
    {
        var levelObjects = DummyDataGenerator.GenerateRandomLevelObject();
        int byteSize = LevelObject.GetByteSize() * levelObjects.Length;
        var sequentialIds = DummyDataGenerator.GenerateSequentialId();
        byteSize += sizeof(ulong) * DummyDataGenerator.RandomObjectLength;
        var v3r = DummyDataGenerator.GenerateVector3();
        byteSize += Marshal.SizeOf(typeof(Vector3)) * DummyDataGenerator.RandomObjectLength;
        var teamStates = DummyDataGenerator.GenerateTeamState();
        byteSize += TeamState.GetByteSize() * DummyDataGenerator.RandomObjectLength;
        var playerStates = DummyDataGenerator.GeneratePlayerState();
        byteSize += PlayerState.GetByteSize() * DummyDataGenerator.RandomObjectLength;
        DebugConsole.Log($"{ClassName} will send {byteSize} Bytes or {byteSize/1000} KB data");
        SendDataClientRpc(levelObjects, 
            sequentialIds, v3r, teamStates, playerStates);
    }
    [ClientRpc]
    private void SendDataClientRpc(LevelObject[] levelObjects, ulong[] playersClientIds, 
        Vector3[] availablePositionsP, TeamState[] teamStates, PlayerState[] playerStates)
    {
        foreach (var item in levelObjects)
        {
            DebugConsole.Log($"{ClassName} on Data received: {item.m_prefabName}");
        }
    }

    private void SetP2pBtnInteractable(bool isInteractable)
    {
        startAsClientBtn.interactable = isInteractable;
        startAsHostBtn.interactable = isInteractable;
    }
}

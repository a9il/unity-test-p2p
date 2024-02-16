using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Debugger;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuCanvas : NetworkBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginWithDeviceBtn;
    [SerializeField] private Button loginWithUserNameBtn;
    [SerializeField] private Button startMatchmakingBtn;
    [SerializeField] private Button changeSceneBtn;
    [SerializeField] private Button sendClientRPCBtn;
    [SerializeField] private AccelByteNetworkTransportManager transportManager;
    [SerializeField] private Button sendToHostBtn;
    private const string ClassName = "[MenuCanvas]";
    private bool _isHost;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        var apiClient = MultiRegistry.GetApiClient();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = OnConnectionApproval;
        loginWithDeviceBtn.onClick.AddListener(OnLoginWithDeviceIdClicked);
        loginWithUserNameBtn.onClick.AddListener(OnLoginWithUsernameClicked);

        startMatchmakingBtn.onClick.AddListener(OnStartMatchmakingBtnClicked);
        changeSceneBtn.onClick.AddListener(OnChangeSceneBtnClicked);
        sendClientRPCBtn.onClick.AddListener(OnSendClientRpcBtnClicked);
        sendToHostBtn.onClick.AddListener(OnSendToHostBtnClicked);
        
        startMatchmakingBtn.interactable = false;
        changeSceneBtn.interactable = false;
        sendClientRPCBtn.interactable = false;
        sendToHostBtn.interactable = false;
        transportManager.Initialize(apiClient);
    }

    private void OnSendToHostBtnClicked()
    {
        //if (IsOwner)
        //{
        //    SendToHostServerRpc();
        //}
        SendToHostServerRpc();
    }
    [ServerRpc(RequireOwnership=false)]
    private void SendToHostServerRpc()
    {
        if (IsHost)
        {
            SendGeneratedData();
            //SendSingleClientRpc(new LevelObject(){
            //    ID = 10,
            //    m_position = new Vector3(10,1,2),
            //    m_prefabName = "prefab from server"
            //});
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        DebugConsole.Log($"{ClassName} onSceneEvent {sceneEvent.ToJsonString()}");
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            if (_isHost)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count == sceneEvent.ClientsThatCompleted.Count)
                {
                    if (IsServer)
                    {
                        SendGeneratedData();
                    }
                }

            }
        }
    }

    private void OnSendClientRpcBtnClicked()
    {
        if (_isHost)
        {
            //SendGeneratedData();
            SendSingleClientRpc(new LevelObject(){
                ID = 10,
                m_position = new Vector3(10,1,2),
                m_prefabName = "prefabName"
            });
        }
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
    [ClientRpc]
    private void SendSingleClientRpc(LevelObject levelObject)
    {
        Debug.Log($"single item received: {levelObject.m_prefabName}");
    }

    private void OnChangeSceneBtnClicked()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("game_scene", LoadSceneMode.Single);
    }

    private void OnStartMatchmakingBtnClicked()
    {
        startMatchmakingBtn.interactable = false;
        MatchmakingHelper.StartMatchmaking(OnMatchmakingCompleted);
    }

    private void OnMatchmakingCompleted(P2PMatchmakingResult result)
    {
        if (String.IsNullOrEmpty(result.ErrorMessage))
        {
            if(result.IsHost)
            {
                changeSceneBtn.interactable = true;
                sendClientRPCBtn.interactable = true;
            }
            else
            {
                sendToHostBtn.interactable = true;
            }
        }

        _isHost = result.IsHost;
    }

    private void OnLoginWithUsernameClicked()
    {
        SetLoginBtnEnabled(false);
        LoginHelper.Login(usernameInput.text, passwordInput.text, OnLoginCompleted);
    }

    private void OnLoginWithDeviceIdClicked()
    {
        SetLoginBtnEnabled(false);
        DebugConsole.Log("start log in with device id");
        LoginHelper.Login(OnLoginCompleted);
    }
    private void OnLoginCompleted(string errorMsg)
    {
        if (String.IsNullOrEmpty(errorMsg))
        {
            startMatchmakingBtn.interactable = true;
        }
        else
        {
            SetLoginBtnEnabled(true);
            DebugConsole.Log($"{ClassName} error login {errorMsg}");
        }
    }
    

    private void SetLoginBtnEnabled(bool isEnabled)
    {
        loginWithDeviceBtn.interactable = isEnabled;
        loginWithUserNameBtn.interactable = isEnabled;
    }
    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var initialConnectionData = FromByteArray<InitialConnectionData>(request.Payload);
        DebugConsole.Log($"{ClassName} ON CONNECTION APPROVAL request user id:{initialConnectionData.userId}");
        response.Approved = true;
        response.Pending = false;
    }
    private void OnClientDisconnected(ulong clientNetworkId)
    {
        DebugConsole.Log($"{ClassName} DISCONNECTED clientNetworkId:{clientNetworkId} " +
                         $"IsServer:{NetworkManager.Singleton.IsServer} IsHost:{NetworkManager.Singleton.IsHost}" +
                         $" IsClient:{NetworkManager.Singleton.IsClient}");
    }

    private void OnClientConnected(ulong clientNetworkId)
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        Debug.LogWarning($"{ClassName} CONNECTED clientNetworkId:{clientNetworkId} " +
                         $"IsServer:{NetworkManager.Singleton.IsServer} IsHost:{NetworkManager.Singleton.IsHost}" +
                         $" IsClient:{NetworkManager.Singleton.IsClient}");
    }
    
    public static byte[] ToByteArray(object source)
    {
        var formatter = new BinaryFormatter();
        using var stream = new MemoryStream();
        formatter.Serialize(stream, source);                
        return stream.ToArray();
    }
    
    public static T FromByteArray<T>(byte[] bytes)
    {
        var binaryFormatter = new BinaryFormatter();
        using var ms = new MemoryStream(bytes);
        object obj = binaryFormatter.Deserialize(ms);
        return (T)obj;
    }
}

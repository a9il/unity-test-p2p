using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using Debugger;
using TMPro;
using Unity.Netcode;

public class AccelbyteP2PTest : MonoBehaviour
{
    [SerializeField] private string username;
    [SerializeField] private string password;
    [SerializeField]
    private AccelByteNetworkTransportManager transportManager;

    [SerializeField] private TMP_InputField hostUserId;
    private User _user;
    private const string ClassName = "[AccelbyteP2PTest]";
    // Start is called before the first frame update
    void Start()
    {
        var apiClient = MultiRegistry.GetApiClient();
        _user = apiClient.GetApi<User, UserApi>();
    }

    private void OnLoginWithUserName()
    {
        DebugConsole.Log("start log in using username");
        _user.LoginWithUsernameV3(username, password, OnLoginCompleted);
    }

    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        DebugConsole.Log($"{ClassName} OnConnectionApproval request:{request.ToJsonString()}");
        response.Approved = true;
        response.Pending = false;
    }

    private void OnStartAsClient()
    {
        string hostUserIdStr = hostUserId.text;
        DebugConsole.Log("start client with host user id: "+hostUserIdStr);
        transportManager.SetTargetHostUserId(hostUserIdStr);
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientDisconnected(ulong clientNetworkId)
    {
        DebugConsole.Log($"{ClassName} DISCONNECTED clientNetworkId:{clientNetworkId} " +
                  $"IsServer:{NetworkManager.Singleton.IsServer} IsHost:{NetworkManager.Singleton.IsHost}" +
                  $" IsClient:{NetworkManager.Singleton.IsClient}");
    }

    private void OnClientConnected(ulong clientNetworkId)
    {
        DebugConsole.Log($"{ClassName} CONNECTED clientNetworkId:{clientNetworkId} " +
                  $"IsServer:{NetworkManager.Singleton.IsServer} IsHost:{NetworkManager.Singleton.IsHost}" +
                  $" IsClient:{NetworkManager.Singleton.IsClient}");
    }

    private void OnStartAsHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void OnLoginWithDeviceId()
    {
        DebugConsole.Log("start log in with device id");
        _user.LoginWithDeviceId(OnLoginCompleted);
    }

    private void OnLoginCompleted(Result<TokenData, OAuthError> result)
    {
        if (result.IsError)
        {
            DebugConsole.Log($"{ClassName} error login {result.Error.ToJsonString()}");
        }
        else
        {
            DebugConsole.Log($"{ClassName} login success user_id: {result.Value.user_id}");
            MultiRegistry.GetApiClient().GetLobby().Connect();
        }
    }
}

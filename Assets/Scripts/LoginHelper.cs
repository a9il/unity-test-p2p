using System;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Debugger;
using UnityEngine;

public class LoginHelper
{
    private static User _user;
    private static Lobby _lobby;
    private static Action<string> _onLoginCompleted;
    private const String ClassName = "[LoginHelper]";
    private static void Init()
    {
        if (_user != null) return;
        _lobby = MultiRegistry.GetApiClient().GetLobby();
        _lobby.Connected += OnLobbyConnected;
        _user = MultiRegistry.GetApiClient().GetUser();
    }

    private static void OnLobbyConnected()
    {
        if (_onLoginCompleted != null)
        {
            _onLoginCompleted("");
        }
        DebugConsole.Log($"{ClassName} connected to lobby");
    }

    public static void Login(Action<string> onLoginCompleted)
    {
        Init();
        _onLoginCompleted = onLoginCompleted;
        _user.LoginWithDeviceId(OnLoginCompleted);
    }

    public static void Login(string userName, string password, Action<string> onLoginCompleted)
    {
        Init();
        _onLoginCompleted = onLoginCompleted;
        _user.LoginWithUsernameV3(userName, password, OnLoginCompleted);
    }
    private static void OnLoginCompleted(Result<TokenData, OAuthError> result)
    {
        if (result.IsError)
        {
            string errorMsg = result.Error.error_description;
            DebugConsole.Log($"{ClassName} login error: {errorMsg}");
            _onLoginCompleted?.Invoke(errorMsg);
        }
        else
        {
            DebugConsole.Log($"{ClassName} login success userId: {result.Value.user_id} \n connecting to lobby");
            _lobby.Connect();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using Debugger;
using Unity.Netcode;
using UnityEngine;

public class MatchmakingHelper
{
    private static MatchmakingV2 _matchmaking;
    private static Session _session;
    private static Action<P2PMatchmakingResult> _onMatchmakingCompleted;
    private const string ClassName = "[MatchmakingHelper]";
    private static void Init()
    {
        if (_matchmaking != null) return;
        _session = MultiRegistry.GetApiClient().GetSession();
        var lobby = MultiRegistry.GetApiClient().GetLobby();
        lobby.MatchmakingV2MatchFound += OnMatchmakingFound;
        _matchmaking = MultiRegistry.GetApiClient().GetMatchmakingV2();
    }

    private static void OnMatchmakingFound(Result<MatchmakingV2MatchFoundNotification> result)
    {
        if (result.IsError)
        {
            var errorMsg = result.Error.Message;
            DebugConsole.Log($"{ClassName} error match found error: {errorMsg}");
            _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult(errorMsg));
        }
        else
        {
            _session.JoinGameSession(result.Value.id, OnJoinedSession);
        }
    }

    private static void OnJoinedSession(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            var errorMsg = result.Error.Message;
            DebugConsole.Log($"{ClassName} error join session: {errorMsg}");
            _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult(errorMsg));
        }
        else
        {
            DebugConsole.Log($"{ClassName} joined session {result.Value.id}");
            var leaderId = result.Value.leaderId;
            var userId = MultiRegistry.GetApiClient().session.UserId;
            if (userId.Equals(leaderId))
            {
                StartAsHost();
            }
            else
            {
                StartAsClient(leaderId);
            }
        }
    }

    private static void StartAsHost()
    {
        DebugConsole.Log($"{ClassName} Start As Host");
        // AccelByteServerPlugin.GetDedicatedServer().LoginWithClientCredentials(OnDSLoginCompleted);
        var initData = new InitialConnectionData(MultiRegistry.GetApiClient().session.UserId);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = MenuCanvas.ToByteArray(initData);
        NetworkManager.Singleton.StartHost();
        _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult("", true));
    }

    private static void OnDSLoginCompleted(Result result)
    {
        if (result.IsError)
        {
            DebugConsole.Log($"{ClassName} OnDSLoginCompleted error:{result.Error.Message}");
            _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult(result.Error.Message));
        }
        else
        {
            var initData = new InitialConnectionData(MultiRegistry.GetApiClient().session.UserId);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = MenuCanvas.ToByteArray(initData);
            NetworkManager.Singleton.StartHost();
            _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult("", true));
        }
    }

    private static void StartAsClient(string targetHostUserId)
    {
        DebugConsole.Log($"{ClassName} Start as client host id {targetHostUserId}");
        var initData = new InitialConnectionData(MultiRegistry.GetApiClient().session.UserId);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = MenuCanvas.ToByteArray(initData);
        var networkTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (networkTransport is AccelByteNetworkTransportManager transportManager)
        {
            transportManager.SetTargetHostUserId(targetHostUserId);
            NetworkManager.Singleton.StartClient();
            _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult(""));
        }
        else
        {
            DebugConsole.Log($"{ClassName} no transport manager");
            _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult("no transport manager"));
        }
    }

    public static void StartMatchmaking(Action<P2PMatchmakingResult> onCompleted)
    {
        Init();
        _onMatchmakingCompleted = onCompleted;
        _matchmaking
            .CreateMatchmakingTicket("unity-elimination-p2p", null, OnMatchmakingCompleted);
    }

    private static void OnMatchmakingCompleted(Result<MatchmakingV2CreateTicketResponse> result)
    {
        if (result.IsError)
        {
            var errorMsg = result.Error.Message;
            DebugConsole.Log($"{ClassName} matchmaking error: {errorMsg}");
            _onMatchmakingCompleted?.Invoke(new P2PMatchmakingResult(errorMsg));
        }
        else
        {
            DebugConsole.Log($"{ClassName} matchmaking ticket created id: {result.Value.matchTicketId}");
        }
    }
}

public struct P2PMatchmakingResult
{
    public P2PMatchmakingResult(string errorMessage, bool isHost=false)
    {
        ErrorMessage = errorMessage;
        IsHost = isHost;
    }
    public string ErrorMessage;
    public bool IsHost;
}

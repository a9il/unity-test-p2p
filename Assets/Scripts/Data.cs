using System;
using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelObject : INetworkSerializable
{
    public string m_prefabName;
    public Vector3 m_position;
    public Quaternion m_rotation;
    public int ID;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref m_prefabName);
        serializer.SerializeValue(ref m_position);
        serializer.SerializeValue(ref m_rotation);
        serializer.SerializeValue(ref ID);
    }

    public static int GetByteSize()
    {
        int byteSize = 0;
        byteSize += DummyDataGenerator.RandomStringLength * sizeof(char) + sizeof(int);
        byteSize += Marshal.SizeOf(typeof(Vector3));
        byteSize += Marshal.SizeOf(typeof(Quaternion));
        byteSize += sizeof(int);
        return byteSize;
    }
}

public class TeamState : INetworkSerializable
{
    public Color teamColour;
    public int teamIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref teamColour);
        serializer.SerializeValue(ref teamIndex);
    }

    public static int GetByteSize()
    {
        int byteSize = 0;
        byteSize += Marshal.SizeOf(typeof(Color));
        byteSize += sizeof(int); 
        return byteSize;
    }
}

public class PlayerState : INetworkSerializable
{
    public string playerName;
    public int playerIndex;
    public int numMissilesFired;
    public float score;
    public int killCount;
    public int lives;
    public int teamIndex;
    public ulong clientNetworkId;
    public string sessionId;
    public Vector3 position;
    public string playerId;
    public string avatarUrl;
    public PlayerState()
    {
        playerName = "";
        sessionId = "";
        position = Vector3.zero;
        playerId = "";
        avatarUrl = "";
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerIndex);
        serializer.SerializeValue(ref numMissilesFired);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref killCount);
        serializer.SerializeValue(ref lives);
        serializer.SerializeValue(ref teamIndex);
        serializer.SerializeValue(ref clientNetworkId);
        serializer.SerializeValue(ref sessionId);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref avatarUrl);
    }

    public string GetPlayerName()
    {
        if (String.IsNullOrEmpty(playerName))
        {
            return "ByteWarrior " + clientNetworkId;
        }
        return playerName;
    }

    public static int GetByteSize()
    {
        int byteSize = 0;
        byteSize += DummyDataGenerator.RandomStringLength * sizeof(char) + sizeof(int);
        byteSize += 5 * sizeof(int);
        byteSize += sizeof(ulong) + sizeof(float);
        byteSize += Marshal.SizeOf(typeof(Vector3));
        return byteSize;
    }
}

public static class DummyDataGenerator
{
    private const string Glyphs= "abcdefghijklmnopqrstuvwxyz0123456789 ";
    public const int RandomObjectLength = 40;
    public const int RandomStringLength = 10;
    private static string GenerateRandomString(int stringLength)
    {
        string result = "";
        for (int i = 0; i < stringLength; i++)
        {
            result += Glyphs[Random.Range(0, Glyphs.Length)];
        }
        return result;
    }

    public static LevelObject[] GenerateRandomLevelObject()
    {

        LevelObject[] results = new LevelObject[RandomObjectLength];
        for (int i = 0; i < RandomObjectLength; i++)
        {
            results[i] = new LevelObject()
            {
                ID = i,
                m_position = new Vector3(1+i, 2+i, 3+i),
                m_prefabName = GenerateRandomString(RandomStringLength),
                m_rotation = Quaternion.identity
            };
        }
        return results;
    }

    public static ulong[] GenerateSequentialId()
    {
        ulong[] result = new ulong[RandomObjectLength];
        for (int i = 0; i < RandomObjectLength; i++)
        {
            result[i] = (ulong)(i+1);
        }
        return result;
    }

    public static Vector3[] GenerateVector3()
    {
        Vector3[] result = new Vector3[RandomObjectLength];
        for (int i = 0; i < RandomObjectLength; i++)
        {
            result[i] = new Vector3(0 + i, 1 + i, 2 + i);
        }
        return result;
    }

    public static TeamState[] GenerateTeamState()
    {
        TeamState[] teamStates = new TeamState[RandomObjectLength];
        for (int i = 0; i < RandomObjectLength; i++)
        {
            teamStates[i] = new TeamState()
            {
                teamColour = Color.blue,
                teamIndex = i
            };
        }
        return teamStates;
    }

    public static PlayerState[] GeneratePlayerState()
    {
        PlayerState[] playerStates = new PlayerState[RandomObjectLength];
        for (int i = 0; i < RandomObjectLength; i++)
        {
            playerStates[i] = new PlayerState()
            {
                playerId = GenerateRandomString(RandomStringLength)
            };
        }
        return playerStates;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InitialConnectionData
{
    public InitialConnectionData(string userID)
    {
        userId = userID;
    }
    public string userId;
}

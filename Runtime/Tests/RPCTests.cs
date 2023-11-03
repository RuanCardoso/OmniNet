using Omni.Core;
using System;
using UnityEngine;
using Logger = Omni.Core.Logger;

public class RPCTests : OmniObject
{
    [SerializeField] private SyncValue<float> m_Health;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && IsMine)
        {
            Remote(1, new ByteStream(0), cacheMode: Enums.CacheMode.Overwrite);
        }

        if (Input.GetKeyDown(KeyCode.C) && IsMine)
        {
            OmniNetwork.GetCache(Enums.CacheType.Remote, true, 1, false, Enums.Channel.Unreliable);
        }
    }

    [Remote(1)]
    private void RPCTest(ByteStream parameters, UInt16 fromId, UInt16 toId, RemoteStats stats)
    {
        Logger.Print("Opa! Recebir em (:");
    }
}
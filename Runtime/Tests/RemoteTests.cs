using Omni.Core;
using UnityEngine;

public class RemoteTests : OmniObject
{
    private void Update()
    {
        if (IsMine)
        {
            if (Input.GetKey(KeyCode.R))
            {
                Remote(1, new ByteStream(0), cacheMode: Enums.CacheMode.Overwrite);
            }
        }
    }

    [Remote(1)]
    public void RemoteEg(ByteStream byteStream, ushort fromId, ushort toId, RemoteStats stats)
    {
        OmniLogger.Print("Remote Eg (:");
    }
}

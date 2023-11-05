using Omni.Core;
using UnityEngine;

public class GlobalRemoteTests : OmniBehaviour
{
    protected override byte Id => 1;

    private void Update()
    {
        if (Input.GetKey(KeyCode.G))
        {
            Remote(1, new ByteStream(0), false, cacheMode: Enums.CacheMode.Overwrite);
        }
    }

    [Remote(1)]
    public void RemoteEg(ByteStream byteStream, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
    {
        OmniLogger.Print("Global Remote Eg");
    }
}

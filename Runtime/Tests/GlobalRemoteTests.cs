using Omni.Core;
using UnityEngine;

public class GlobalRemoteTests : OmniBehaviour
{
    protected override byte Id => 1;

    private void Update()
    {
        if (Input.GetKey(KeyCode.G))
        {
            Remote(1, new DataIOHandler(0), false, cachingOption: Enums.DataCachingOption.Overwrite);
        }
    }

    [Remote(1)]
    public void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
    {
        OmniLogger.Print("Global Remote Eg");
    }
}

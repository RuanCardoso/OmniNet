using Omni.Core;
using UnityEngine;

namespace Omni.Tests
{
    public class RemoteTests : OmniObject
    {
        private void Update()
        {
            if (IsMine)
            {
                if (Input.GetKey(KeyCode.R))
                {
                    Remote(1, new DataIOHandler(0), cachingOption: Enums.DataCachingOption.Overwrite, deliveryMode: Enums.DataDeliveryMode.Unsecured);
                }
            }
        }

        [Remote(1)]
        public void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
        {
            OmniLogger.Print("Remote Eg (:");
        }
    }
}

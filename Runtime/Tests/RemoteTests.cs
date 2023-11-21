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
                if (Input.GetKeyDown(KeyCode.R))
                {
                    var IOHandler = Get;
                    IOHandler.Write("Unity Newbies (:");
                    Remote(1, IOHandler, Enums.DataDeliveryMode.SecuredWithAes, Enums.DataTarget.BroadcastExcludingSelf, Enums.DataProcessingOption.ProcessOnServer, Enums.DataCachingOption.Overwrite);
                }
            }
        }

        [Remote(1)]
        public void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
        {
            OmniLogger.Print(IOHandler.ReadString());
        }
    }
}

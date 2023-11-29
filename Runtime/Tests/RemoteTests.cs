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
                if (Input.GetKeyDown(KeyCode.A))
                {
                    var IOHandler = Get;
                    IOHandler.Write("Mensagem de teste! Testando o envio de mensagens remotas! Ebaaa!");
                    Remote(1, IOHandler, Enums.DataDeliveryMode.SecuredWithAes, Enums.DataTarget.Self, Enums.DataProcessingOption.ProcessOnServer, Enums.DataCachingOption.Overwrite);
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    var IOHandler = Get;
                    IOHandler.Write("Mensagem de teste! Testando o envio de mensagens remotas! Ebaaa!");
                    Remote(1, IOHandler, Enums.DataDeliveryMode.Secured, Enums.DataTarget.Self, Enums.DataProcessingOption.ProcessOnServer, Enums.DataCachingOption.Overwrite);
                }

                for (int i = 0; i < 0; i++)
                {
                    //if (Input.GetKeyDown(KeyCode.U))
                    {
                        var IOHandler = Get;
                        IOHandler.WriteWithoutAllocation("Mensagem de teste! Testando o envio de mensagens remotas! Ebaaa!");
                        Remote(1, IOHandler, Enums.DataDeliveryMode.Unsecured, Enums.DataTarget.BroadcastExcludingSelf, Enums.DataProcessingOption.ProcessOnServer, Enums.DataCachingOption.Overwrite);
                    }
                }
            }
        }

        [Remote(1)]
        public void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
        {
            OmniLogger.Print(IOHandler.ReadStringWithoutAllocation());
        }
    }
}

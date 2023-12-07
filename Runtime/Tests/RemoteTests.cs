using Omni.Core;
using UnityEngine;

namespace Omni.Tests
{
    public partial class RemoteTests : OmniObject
    {
        protected override void Start()
        {
            base.Start();
        }

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
        partial void RPC1(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats);
    }
}

// Definindo uma classe com um método parcial
public partial class Exemplo
{
    // Parte 1 do método
    public partial void ExibirMensagem(string mensagem);

    // Parte 2 do método
    public partial void ExibirMensagem(string mensagem)
    {
       
    }

    // Outros membros da classe...
}
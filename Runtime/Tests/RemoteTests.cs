using Omni.Core;

namespace Omni.Tests
{
	public partial class RemoteTests : OmniObject
	{
		//public void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
		//{
		//	throw new System.NotImplementedException();
		//}

		protected override void Start()
		{
			base.Start();
		}

		private void Update()
		{
			//if (IsMine)
			//{
			//    if (Input.GetKeyDown(KeyCode.A))
			//    {
			//        var IOHandler = Get;
			//        IOHandler.Write("Mensagem de teste! Testando o envio de mensagens remotas! Ebaaa!");
			//        Remote(1, IOHandler, Enums.DataDeliveryMode.SecuredWithAes, Enums.DataTarget.Self, Enums.DataProcessingOption.ProcessOnServer, Enums.DataCachingOption.Overwrite);
			//    }

			//    if (Input.GetKeyDown(KeyCode.R))
			//    {
			//        var IOHandler = Get;
			//        IOHandler.Write("Mensagem de teste! Testando o envio de mensagens remotas! Ebaaa!");
			//        Remote(1, IOHandler, Enums.DataDeliveryMode.Secured, Enums.DataTarget.Self, Enums.DataProcessingOption.ProcessOnServer, Enums.DataCachingOption.Overwrite);
			//    }

			//    for (int i = 0; i < 0; i++)
			//    {
			//        //if (Input.GetKeyDown(KeyCode.U))
			//        {
			//            var IOHandler = Get;
			//            IOHandler.WriteWithoutAllocation("Mensagem de teste! Testando o envio de mensagens remotas! Ebaaa!");
			//            Remote(1, IOHandler, Enums.DataDeliveryMode.Unsecured, Enums.DataTarget.BroadcastExcludingSelf, Enums.DataProcessingOption.ProcessOnServer, Enums.DataCachingOption.Overwrite);
			//        }
			//    }
			//}
		}

		//      [Remote(1)]
		//      partial void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats);

		//public override void RemoteEgServerLogic(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
		//{
		//	base.RemoteEgServerLogic(IOHandler, fromId, toId, stats);
		//}

		//public override void RemoteEgClientLogic(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
		//{
		//	base.RemoteEgClientLogic(IOHandler, fromId, toId, stats);
		//}
	}
}
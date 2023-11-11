using Omni.Core;
using UnityEngine;

public class SendMessageTests : OmniObject
{
    // Start is called before the first frame update
    void Awake()
    {
        Identity.OnAfterRegistered += () =>
        {
            if (IsMine)
            {
                GlobalEventHandler.OnMessageReceived += GlobalEventHandler_OnMessageReceived;
            }
        };
    }

    private void GlobalEventHandler_OnMessageReceived(DataIOHandler arg1, ushort fromId, bool arg2)
    {
        OmniLogger.Print(arg2);
        OmniLogger.Print(arg1.ReadString());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && IsMine)
        {
            var IOHandler = DataIOHandler.Get();
            IOHandler.Write("Ruan");
            OmniNetwork.SendMessage(IOHandler, PlayerId, target: Enums.DataTarget.Self, fromServer: true);
        }
    }
}

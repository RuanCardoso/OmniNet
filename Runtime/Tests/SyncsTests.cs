using Omni.Core;
using UnityEngine;

public class SyncsTests : OmniObject
{
    [SerializeField] private SyncValue<bool> value;

    void Start()
    {
        value = new SyncValue<bool>(this, false, (value) =>
        {
            OmniLogger.Print(value);
        }, Enums.DataDeliveryMode.Unsecured, Enums.DataTarget. Broadcast, Enums.DataProcessingOption.DoNotProcessOnServer, Enums.DataCachingOption.Overwrite, Enums.AuthorityMode.Server);
    }
}

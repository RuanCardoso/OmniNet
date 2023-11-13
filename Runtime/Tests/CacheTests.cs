using Omni.Core;
using UnityEngine;

namespace Omni.Tests
{
    public class CacheTests : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                OmniNetwork.GetCache(Enums.DataStorageType.GlobalRemote, true, 1, false, Enums.DataDeliveryMode.Unsecured);
                OmniNetwork.GetCache(Enums.DataStorageType.Remote, true, 1, false, Enums.DataDeliveryMode.Unsecured);
                OmniNetwork.GetCache(Enums.DataStorageType.NetworkVars, true, 1, false, Enums.DataDeliveryMode.Unsecured);
            }
        }
    }
}
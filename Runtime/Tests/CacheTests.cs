using Omni.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            OmniNetwork.GetCache(Enums.CacheType.GlobalRemote, true, 1, false, Enums.Channel.Unreliable);
            OmniNetwork.GetCache(Enums.CacheType.Remote, true, 1, false, Enums.Channel.Unreliable);
        }
    }
}

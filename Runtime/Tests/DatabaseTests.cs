using Neutron.Core;
using UnityEngine;

public class DatabaseTests : MonoBehaviour
{
    private SGBDManager Manager = new((db) => db.Initialize("Users"), 5);
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // var dB = Manager.Get().Db;
            // dB.Insert();
            // Manager.Release(dB);
        }
    }
}
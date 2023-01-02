/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using Neutron.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neutron.Tests
{
    [AddComponentMenu("")]
    public class SpawnPlayerTests : NeutronObject
    {
        [SerializeField] private NeutronIdentity objectToSpawn;
        private void Start()
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Lobby 1"));
            if (IsMine)
            {
                SpawnRemote(transform.position, Quaternion.identity, (parameters) =>
                {
                    parameters.Write("Ruan");
                }, subTarget: Enums.SubTarget.Server);
            }
        }

        protected override NeutronIdentity OnSpawnedObject(Vector3 position, Quaternion rotation, ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
        {
            return Instantiate(objectToSpawn, position, rotation);
        }
    }
}
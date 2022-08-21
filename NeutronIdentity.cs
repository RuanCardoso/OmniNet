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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    public class NeutronIdentity : ActionDispatcher
    {
        [SerializeField] private ushort id;
        [SerializeField] private ushort playerId;
        [SerializeField] private NeutronObjectType objectType = NeutronObjectType.Player;
        [SerializeField] internal bool isItFromTheServer;
        private bool isInRoot = false;
        private readonly Dictionary<(ushort, ushort, byte, byte, NeutronObjectType), Action> iRPCMethods = new(); // playerid, identityid, instance id, rpcId, type

        protected virtual void Awake()
        {
            isInRoot = transform == transform.root;
        }

        protected virtual void Start()
        {
#if UNITY_EDITOR
            if (isInRoot && !isItFromTheServer && objectType == NeutronObjectType.Static)
            {
                GameObject serverObject = Instantiate(gameObject);
                serverObject.name = $"{gameObject.name}_Server";
                NeutronIdentity serverIdentity = serverObject.GetComponent<NeutronIdentity>();
                serverIdentity.isItFromTheServer = true;
                SceneManager.MoveGameObjectToScene(serverObject, SceneManager.GetSceneByName("Server"));
            }
#endif
        }

        internal void AddRpc(byte instanceId, byte rpcId, Action method)
        {
            if (!iRPCMethods.TryAdd((playerId, id, instanceId, rpcId, objectType), method))
                Logger.PrintError($"The RPC {instanceId}:{rpcId} is already registered.");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (objectType == NeutronObjectType.Static)
                {
                    if (!(isInRoot = transform == transform.root))
                        Logger.PrintError($"{gameObject.name} -> Only root objects can have a NeutronIdentity component.");
                    if (isInRoot)
                    {
                        NeutronIdentity[] identities = FindObjectsOfType<NeutronIdentity>(true);
                        if (id == 0) id = (ushort)Helper.GetAvailableId(identities, x => x.id, short.MaxValue);
                        else
                        {
                            int count = identities.Count(x => x.id == id);
                            if (count > 1) id = 0;
                        }
                    }
                }
                else id = 0;
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;
            if (!isItFromTheServer) position.y += 0.2f;
            else position.y -= 0.2f;
            UnityEditor.Handles.Label(position, $"{(isItFromTheServer ? "Server -> " : "Client -> ")} {id}");
        }
#endif
    }
}
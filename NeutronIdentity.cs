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
    [DefaultExecutionOrder(-0x63)]
    public class NeutronIdentity : ActionDispatcher
    {
        internal static readonly Dictionary<(ushort, ObjectType, bool), NeutronIdentity> identities = new(); // identity, object type, server or client!
        internal static readonly Dictionary<(ushort, ushort, ObjectType, bool), NeutronIdentity> dynamicIdentities = new(); // playerid, identity, object type, server or client!
        [SerializeField] internal bool isRegistered;
        [SerializeField] internal ushort id;
        [SerializeField] internal ushort playerId;
        [SerializeField] internal ObjectType objectType = ObjectType.Player;
        [SerializeField] internal bool isItFromTheServer;
        private bool isInRoot = false;
        private readonly Dictionary<(ushort, ushort, byte, byte, ObjectType), Action> iRPCMethods = new(); // playerid, identityid, instance id, rpcId, type

        protected virtual void Awake()
        {
            isInRoot = transform == transform.root;
        }

        protected virtual void Start()
        {
#if UNITY_EDITOR
            if (isInRoot && !isItFromTheServer)
            {
                GameObject serverObject = Instantiate(gameObject);
                serverObject.name = $"{gameObject.name}_Server";
                NeutronIdentity serverIdentity = serverObject.GetComponent<NeutronIdentity>();
                serverIdentity.isItFromTheServer = true;
                SceneManager.MoveGameObjectToScene(serverObject, SceneManager.GetSceneByName("Server"));
            }
#endif
            if (isInRoot && isRegistered)
            {
                switch (objectType)
                {
                    case ObjectType.Player:
                    case ObjectType.Scene:
                        if (!identities.TryAdd((id, objectType, isItFromTheServer), this))
                            Logger.PrintError($"NeutronIdentity: {id} | {objectType} | {isItFromTheServer} already exists!");
                        break;
                    case ObjectType.Instantiated:
                        if (!dynamicIdentities.TryAdd((playerId, id, objectType, isItFromTheServer), this))
                            Logger.PrintError($"NeutronIdentity: {playerId} | {id} | {objectType} | {isItFromTheServer} already exists!");
                        break;
                }
            }
        }

        private void RegisterIdentity()
        {
            if (isInRoot && !isRegistered && objectType != ObjectType.Scene)
            {
                switch (objectType)
                {
                    case ObjectType.Player:
                        playerId = id = (ushort)NeutronNetwork.Id;
                        break;
                    case ObjectType.Instantiated:
                        playerId = (ushort)NeutronNetwork.Id;
                        break;
                }

                isRegistered = true;
            }
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
                if (objectType == ObjectType.Scene)
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
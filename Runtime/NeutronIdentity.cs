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
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using static Dapper.SqlMapper;

namespace Neutron.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x63)]
    public class NeutronIdentity : ActionDispatcher
    {
        //internal static readonly Dictionary<(ushort, ObjectType, bool), NeutronIdentity> identities = new(); // identity, object type, server or client!
        //internal static readonly Dictionary<(ushort, ushort, ObjectType, bool), NeutronIdentity> dynamicIdentities = new(); // playerid, identity, object type, server or client!

        [SerializeField] internal bool isRegistered;
        [SerializeField] internal ushort id;
        [SerializeField] internal ushort playerId;
        [SerializeField] internal ObjectType objectType = ObjectType.Player;
        [SerializeField] internal bool isItFromTheServer;
        [Header("[Editor]")]
        [SerializeField] private bool simulateServerObj = true;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] internal bool rootMode = true;

        private bool isInRoot = false;
        private readonly Dictionary<(ushort, ushort, byte, byte, ObjectType), Action> remoteMethods = new(); // playerid, identityid, instance id, rpcId, type

        protected virtual void Awake() => isInRoot = (transform == transform.root) || !rootMode;
        protected virtual void Start()
        {
#if UNITY_EDITOR
            if (isInRoot && !isItFromTheServer && simulateServerObj)
            {
                if (objectType == ObjectType.Scene || objectType == ObjectType.Static)
                {
                    GameObject serverObject = Instantiate(gameObject);
                    serverObject.name = $"{gameObject.name} -> [Server]";
                    NeutronIdentity identity = serverObject.GetComponent<NeutronIdentity>();
                    identity.isItFromTheServer = true;
                    SceneManager.MoveGameObjectToScene(serverObject, SceneManager.GetSceneByName("Server"));
                }
            }
#endif
            //if (isInRoot && isRegistered)
            //{
            //    switch (objectType)
            //    {
            //        case ObjectType.Player:
            //        case ObjectType.Scene:
            //            if (!identities.TryAdd((id, objectType, isItFromTheServer), this))
            //                Logger.PrintError($"NeutronIdentity: {id} | {objectType} | {isItFromTheServer} already exists!");
            //            break;
            //        case ObjectType.Instantiated:
            //            if (!dynamicIdentities.TryAdd((playerId, id, objectType, isItFromTheServer), this))
            //                Logger.PrintError($"NeutronIdentity: {playerId} | {id} | {objectType} | {isItFromTheServer} already exists!");
            //            break;
            //    }
            //}
        }

        private void Register()
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
            if (!remoteMethods.TryAdd((playerId, id, instanceId, rpcId, objectType), method))
                Logger.PrintError($"The RPC {instanceId}:{rpcId} is already registered.");
        }

#if UNITY_EDITOR
        private ObjectType VAL_OBJ_TYPE;
        private void Reset()
        {
            var nObjects = transform.GetComponentsInChildren<NeutronObject>();
            for (int i = 0; i < nObjects.Length; i++)
            {
                var nObject = nObjects[i];
                nObject.OnValidate();
            }

            VAL_OBJ_TYPE = objectType;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (!(isInRoot = (transform == transform.root) || !rootMode))
                    Logger.PrintError($"{gameObject.name} -> Only root objects can have a NeutronIdentity component.");

                if (objectType == ObjectType.Scene || objectType == ObjectType.Static)
                {
                    if (VAL_OBJ_TYPE != objectType)
                    {
                        id = 0;
                        VAL_OBJ_TYPE = objectType;
                    }

                    if (isInRoot)
                    {
                        NeutronIdentity[] identities = FindObjectsOfType<NeutronIdentity>(true).Where(x => x.objectType == objectType).ToArray();
                        if (id == 0) id = (ushort)Helper.GetAvailableId(identities, x => x.id, short.MaxValue);
                        else
                        {
                            int count = identities.Count(x => x.id == id);
                            if (count > 1) id = 0;
                        }
                    }
                    else Logger.PrintError($"{transform.name} -> [IsRoot]={isInRoot}");
                }
                else id = 0;
            }
        }

        [ContextMenu("Re-order by object type")]
        private void Reorder()
        {
            if (!Application.isPlaying)
            {
                NeutronIdentity[] identities = FindObjectsOfType<NeutronIdentity>(true).Where(x => x.objectType == objectType).OrderBy(x => x.transform.GetSiblingIndex()).ToArray();
                for (int i = 0; i < identities.Length; i++)
                {
                    var identity = identities[i];
                    identity.id = (ushort)(i + 1);
                    EditorUtility.SetDirty(identity.gameObject);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (drawGizmos)
            {
                Vector3 position = transform.position;
                if (!isItFromTheServer) position.y += 0.2f;
                else position.y -= 0.2f;
                Handles.Label(position, $"{(isItFromTheServer ? "Server -> " : "Client -> ")} {id}");
            }
        }
#endif
    }
}
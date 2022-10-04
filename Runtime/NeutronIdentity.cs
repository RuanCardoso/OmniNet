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
        [SerializeField] internal bool isRegistered;
        [SerializeField] internal ushort id;
        [SerializeField] internal ushort playerId;
        [SerializeField] internal ObjectType objectType = ObjectType.Player;
        [SerializeField] internal bool isItFromTheServer;
        [Header("[Editor]")]
        [SerializeField] private bool simulateServerObj = true;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] internal bool rootMode = true;
#if UNITY_EDITOR
        [SerializeField][HideInInspector] private bool isCloned = false;
#endif

        private bool isInRoot = false;
        private readonly Dictionary<(byte rpcId, byte instanceId), Action> remoteMethods = new(); // [rpc id, instanceId]

        protected virtual void Awake()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            isItFromTheServer = true;
#endif

            if (!NeutronNetwork.IsConnected)
            {
                Logger.PrintError("Neutron is not connected!");
                Destroy(transform.root.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                Clone();
#endif
                Register();
                isInRoot = (transform == transform.root) || !rootMode;
            }
        }

#if UNITY_EDITOR
        private void Clone()
        {
            if (objectType == ObjectType.Scene || objectType == ObjectType.Static)
            {
                if (simulateServerObj)
                {
                    if (!isCloned)
                    {
                        isCloned = true;
                        Instantiate(gameObject);
                        name = $"{gameObject.name} -> [Client]";
                    }
                    else
                    {
                        isItFromTheServer = true;
                        name = $"{gameObject.name.Replace("(Clone)", "")} -> [Server]";
                        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("Server"));
                    }
                }
            }
        }
#endif

        private void Register()
        {
            if (!isRegistered)
            {
                switch (objectType)
                {
                    case ObjectType.Static:
                    case ObjectType.Scene:
                        isRegistered = true;
                        playerId = isItFromTheServer ? NeutronNetwork.ServerId : (ushort)NeutronNetwork.Id;
                        break;
                }

                NeutronNetwork.AddIdentity(this);
            }
            else Logger.PrintError("This object is already registered!");
        }

        internal void AddRpc(byte instanceId, byte rpcId, Action method)
        {
            if (!remoteMethods.TryAdd((rpcId, instanceId), method))
                Logger.PrintError($"The RPC {instanceId}:{rpcId} is already registered.");
        }

        internal Action GetRpc(byte instanceId, byte rpcId)
        {
            var key = (rpcId, instanceId);
            if (!remoteMethods.TryGetValue(key, out Action value))
                Logger.PrintWarning($"RPC does not exist! -> {key} -> [IsServer]={isItFromTheServer}");
            return value;
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
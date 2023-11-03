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

using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using static Omni.Core.Enums;

namespace Omni.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-0x63)]
    public sealed class OmniIdentity : OmniDispatcher
    {
        public event Action OnAfterRegistered;

        [SerializeField][HideInInspector] internal bool itIsRegistered;
        [ValidateInput("WarnIfNotRoot", "Root mode is enabled, but the identity is not on the root object.")]
        [Header("Registration")]
        [SerializeField][ReadOnly] internal ushort id;
        [SerializeField][ReadOnly] internal ushort playerId;
        [SerializeField][ReadOnly][ShowIf("objectType", ObjectType.Scene)] internal byte sceneId;
        [SerializeField][Label("Mode")] internal ObjectType objectType = ObjectType.Player;
        [SerializeField][ReadOnly] private OmniObject[] networks;
#if UNITY_SERVER && !UNITY_EDITOR
        [NonSerialized]
        internal bool isItFromTheServer = true;
#else
        [SerializeField]
        [HideInInspector]
        internal bool isItFromTheServer;
#endif
        [Header("Editor")]
        [SerializeField] private bool simulateServerObj = true;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] internal bool rootMode = true;
#if UNITY_EDITOR
        [SerializeField][HideInInspector] private bool loop = false;
#endif

        private bool isInRoot = false;
        private readonly Dictionary<(byte rpcId, byte instanceId), Action<ByteStream, ushort, ushort, RemoteStats>> remoteMethods = new(); // [rpc id, instanceId]
        private Dictionary<byte, OmniObject> omniObjects;

        private Transform RootOr() => rootMode ? transform.root : transform;
#pragma warning disable IDE0051
        private bool WarnIfNotRoot() => !rootMode || transform == transform.root;
#pragma warning restore IDE0051
        private void Awake()
        {
            if (!OmniNetwork.IsConnected)
            {
                Logger.PrintError("Omni is not connected!");
                Destroy(RootOr().gameObject);
            }
            else
            {
                if (networks != null && networks.Length > 0)
                {
                    omniObjects = networks.Where(x => x != null).ToDictionary(k => k.id, v => v);
                    foreach (var nO in omniObjects.Values)
                        nO.OnAwake();
                }
                else { }
#if UNITY_EDITOR
                Clone();
#endif
                Register();
                isInRoot = transform == RootOr();
            }
        }

#if UNITY_EDITOR // Visuale
        private void DrawGizmos()
        {
            if (drawGizmos)
            {
                Vector3 position = transform.position;
                if (!isItFromTheServer) position.y += 0.1f;
                else position.y -= 0.1f;
                //----------------------------------------------------------
                GameObject @object = new("@object");
                @object.transform.position = position;
                @object.transform.localScale = new(0.01f, 0.01f, 0.01f);
                @object.transform.parent = transform;
                //----------------------------------------------------------
                Canvas canvas = @object.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                //----------------------------------------------------------
                TextMesh text = @object.AddComponent<TextMesh>();
                text.text = $"{(isItFromTheServer ? "Server -> " : "Client -> ")} {id}";
                text.fontStyle = FontStyle.Bold;
                text.fontSize = 90;
                text.color = !isItFromTheServer ? Color.white : Color.red;
            }
        }

        private void Clone()
        {
            if (OmniNetwork.IsBind)
            {
                if (objectType == ObjectType.Scene || objectType == ObjectType.Static)
                {
                    if (simulateServerObj)
                    {
                        if (!loop)
                        {
                            loop = true;
                            Instantiate(gameObject);
                            name = $"{gameObject.name} -> [Client]";
                        }
                        else
                        {
                            isItFromTheServer = true;
                            name = $"{gameObject.name.Replace("(Clone)", "")} -> [Server]";
                        }
                    }
                }
            }
        }
#endif

        public OmniObject GetOmniObject(byte id) => omniObjects[id];
        private void Register()
        {
            if (objectType == ObjectType.Scene || objectType == ObjectType.Static)
            {
                if (!itIsRegistered)
                {
                    itIsRegistered = true;
                    playerId = isItFromTheServer ? OmniNetwork.NetworkId : (ushort)OmniNetwork.Id;
                    OmniNetwork.AddIdentity(this);
                    OnAfterRegistered?.Invoke();
                    OnAfterRegistered = null;
#if UNITY_EDITOR
                    OmniHelper.MoveToServer(isItFromTheServer, gameObject);
                    DrawGizmos();
#endif
                }
                else Logger.PrintError("This object is already registered!");
            }
        }

        internal void Register(bool isServer, ushort playerId) => Register(isServer, playerId, 0);
        internal void Register(bool isServer, ushort playerId, ushort id = 0)
        {
            if (objectType == ObjectType.Player || objectType == ObjectType.Dynamic)
            {
                if (objectType == ObjectType.Dynamic && id == 0)
                {
                    Debug.LogError("it is necessary to register a unique id for the dynamically instantiated object!");
                    Destroy(gameObject);
                }
                else
                {
                    isItFromTheServer = isServer;
                    if (!itIsRegistered)
                    {
                        itIsRegistered = true;
                        this.playerId = playerId;
                        this.id = objectType == ObjectType.Player ? playerId : id;
                        #region Visuale
#if UNITY_EDITOR
                        name = isServer ? $"{gameObject.name.Replace("(Clone)", "")} -> [Server]" : $"{gameObject.name.Replace("(Clone)", "")} -> [Client]";
#endif
                        #endregion
                        OmniNetwork.AddIdentity(this);
                        OnAfterRegistered?.Invoke();
                        OnAfterRegistered = null;
#if UNITY_EDITOR
                        OmniHelper.MoveToServer(isServer, gameObject);
                        DrawGizmos();
#endif
                    }
                    else Logger.PrintError("This object is already registered!");
                }
            }
            else Logger.PrintError("Scene or Static object does not support dynamic registration.");
        }

        internal void AddRpc(byte instanceId, byte rpcId, Action<ByteStream, ushort, ushort, RemoteStats> method)
        {
            if (!remoteMethods.TryAdd((rpcId, instanceId), method))
                Logger.PrintError($"The RPC {instanceId}:{rpcId} is already registered.");
        }

        internal Action<ByteStream, ushort, ushort, RemoteStats> GetRpc(byte instanceId, byte rpcId)
        {
            var key = (rpcId, instanceId);
            if (!remoteMethods.TryGetValue(key, out Action<ByteStream, ushort, ushort, RemoteStats> value))
                Logger.PrintWarning($"RPC does not exist! -> {key} -> [IsServer]={isItFromTheServer}");
            return value;
        }

#if UNITY_EDITOR
        private ObjectType VAL_OBJ_TYPE;
        private void Reset()
        {
            OnValidate();
            VAL_OBJ_TYPE = objectType;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                networks = transform.GetComponentsInChildren<OmniObject>();
                if (!(isInRoot = transform == RootOr()))
                    Logger.PrintWarning($"{gameObject.name} -> Only root objects can have a OmniIdentity component.");

                byte sceneId = (byte)SceneManager.GetActiveScene().buildIndex;
                this.sceneId = objectType == ObjectType.Scene ? sceneId : (byte)255;
                if (objectType == ObjectType.Scene || objectType == ObjectType.Static)
                {
                    if (VAL_OBJ_TYPE != objectType)
                    {
                        id = 0;
                        VAL_OBJ_TYPE = objectType;
                    }

                    if (isInRoot)
                    {
                        OmniIdentity[] identities = FindObjectsOfType<OmniIdentity>(true).Where(x => x.objectType == objectType).ToArray();
                        if (id == 0) id = (ushort)OmniHelper.GetAvailableId(identities, x => x.id, short.MaxValue);
                        else
                        {
                            int count = identities.Count(x => x.id == id);
                            if (count > 1) id = 0;
                        }
                    }
                    else Logger.PrintWarning($"{transform.name} -> [IsRoot]={isInRoot}");
                }
                else id = 0;
            }
        }

        [ContextMenu("Omni/Re-order Identities")]
        [Button("Global: Re-order Identities", EButtonEnableMode.Editor)]
        private void IReorder()
        {
            if (!Application.isPlaying)
            {
                OmniIdentity[] identities = FindObjectsOfType<OmniIdentity>(true).Where(x => x.objectType == objectType).OrderBy(x => x.transform.GetSiblingIndex()).ToArray();
                for (int i = 0; i < identities.Length; i++)
                {
                    var identity = identities[i];
                    identity.id = (ushort)(i + 1);
                    EditorUtility.SetDirty(identity.gameObject);
                }
            }
        }

        [ContextMenu("Omni/Register Omni Objects")]
        [Button("Register Omni Objects", EButtonEnableMode.Editor)]
        private void AddOmniObjects()
        {
            if (!Application.isPlaying)
            {
                networks = transform.GetComponentsInChildren<OmniObject>();
                var omniObjects = RootOr().GetComponentsInChildren<OmniObject>(true);
                for (int i = 0; i < omniObjects.Length; i++)
                {
                    var nObject = omniObjects[i];
                    nObject.id = (byte)(i + 1);
                    nObject.identity = this;
                    EditorUtility.SetDirty(nObject.gameObject);
                }
            }
        }
#endif
    }
}
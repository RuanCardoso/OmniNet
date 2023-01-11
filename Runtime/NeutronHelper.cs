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
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Neutron.Core.Enums;
using MessageType = Neutron.Core.Enums.MessageType;

namespace Neutron.Core
{
    internal static class NeutronHelper
    {
        internal static int GetFreePort()
        {
            System.Net.Sockets.UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint endPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
            int port = endPoint.Port;
            udpClient.Close();
            return port;
        }

        internal static string ToAddress(long address)
        {
            long n1 = address % 256;
            long n2 = address / 256 % 256;
            long n3 = address / 256 / 256 % 256;
            long n4 = address / 256 / 256 / 256;
            return string.Format("{0}.{1}.{2}.{3}", n1, n2, n3, n4);
        }

        internal static ObjectType GetObjectType(MessageType messageType) // bandwidth optimize
        {
            return messageType switch
            {
                MessageType.RemoteStatic => ObjectType.Static,
                MessageType.RemoteScene => ObjectType.Scene,
                MessageType.RemotePlayer => ObjectType.Player,
                MessageType.RemoteDynamic => ObjectType.Dynamic,
                //*************************************************
                MessageType.OnSerializeStatic => ObjectType.Static,
                MessageType.OnSerializeScene => ObjectType.Scene,
                MessageType.OnSerializePlayer => ObjectType.Player,
                MessageType.OnSerializeDynamic => ObjectType.Dynamic,
                //*************************************************
                MessageType.OnSyncBaseStatic => ObjectType.Static,
                MessageType.OnSyncBaseScene => ObjectType.Scene,
                MessageType.OnSyncBasePlayer => ObjectType.Player,
                MessageType.OnSyncBaseDynamic => ObjectType.Dynamic,
                //*************************************************
                MessageType.LocalMessageStatic => ObjectType.Static,
                MessageType.LocalMessageScene => ObjectType.Scene,
                MessageType.LocalMessagePlayer => ObjectType.Player,
                MessageType.LocalMessageDynamic => ObjectType.Dynamic,
                _ => default,
            };
        }

        internal static MessageType GetMessageTypeToRemote(ObjectType messageType) // bandwidth optimize
        {
            return messageType switch
            {
                ObjectType.Static => MessageType.RemoteStatic,
                ObjectType.Scene => MessageType.RemoteScene,
                ObjectType.Player => MessageType.RemotePlayer,
                ObjectType.Dynamic => MessageType.RemoteDynamic,
                _ => default,
            };
        }

        internal static MessageType GetMessageTypeToOnSerialize(ObjectType messageType) // bandwidth optimize
        {
            return messageType switch
            {
                ObjectType.Static => MessageType.OnSerializeStatic,
                ObjectType.Scene => MessageType.OnSerializeScene,
                ObjectType.Player => MessageType.OnSerializePlayer,
                ObjectType.Dynamic => MessageType.OnSerializeDynamic,
                _ => default,
            };
        }

        internal static MessageType GetMessageTypeToOnSyncBase(ObjectType messageType) // bandwidth optimize
        {
            return messageType switch
            {
                ObjectType.Static => MessageType.OnSyncBaseStatic,
                ObjectType.Scene => MessageType.OnSyncBaseScene,
                ObjectType.Player => MessageType.OnSyncBasePlayer,
                ObjectType.Dynamic => MessageType.OnSyncBaseDynamic,
                _ => default,
            };
        }

        internal static MessageType GetMessageTypeToLocalMessage(ObjectType messageType) // bandwidth optimize
        {
            return messageType switch
            {
                ObjectType.Static => MessageType.LocalMessageStatic,
                ObjectType.Scene => MessageType.LocalMessageScene,
                ObjectType.Player => MessageType.LocalMessagePlayer,
                ObjectType.Dynamic => MessageType.LocalMessageDynamic,
                _ => default,
            };
        }

        internal static SubTarget GetSubTarget(bool fromServer, SubTarget subTarget) => fromServer ? subTarget : SubTarget.Server;
        internal static ushort GetPlayerId(bool fromServer) => fromServer ? NeutronNetwork.NetworkId : (ushort)NeutronNetwork.Id;
        internal static int GetAvailableId<T>(T[] array, Func<T, int> predicate, int maxRange, int minRange = 0)
        {
            var ids = array.Select(predicate);
#pragma warning disable IDE0046
            if (maxRange == ids.Count())
                return maxRange;
#pragma warning restore IDE0046
            return Enumerable.Range(minRange, maxRange).Except(ids).ToArray()[0];
        }

#if UNITY_EDITOR
        internal static void MoveToServer(bool isServer, GameObject gameObject)
        {
            if (isServer && NeutronNetwork.IsBind)
                SceneManager.MoveGameObjectToScene(gameObject, NeutronNetwork.Scene);
        }

        internal static List<string> GetDefines(out BuildTargetGroup targetGroup)
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
#if UNITY_SERVER
            var symbols = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Server).Split(';').ToList();
#else
            var symbols = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup)).Split(';').ToList();
#endif
            return symbols;
        }

        internal static void SetDefines(params NeutronDefine[] defines)
        {
            List<string> definedSymbols = GetDefines(out var targetGroup);
            for (int i = 0; i < defines.Length; i++)
            {
                NeutronDefine define = defines[i];
                if (define.enabled)
                {
                    if (!definedSymbols.Contains(define.define)) definedSymbols.Add(define.define);
                    else { /* the symbol has already been defined */ }
                }
                else
                {
                    if (definedSymbols.Contains(define.define)) definedSymbols.Remove(define.define);
                    else { /* the symbol has already been removed */ }
                }
            }

            string symbols = string.Join(';', definedSymbols);
#if UNITY_SERVER
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Server, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup), symbols);
#endif
        }
#endif
    }
}
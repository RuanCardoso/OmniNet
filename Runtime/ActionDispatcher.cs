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
using UnityEngine;

namespace Neutron.Core
{
    [AddComponentMenu("")]
    public class ActionDispatcher : MonoBehaviour
    {
        [Header("Client & Editor")]
        [SerializeField][Range(1, byte.MaxValue)][Label("Actions Per Frame")] protected int CLIENT_APF = 1; // Client
        [Header("Server")]
        [SerializeField][Range(1, byte.MaxValue)][Label("Actions Per Frame")] protected int SERVER_APF = 1; // Server

        private readonly object syncRoot = new();
        private readonly Queue<Action> actions = new();

        protected void ProcessActions()
        {
            lock (syncRoot)
            {
#if UNITY_EDITOR
                int apf = CLIENT_APF; // client
#elif !UNITY_SERVER
                int apf = CLIENT_APF; // client
#else
                int apf = SERVER_APF; // server
#endif
                for (int i = 0; i < apf && actions.Count > 0; i++)
                    actions.Dequeue()();
            }
        }

        protected void Dispatch(Action action)
        {
            lock (syncRoot)
            {
                actions.Enqueue(action);
            }
        }
    }
}
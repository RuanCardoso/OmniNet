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
using System.Threading.Tasks;
using UnityEngine;

namespace Omni.Core
{
    [AddComponentMenu("")]
    public class OmniDispatcher : DBMSBehaviour
    {
        [Header("Client & Editor")]
#if !OMNI_MULTI_THREADED
        [HideInInspector]
#endif
        [SerializeField][Range(1, byte.MaxValue)][Label("Actions Per Frame")] protected int CLIENT_APF = 1; // Client
        [Header("Server")]
#if !OMNI_MULTI_THREADED
        [HideInInspector]
#endif
        [SerializeField][Range(1, byte.MaxValue)][Label("Actions Per Frame")] protected int SERVER_APF = 1; // Server

        private readonly object syncRoot = new();
        private readonly Queue<Action> actions = new();

        /// <summary>
        /// Enable processing of actions on the main thread.<br/>
        /// Call this method in the Update method of a MonoBehaviour.<br/>
        /// </summary>
        protected void Process()
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
                {
                    actions.Dequeue()();
                }
            }
        }

        /// <summary>
        /// Dispatches an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        protected void Dispatch(Action action)
        {
            lock (syncRoot)
            {
                actions.Enqueue(action);
            }
        }

        /// <summary>
        /// Dispatches an action to be executed on the main thread.<br/>
        /// </summary>
        /// <returns></returns>
        protected Task DispatchAsync(Action action)
        {
            TaskCompletionSource<bool> tcs = new();
            lock (syncRoot)
            {
                actions.Enqueue(() =>
                {
                    action?.Invoke();
                    tcs.SetResult(true);
                });
            }
            return tcs.Task;
        }

        /// <summary>
        /// Dispatches an action to be executed on the main thread with a return value.<br/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="value">value when be returned</param>
        /// <returns></returns>
        protected Task<T> DispatchAsync<T>(Action action, T value)
        {
            TaskCompletionSource<T> tcs = new();
            lock (syncRoot)
            {
                actions.Enqueue(() =>
                {
                    action?.Invoke();
                    tcs.SetResult(value);
                });
            }
            return tcs.Task;
        }
    }
}
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
using UnityEngine;

namespace Neutron.Core
{
    [AddComponentMenu("")]
    public class ActionDispatcher : MonoBehaviour
    {
        [SerializeField][Min(1)] internal int actionsPerFrame = 1;
        private readonly object syncRoot = new();
        private readonly Queue<Action> actions = new();

        protected virtual void Update()
        {
            lock (syncRoot)
            {
                for (int i = 0; i < actionsPerFrame && actions.Count > 0; i++)
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
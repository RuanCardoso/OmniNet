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
        private object root = new();
        private Queue<Action> actions = new();
        [Header("[DISPATCHER OPTIONS]")]
        [SerializeField] protected int ACTIONS_PER_FRAME = 1;

        protected virtual void Update()
        {
            lock (root)
            {
                for (int i = 0; i < ACTIONS_PER_FRAME && actions.Count > 0; i++)
                    actions.Dequeue()();
            }
        }

        public void Dispatch(Action action)
        {
            lock (root)
            {
                actions.Enqueue(action);
            }
        }
    }
}
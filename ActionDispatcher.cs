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

        protected virtual void Update()
        {
            lock (root)
            {
                while (actions.Count > 0)
                {
                    actions.Dequeue()();
                }
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
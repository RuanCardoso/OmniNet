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
        private object syncRoot = new();
        private Queue<Action> actions = new();

        #region Serialized
        [Header("[DISPATCHER OPTIONS]")]
        [SerializeField] protected int ACT_PER_FRAME = 1;
        #endregion

        protected virtual void Update()
        {
            lock (syncRoot)
            {
                for (int i = 0; i < ACT_PER_FRAME && actions.Count > 0; i++)
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
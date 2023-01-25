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

using Neutron.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Neutron.Core.Logger;

namespace Neutron.Tests
{
    [AddComponentMenu("")]
    public class SyncBaseTests : NeutronObject
    {
        [SerializeField] private SyncValue<float> health;
        [SerializeField] private SyncValue<int> points;
        [SerializeField] private SyncValue<double> time;
        [SerializeField] private SyncValue trigger;

        [SerializeField] private SyncValueCustom<Player> player;
        [SerializeField] private SyncRefCustom<Child> child;

        private void Awake()
        {
            health = new(this);
            points = new(this);
            time = new(this);
            trigger = new(this, OnJumpTriggered);
            player = new(this, new(10));
            child = new(this, new(10));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                trigger.Get();
            }
        }

        private void OnJumpTriggered()
        {
            Logger.PrintError("Jump Triggered");
        }
    }

    [Serializable]
    public struct Player : ISyncCustom
    {
        public int id;

        public Player(int id)
        {
            this.id = id;
        }

        public void Deserialize(ByteStream parameters)
        {
            id = parameters.ReadInt();
        }

        public void Serialize(ByteStream parameters)
        {
            parameters.Write(id);
        }
    }

    [Serializable]
    public class Child : ISyncCustom
    {
        public int id;

        public Child(int id)
        {
            this.id = id;
        }

        public void Deserialize(ByteStream parameters)
        {
            id = parameters.ReadInt();
        }

        public void Serialize(ByteStream parameters)
        {
            parameters.Write(id);
        }
    }
}
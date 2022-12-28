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
using static Neutron.Core.Enums;

namespace Neutron.Tests
{
    [AddComponentMenu("")]
    public class SyncBaseTests : NeutronObject
    {
        [Serializable]
        public class Person : ISyncCustom
        {
            public int value = 1;

            public void Deserialize(ByteStream parameters)
            {
                value = parameters.ReadInt();
            }

            public void Serialize(ByteStream parameters)
            {
                parameters.Write(value);
            }
        }

        [SerializeField] private SyncValue<byte> health;
        [SerializeField] private SyncValue<int> points;
        [SerializeField] private SyncValue<float> xAxis;

        [SerializeField] private SyncRef<int[]> arrayOfInt;
        [SerializeField] private SyncRef<List<float>> listOfFloat;

        [SerializeField] private SyncCustom<Person> person;

        private void Start()
        {
            health = new SyncValue<byte>(this, authority: AuthorityMode.Mine, target: Target.Me, subTarget: SubTarget.Server);
            points = new SyncValue<int>(this);
            xAxis = new SyncValue<float>(this);
            arrayOfInt = new SyncRef<int[]>(this, new int[] { });
            listOfFloat = new SyncRef<List<float>>(this, new List<float> { });
            person = new SyncCustom<Person>(this, new Person
            {
                value = 1
            });
        }
    }
}
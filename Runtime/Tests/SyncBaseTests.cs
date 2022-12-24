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
using System.Collections.Generic;
using UnityEngine;
using static Neutron.Core.Enums;

namespace Neutron.Tests
{
    [AddComponentMenu("")]
    public class SyncBaseTests : NeutronObject
    {
        [SerializeField] private SyncValue<byte> health;
        [SerializeField] private SyncValue<int> points;
        [SerializeField] private SyncValue<float> xAxis;

        [SerializeField] private SyncRef<int[]> arrayOfInt;
        [SerializeField] private SyncRef<List<float>> listOfFloat;

        private void Start()
        {
            health = new SyncValue<byte>(this, authority: AuthorityMode.Mine, target: Target.Me, subTarget: SubTarget.Server);
            points = new SyncValue<int>(this);
            xAxis = new SyncValue<float>(this);
            //----------------------------------------------------------------
            arrayOfInt = new SyncRef<int[]>(this, new int[] { });
            listOfFloat = new SyncRef<List<float>>(this, new List<float> { });
        }

        protected internal override void OnSerializeView(byte id, ByteStream parameters)
        {
            switch (id)
            {
                case 0:
                    health.Set(parameters.ReadByte());
                    break;
                case 1:
                    points.Set(parameters.ReadInt());
                    break;
                case 2:
                    xAxis.Set(parameters.ReadFloat());
                    break;
                case 3:
                    arrayOfInt.Set(parameters.Deserialize<int[]>());
                    break;
                case 4:
                    listOfFloat.Set(parameters.Deserialize<List<float>>());
                    break;
            }
        }
    }
}
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
using UnityEngine;

namespace Neutron.Core
{
    [Serializable]
    public class SyncValue<T> where T : unmanaged
    {
        [SerializeField] private T value = default;
        public SyncValue(T value = default) => this.value = value;

        public void Set(T value)
        {
            this.value = value;
        }

        public static implicit operator T(SyncValue<T> value) => value.value;

        public override bool Equals(object obj) => ((T)obj).Equals(value);
        public override int GetHashCode() => value.GetHashCode();
        public override string ToString() => value.ToString();
    }
}
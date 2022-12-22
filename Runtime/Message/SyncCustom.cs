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

namespace Neutron.Core
{
    [Serializable]
    public class SyncCustom<T> : SyncBase<T> where T : class, ISerializeValueType
    {
        public SyncCustom(NeutronObject @this, T value = default) : base(@this, value, value)
        {
        }
    }
}
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
    public class SyncRef<T> : SyncBase<T> where T : class
    {
        public SyncRef(NeutronObject @this, T value) : base(@this, value)
        {
        }
    }
}
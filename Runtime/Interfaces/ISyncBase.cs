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

namespace Omni.Core
{
    public interface ISyncBase
    {
        void OnValueChanged();
        bool IsEnum();
        Enum GetEnum();
        void SetEnum(Enum enumValue);
    }
}
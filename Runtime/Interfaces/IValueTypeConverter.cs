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

namespace Neutron.Core
{
    internal interface IValueTypeConverter<T>
    {
        int GetInt(T value);
        T GetInt(int value);
        bool GetBool(T value);
        T GetBool(bool value);
        float GetFloat(T value);
        T GetFloat(float value);
        byte GetByte(T value);
        T GetByte(byte value);
    }
}
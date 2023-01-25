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

namespace Neutron.Core
{
    internal class ValueTypeConverter : IValueTypeConverter<int>, IValueTypeConverter<bool>, IValueTypeConverter<float>, IValueTypeConverter<byte>
    {
        internal static readonly ValueTypeConverter _ = new();
        internal static readonly HashSet<Type> Types = new()
        {
            typeof(int),
            typeof(bool),
            typeof(float),
            typeof(byte),
            typeof(Trigger),
        };

        #region _
        public bool GetBool(int value) => throw new NotImplementedException();
        public int GetInt(bool value) => throw new NotImplementedException();
        public float GetFloat(int value) => throw new NotImplementedException();
        public float GetFloat(bool value) => throw new NotImplementedException();
        public int GetInt(float value) => throw new NotImplementedException();
        public bool GetBool(float value) => throw new NotImplementedException();
        public byte GetByte(int value) => throw new NotImplementedException();
        public byte GetByte(bool value) => throw new NotImplementedException();
        public byte GetByte(float value) => throw new NotImplementedException();
        public int GetInt(byte value) => throw new NotImplementedException();
        public bool GetBool(byte value) => throw new NotImplementedException();
        public float GetFloat(byte value) => throw new NotImplementedException();
        #endregion

        public int GetInt(int value) => value;
        public bool GetBool(bool value) => value;
        public float GetFloat(float value) => value;
        public byte GetByte(byte value) => value;

        #region _
        bool IValueTypeConverter<bool>.GetInt(int value) => throw new NotImplementedException();
        float IValueTypeConverter<float>.GetInt(int value) => throw new NotImplementedException();
        byte IValueTypeConverter<byte>.GetInt(int value) => throw new NotImplementedException();
        int IValueTypeConverter<int>.GetBool(bool value) => throw new NotImplementedException();
        int IValueTypeConverter<int>.GetFloat(float value) => throw new NotImplementedException();
        int IValueTypeConverter<int>.GetByte(byte value) => throw new NotImplementedException();
        bool IValueTypeConverter<bool>.GetFloat(float value) => throw new NotImplementedException();
        bool IValueTypeConverter<bool>.GetByte(byte value) => throw new NotImplementedException();
        float IValueTypeConverter<float>.GetBool(bool value) => throw new NotImplementedException();
        float IValueTypeConverter<float>.GetByte(byte value) => throw new NotImplementedException();
        byte IValueTypeConverter<byte>.GetBool(bool value) => throw new NotImplementedException();
        byte IValueTypeConverter<byte>.GetFloat(float value) => throw new NotImplementedException();
        #endregion
    }
}
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
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    internal static class NeutronExtensions
    {
        internal static string ToSizeUnit(this long value, SizeUnits unit) => (value / (double)Math.Pow(1024, (long)unit)).ToString("0.00");
        internal static bool IsInBounds<T>(this T[] array, int index) => (index >= 0) && (index < array.Length);
        public static void Read<T>(this SyncRef<T> value, ByteStream message) where T : class => value.Intern_Set(message.Deserialize<T>());
        public static void Read<T>(this SyncValue<T> value, ByteStream message) where T : unmanaged
        {
            var converter = SyncValue<T>.Converter;
            switch (value.typeCode)
            {
                case TypeCode.Int32:
                    value.Intern_Set(converter.GetInt(message.ReadInt()));
                    break;
                case TypeCode.Boolean:
                    value.Intern_Set(converter.GetBool(message.ReadBool()));
                    break;
                case TypeCode.Single:
                    value.Intern_Set(converter.GetFloat(message.ReadFloat()));
                    break;
                case TypeCode.Byte:
                    value.Intern_Set(converter.GetByte(message.ReadByte()));
                    break;
            }
        }

        public static void Read<T>(this SyncCustom<T> value, ByteStream message) where T : class, ISyncCustom
        {
            ISyncCustom ISerialize = value.Get() as ISyncCustom;
            if (ISerialize != null) ISerialize.Deserialize(message);
            else Logger.PrintError("SyncCustom -> Deserialize fail!");
        }
    }
}
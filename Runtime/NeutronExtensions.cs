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
        internal static bool InBounds<T>(this T[] array, int index) => (index >= 0) && (index < array.Length);
    }
}
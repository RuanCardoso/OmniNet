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

using UnityEngine;

namespace Neutron.Core.Tests
{
    public class StaticFuncsTests : MonoBehaviour
    {
        byte[] data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        private void Start()
        {
            Logger.PrintError(data.Length);

            var dd = ByteStream.Get();
            dd.Write(data);
        }
    }
}

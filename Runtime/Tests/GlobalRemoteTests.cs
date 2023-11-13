using Omni.Core;
using UnityEngine;

namespace Omni.Tests
{
    public class GlobalRemoteTests : OmniBehaviour
    {
        protected override byte Id => 1;

        private unsafe void Start()
        {
            // bool v1 = false;
            // bool v2 = true;
            // bool v3 = false;
            // bool v4 = true;
            // bool v5 = false;

            // byte bit = (byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4);

            // // print in binary,eg: 00000001
            // OmniLogger.Print(Convert.ToString(bit, 2).PadLeft(8, '0'));
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.G))
            {
                var IOHandler = Get;
                //IOHandler.Write(true, false, true, false);
                // com 5 booleanos para testar
                IOHandler.Write(true, true, true, false, true);

                Remote(1, IOHandler, false, cachingOption: Enums.DataCachingOption.Overwrite);
            }
        }

        [Remote(1)]
        public void RemoteEg(DataIOHandler IOHandler, ushort fromId, ushort toId, bool isServer, RemoteStats stats)
        {
            //IOHandler.ReadBool(out bool v1, out bool v2, out bool v3, out bool v4);
            //OmniLogger.Print($"v1: {v1}, v2: {v2}, v3: {v3}, v4: {v4}");
            // ler os 5 boleanos e printa
            IOHandler.ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5);
            OmniLogger.Print($"v1: {v1}, v2: {v2}, v3: {v3}, v4: {v4}, v5: {v5}");
        }
    }
}

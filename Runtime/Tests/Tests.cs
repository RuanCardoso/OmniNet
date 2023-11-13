using UnityEngine;

namespace Omni.Tests
{
    public class Tests : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            // DataDeliveryMode mode = DataDeliveryMode.Secured;
            // DataTarget dataTarget = DataTarget.Self;
            // DataProcessingOption dataProcessingOption = DataProcessingOption.ProcessOnServer;
            // DataCachingOption dataCachingOption = DataCachingOption.Overwrite;

            // // eg: 0010 1111
            // //             | 1: Secured
            // //           || 3:  Self;
            // //          | 1: ProcessOnServer
            // //       ||| 5: Overwrite

            // byte bit = (byte)((byte)mode | (byte)dataTarget << 1 | (byte)dataProcessingOption << 3 | (byte)dataCachingOption << 4);

            // // print in binary,eg: 00000001
            // Debug.Log(Convert.ToString(bit, 2).PadLeft(8, '0'));

            // byte byteToUnpack = bit;

            // DataDeliveryMode mode1 = (DataDeliveryMode)(byteToUnpack & 0b1);
            // DataTarget dataTarget1 = (DataTarget)((byteToUnpack >> 1) & 0b11);
            // DataProcessingOption dataProcessingOption1 = (DataProcessingOption)((byteToUnpack >> 3) & 0b1);
            // DataCachingOption dataCachingOption1 = (DataCachingOption)((byteToUnpack >> 4) & 0b111);

            // Debug.Log($"mode1: {mode1}, dataTarget1: {dataTarget1}, dataProcessingOption1: {dataProcessingOption1}, dataCachingOption1: {dataCachingOption1}");
        }
    }
}
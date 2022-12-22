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
    public interface ISerializeValueType
    {
        void Serialize(ByteStream parameters);
        void Deserialize(ByteStream parameters);
    }
}
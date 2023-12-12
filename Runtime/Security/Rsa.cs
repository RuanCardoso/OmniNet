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

using System.Security.Cryptography;

#pragma warning disable IDE0063
namespace Omni.Core.Cryptography
{
	internal class RSACryptography
	{
		internal const int IOHandlerSize = 500;
		internal const int keySize = 1024;
		internal static void GetRSAKeys(out string privateKey, out string publicKey)
		{
			using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(keySize))
			{
				privateKey = RSA.ToXmlString(true);
				publicKey = RSA.ToXmlString(false);
			}
		}

		internal static byte[] Encrypt(byte[] data, string publicKey)
		{
			using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(keySize))
			{
				RSA.FromXmlString(publicKey);
				return RSA.Encrypt(data, false);
			}
		}

		internal static byte[] Decrypt(byte[] data, string privateKey)
		{
			using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(keySize))
			{
				RSA.FromXmlString(privateKey);
				return RSA.Decrypt(data, false);
			}
		}
	}
}
#pragma warning restore IDE0063
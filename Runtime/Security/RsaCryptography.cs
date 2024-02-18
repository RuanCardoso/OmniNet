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
	public class RsaCryptography
	{
		// The code implements RSA cryptography in C#. It differentiates key sizes (1024 bits for UNITY_EDITOR and 2048 bits for other environments) likely due to a consideration of security and performance.
		// Larger keys offer more security but require more computational resources. Thus, a smaller key may be preferred during development (UNITY_EDITOR) to improve performance, while a larger key is used in production environments to ensure adequate security.
		// This differentiation enables a balance between security and performance, adapting to the execution environment.
#if UNITY_EDITOR
		private const int keySize = 1024;
#else
        private const int keySize = 2048;
#endif
		public static void GetRsaKeys(out string privateKey, out string publicKey)
		{
			using (RSA Rsa = RSA.Create(keySize))
			{
				privateKey = Rsa.ToXmlString(true);
				publicKey = Rsa.ToXmlString(false);
			}
		}

		public static byte[] Encrypt(byte[] data, string publicKey)
		{
			using (RSA Rsa = RSA.Create(keySize))
			{
				Rsa.FromXmlString(publicKey);
				return Rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
			}
		}

		public static byte[] Decrypt(byte[] data, string privateKey)
		{
			using (RSA Rsa = RSA.Create(keySize))
			{
				Rsa.FromXmlString(privateKey);
				return Rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
			}
		}
	}
}
#pragma warning restore IDE0063
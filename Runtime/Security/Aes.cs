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
	/// <summary>
	/// Provides AES encryption and decryption services.<br/>
	/// Supports 128, 192 and 256 bit keys.
	/// </summary>
	internal class Aes
	{
		internal static byte[] Encrypt(byte[] data, int offset, int length, byte[] key, out byte[] IV)
		{
			using (System.Security.Cryptography.Aes AES = System.Security.Cryptography.Aes.Create())
			{
				AES.KeySize = 128; // 128 bit key
				AES.BlockSize = 128; // 128 bit block size
				AES.Mode = CipherMode.CBC; // Cipher Block Chaining
				AES.Padding = PaddingMode.PKCS7; // PKCS7 padding

				AES.Key = key;
				AES.GenerateIV();
				IV = AES.IV;

                ICryptoTransform encryptor = AES.CreateEncryptor();
				return encryptor.TransformFinalBlock(data, offset, length);
			}
		}

		internal static byte[] Decrypt(byte[] data, int offset, int length, byte[] key, byte[] IV)
		{
			using (System.Security.Cryptography.Aes AES = System.Security.Cryptography.Aes.Create())
			{
				AES.KeySize = 128; // 128 bit key
				AES.BlockSize = 128; // 128 bit block size
				AES.Mode = CipherMode.CBC; // Cipher Block Chaining
				AES.Padding = PaddingMode.PKCS7; // PKCS7 padding

				AES.IV = IV;
				AES.Key = key;

                ICryptoTransform decryptor = AES.CreateDecryptor();
				return decryptor.TransformFinalBlock(data, offset, length);
			}
		}

		internal static byte[] GenerateKey()
		{
			using (System.Security.Cryptography.Aes AES = System.Security.Cryptography.Aes.Create())
			{
				AES.KeySize = 128; // 128 bit key
				AES.BlockSize = 128; // 128 bit block size
				AES.Mode = CipherMode.CBC; // Cipher Block Chaining
				AES.Padding = PaddingMode.PKCS7; // PKCS7 padding
				AES.GenerateKey();
				return AES.Key;
			}
		}
	}
}
#pragma warning restore IDE0063
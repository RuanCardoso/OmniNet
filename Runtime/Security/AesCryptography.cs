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
	public class AesCryptography
	{
		public static byte[] Encrypt(byte[] data, int offset, int length, byte[] key, out byte[] Iv)
		{
			using (Aes Aes = Aes.Create())
			{
				Aes.KeySize = 128; // 128 bit key
				Aes.BlockSize = 128; // 128 bit block size
				Aes.Mode = CipherMode.CBC; // Cipher Block Chaining
				Aes.Padding = PaddingMode.PKCS7;

				Aes.Key = key;
				Aes.GenerateIV();
				Iv = Aes.IV;

				ICryptoTransform encryptor = Aes.CreateEncryptor();
				return encryptor.TransformFinalBlock(data, offset, length);
			}
		}

		public static byte[] Decrypt(byte[] data, int offset, int length, byte[] key, byte[] Iv)
		{
			using (Aes Aes = Aes.Create())
			{
				Aes.KeySize = 128;
				Aes.BlockSize = 128;
				Aes.Mode = CipherMode.CBC;
				Aes.Padding = PaddingMode.PKCS7;

				Aes.IV = Iv;
				Aes.Key = key;

				ICryptoTransform decryptor = Aes.CreateDecryptor();
				return decryptor.TransformFinalBlock(data, offset, length);
			}
		}

		public static byte[] GenerateKey()
		{
			using (Aes Aes = Aes.Create())
			{
				Aes.KeySize = 128;
				Aes.BlockSize = 128;
				Aes.Mode = CipherMode.CBC;
				Aes.Padding = PaddingMode.PKCS7;
				Aes.GenerateKey();
				return Aes.Key;
			}
		}
	}
}
#pragma warning restore IDE0063
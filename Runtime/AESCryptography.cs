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
    internal class AESEncryption
    {
        internal static byte[] Encrypt(byte[] data, int offset, int length, byte[] key, out byte[] IV)
        {
            using (Aes AES = Aes.Create())
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
            using (Aes AES = Aes.Create())
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
    }

    internal class RSACryptography
    {
        private static readonly int keySize = 2048;
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
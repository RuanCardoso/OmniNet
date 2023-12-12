using System;
using System.Security.Cryptography;
using System.Text;

namespace Omni.Core.Hashing
{
  public enum SecurityAlgorithm
  {
    MD5,
    [Obsolete("SHA1 is not recommended for security purposes, use SHA256/SHA384/SHA512 or BCrypt instead.")]
    SHA1,
    SHA256,
    SHA384,
    SHA512,
    BCrypt
  }

  public class HashingUtility
  {
    public static bool Verify(string input, string hash, SecurityAlgorithm securityAlgorithm)
    {
      if (securityAlgorithm == SecurityAlgorithm.BCrypt)
      {
        return BCrypt.Net.BCrypt.Verify(input, hash);
      }
      else
      {
        string hashOfInput = Hash(input, securityAlgorithm);
        return hashOfInput == hash;
      }
    }

    public static string Hash(string input, SecurityAlgorithm securityAlgorithm)
    {
      switch (securityAlgorithm)
      {
        case SecurityAlgorithm.MD5:
          {
            using MD5 md5 = MD5.Create();
            return Hash(md5, input);
          }
#pragma warning disable 618
        case SecurityAlgorithm.SHA1:
#pragma warning restore 618
          {
            using SHA1 sha1 = SHA1.Create();
            return Hash(sha1, input);
          }
        case SecurityAlgorithm.SHA256:
          {
            using SHA256 sha256 = SHA256.Create();
            return Hash(sha256, input);
          }
        case SecurityAlgorithm.SHA384:
          {
            using SHA384 sha384 = SHA384.Create();
            return Hash(sha384, input);
          }
        case SecurityAlgorithm.SHA512:
          {
            using SHA512 sha512 = SHA512.Create();
            return Hash(sha512, input);
          }
        case SecurityAlgorithm.BCrypt:
          {
            return BCrypt.Net.BCrypt.HashPassword(input);
          }
        default:
          {
            throw new NotImplementedException();
          }
      }
    }

    private static string Hash(HashAlgorithm hashAlgorithm, string input, Encoding encoding = null)
    {
      encoding ??= Encoding.UTF8;
      byte[] bytes = encoding.GetBytes(input);
      byte[] hash = hashAlgorithm.ComputeHash(bytes);
      return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
  }
}
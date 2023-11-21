internal class AuthStorage
{
    internal static byte[] AesKey { get; set; }
    internal class RSA
    {
        internal class Client
        {
            internal static string PublicKey { get; set; }
            internal static string PrivateKey { get; set; }
        }

        internal class Server
        {
            internal static string PublicKey { get; set; }
            internal static string PrivateKey { get; set; }
        }
    }
}
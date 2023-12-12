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

namespace Omni.Core
{
	public static class Enums
	{
		internal enum MessageType : byte
		{
			None = 0,
			StressTest = 1,
			Acknowledgement = 2,
			Zone = 3,
			GlobalMessage = 4,
			RemotePlayer = 5,
			RemoteDynamic = 6,
			RemoteScene = 7,
			RemoteStatic = 8,
			Ping = 9,
			OnSerializeStatic = 10,
			OnSerializePlayer = 11,
			OnSerializeDynamic = 12,
			OnSerializeScene = 13,
			OnSyncBaseStatic = 14,
			OnSyncBasePlayer = 15,
			OnSyncBaseDynamic = 16,
			OnSyncBaseScene = 17,
			GetCache = 18,
			LocalMessageStatic = 19,
			LocalMessagePlayer = 20,
			LocalMessageDynamic = 21,
			LocalMessageScene = 22,
			Remote = 23,
			SendMessage = 24,
			PacketLoss = 25,
			RSAExchange = 26,
			AESExchange = 254,
			Disconnect = 255,
		}

		/// <summary>
		/// Manages the types of data cached on the server, providing a structured approach to categorizing and retrieving information.
		/// </summary>
		public enum DataStorageType : byte
		{
			/// <summary>
			/// Retrieves all data marked as "Remote" from the server based on its unique identifier.
			/// </summary>
			Remote = 0,

			/// <summary>
			/// Retrieves all data marked as "OnSerializeView" from the server based on its unique identifier.
			/// </summary>
			OnSerializeView = 1,

			/// <summary>
			/// Retrieves all data marked as "NetworkVars" from the server based on its unique identifier.
			/// </summary>
			NetworkVars = 2,

			/// <summary>
			/// Retrieves all data marked as "GlobalMessage" from the server based on its unique identifier.
			/// </summary>
			GlobalMessage = 3,

			/// <summary>
			/// Retrieves all data marked as "LocalMessage" from the server based on its unique identifier.
			/// </summary>
			LocalMessage = 4,

			/// <summary>
			/// Retrieves all data marked as "GlobalRemote" from the server based on its unique identifier.
			/// </summary>
			GlobalRemote = 5,
		}


		/// <summary>
		/// Enumerates the various modes for data delivery, defining how data is transmitted and processed.
		/// </summary>
		public enum DataDeliveryMode : byte
		{
			/// <summary>
			/// Specifies the unreliable data delivery mode, where data may be susceptible to loss, duplication, or non-sequential arrival.
			/// This mode is suitable for frequently updated data, such as player position and rotation in a real-time application.
			/// </summary>
			/// <remarks>
			/// In Unsecured mode (0), the system provides no guarantees regarding the delivery of data integrity, making it suitable for scenarios
			/// where occasional data discrepancies are acceptable and can be compensated for by subsequent updates.
			/// </remarks>
			/// <example>
			/// Example of usage:
			/// <code>
			///   DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured;
			///   // Further implementation details...
			/// </code>
			/// </example>
			/// <seealso cref="DataDeliveryMode"/>
			/// <seealso cref="DataDeliveryOptions"/>
			Unsecured = 0,
			/// <summary>
			/// Specifies the reliable data delivery mode, ensuring that data is not lost, duplicated, or received out of order.
			/// This mode is ideal for infrequently updated data, such as player name, health, damage, and database operations.
			/// It's important to note that data transmitted in Secured mode (1) is not encrypted; therefore, this mode is suitable only for non-sensitive data.
			/// </summary>
			/// <remarks>
			/// In Secured mode, the system guarantees the integrity and orderliness of the delivered data, making it well-suited for critical information
			/// that requires accurate and reliable processing. However, it should be avoided for sensitive data due to the absence of encryption.
			/// For sensitive data, the SecuredWithAes (2) mode should be used instead.
			/// Replay attacks aren't possible in Secured mode (1), as the system ensures that data is received in the correct order and without duplication.
			/// For sensitive data and database operations, the SecuredWithAes (2) mode is recommended, as it provides enhanced security through encryption.
			/// </remarks>
			/// <example>
			/// Example of usage:
			/// <code>
			///   DataDeliveryMode deliveryMode = DataDeliveryMode.Secured;
			///   // Further implementation details...
			/// </code>
			/// </example>
			/// <seealso cref="DataDeliveryMode"/>
			/// <seealso cref="DataDeliveryOptions"/>
			Secured = 1,
			/// <summary>
			/// Specifies the reliable data delivery mode with enhanced security through encryption, ensuring data is immune to loss, duplication, or out-of-order reception.
			/// This mode is specifically designed for handling sensitive data, including passwords, emails, identification, card numbers, phone numbers, and transactions.
			/// Utilizing 128-bit AES encryption, the SecuredWithAes mode offers robust protection, although it may incur a performance cost and increased bandwidth usage.
			/// The encryption is bidirectional, implemented both on the client and server sides, and supports decryption at both ends by default (refer to the documentation for detailed information).
			/// Additionally, the RSA algorithm is employed to further strengthen the overall security of the transmitted data.
			/// </summary>
			/// <remarks>
			/// It is crucial to choose SecuredWithAes (2) when dealing with sensitive information, recognizing that the additional security measures contribute to enhanced data protection.
			/// Users are advised to review the documentation for comprehensive details on the bidirectional encryption process and the integration of the RSA algorithm.
			/// For optimal security in database operations, it is highly recommended to use the SecuredWithAes (2) mode, ensuring the confidentiality and integrity of critical data stored and retrieved from databases. 
			/// This mode helps mitigate security risks, including potential replay attacks, and ensures that data remains secure even if intercepted.
			/// </remarks>
			/// <example>
			/// Example of usage:
			/// <code>
			///   DataDeliveryMode deliveryMode = DataDeliveryMode.SecuredWithAes;
			///   // Further implementation details...
			/// </code>
			/// </example>
			/// <seealso cref="DataDeliveryMode"/>
			/// <seealso cref="DataDeliveryOptions"/>
			SecuredWithAes = 2
		}

		/// <summary>
		/// Enum that represents the data target, used to send data to a specific target.
		/// </summary>
		public enum DataTarget : byte
		{
			/// <summary>
			/// Only the server will receive the data.
			/// </summary>
			Server = 0,
			/// <summary>
			/// All clients will receive the data.
			/// </summary>
			Broadcast = 1,
			/// <summary>
			/// All clients will receive the data, except the sender.
			/// </summary>
			BroadcastExcludingSelf = 2,
			/// <summary>
			/// Only the sender will receive the data.
			/// </summary>
			Self = 3,
		}

		/// <summary>
		/// Determines whether the data will be processed on the server or not.
		/// </summary>
		public enum DataProcessingOption : byte
		{
			/// <summary>
			/// The data will not be processed on the server.
			/// </summary>
			DoNotProcessOnServer = 0,
			/// <summary>
			/// The data will be processed on the server.
			/// </summary>
			ProcessOnServer = 1,
		}

		/// <summary>
		/// Determines whether the data will be cached on the server or not.
		/// </summary>
		public enum DataCachingOption : byte
		{
			/// <summary>
			/// The data will not be cached on the server.
			/// </summary>
			None = 0,
			/// <summary>
			/// The data will be cached on the server with append mode, the data will be added to the cache.<br/>
			/// Warning: This mode can cause memory overflow if the data is not removed from the cache.<br/>
			/// Warning: This mode has a high performance cost, use it only if necessary and if the high memory usage is acceptable.<br/>
			/// </summary>
			Append = 1,
			/// <summary>
			/// The data will be cached on the server with overwrite mode, the data will be overwritten in the cache if it already exists.<br/>
			/// High performance, recommended for most cases.<br/>
			/// </summary>
			Overwrite = 2,
		}

		public enum ObjectType : byte
		{
			Player = 0,
			Dynamic = 1,
			Scene = 2,
			Static = 3,
		}

		internal enum SizeUnits
		{
			Byte, KB, MB, GB, TB, PB, EB, ZB, YB
		}

		internal enum LocalPhysicsMode
		{
			Physics2D = 0x1,
			Physics3D = 0x2
		}

		/// <summary>
		/// Represents the authority mode for network objects.
		/// </summary>
		public enum AuthorityMode
		{
			/// <summary>
			/// When is mine, the object will be controlled by the local player.
			/// </summary>
			Mine,
			/// <summary>
			/// When is server, the object will be controlled by the server.
			/// </summary>
			Server,
			/// <summary>
			/// When is client, the object will be controlled by the remote player.<br/>
			/// All players has authority over the object.
			/// </summary>
			Client,
			/// <summary>
			/// When is custom, the object will be controlled by the your authority implementation.
			/// </summary>
			Custom
		}

		public enum PlatformOp
		{
			Editor,
			Client,
			Server
		}
	}
}
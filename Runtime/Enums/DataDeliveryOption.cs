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
		/// In Unreliable mode (0), the system provides no guarantees regarding the delivery of data integrity, making it suitable for scenarios
		/// where occasional data discrepancies are acceptable and can be compensated for by subsequent updates.
		/// </remarks>
		/// <example>
		/// Example of usage:
		/// <code>
		///   DataDeliveryMode deliveryMode = DataDeliveryMode.Unreliable;
		///   // Further implementation details...
		/// </code>
		/// </example>
		/// <seealso cref="DataDeliveryMode"/>
		Unreliable = 0,
		/// <summary>
		/// Specifies the reliable data delivery mode, ensuring that data is not lost, duplicated, or received out of order.
		/// This mode is ideal for infrequently updated data, such as player name, health, damage, and database operations(Insecure).
		/// It's important to note that data transmitted in Reliable mode (1) is not encrypted; therefore, this mode is suitable only for non-sensitive data.
		/// </summary>
		/// <remarks>
		/// In Reliable mode, the system guarantees the integrity and orderliness of the delivered data, making it well-suited for critical information
		/// that requires accurate and reliable processing. However, it should be avoided for sensitive data due to the absence of encryption.
		/// For sensitive data, the RealibleEncrypted (2) mode should be used instead.
		/// Replay attacks aren't possible in Reliable mode (1), as the system ensures that data is received in the correct order and without duplication.
		/// For sensitive data and database operations, the RealibleEncrypted (2) mode is recommended, as it provides enhanced security through encryption.
		/// </remarks>
		/// <example>
		/// Example of usage:
		/// <code>
		///   DataDeliveryMode deliveryMode = DataDeliveryMode.Reliable;
		///   // Further implementation details...
		/// </code>
		/// </example>
		/// <seealso cref="DataDeliveryMode"/>
		ReliableOrdered = 1,
		/// <summary>
		/// Specifies the reliable data delivery mode with enhanced security through encryption, ensuring data is immune to loss, duplication, or out-of-order reception.
		/// This mode is specifically designed for handling sensitive data, including passwords, emails, identification, card numbers, phone numbers, and transactions.
		/// Utilizing 128-bit AES encryption, the RealibleEncrypted mode offers robust protection, although it may incur a performance cost and increased bandwidth usage.
		/// The encryption is bidirectional, implemented both on the client and server sides, and supports decryption at both ends by default (refer to the documentation for detailed information).
		/// Additionally, the RSA algorithm is employed to further strengthen the overall security of the transmitted data.
		/// <br></br>
		/// <br></br>
		/// This reliable data delivery mode employs 128-bit AES encryption and the robust RSA algorithm(2048 Bits), both widely recognized and utilized by governments, financial institutions, and organizations worldwide to safeguard sensitive information.
		/// AES and RSA are considered high-level security standards and are widely adopted across various applications, including banking systems, online financial transactions, and government communications.
		/// Moreover, AES is approved by the United States National Institute of Standards and Technology (NIST), and RSA is extensively used to ensure the authenticity, integrity, and confidentiality of data in secure networks.
		/// Therefore, by choosing the RealibleEncrypted (2) mode, you are adopting a reliable and internationally recognized approach to protect your critical data against security threats.
		/// </summary>
		/// <remarks>
		/// It is crucial to choose RealibleEncrypted (2) when dealing with sensitive information, recognizing that the additional security measures contribute to enhanced data protection.
		/// Users are advised to review the documentation for comprehensive details on the bidirectional encryption process and the integration of the RSA algorithm.
		/// For optimal security in database operations, it is highly recommended to use the RealibleEncrypted (2) mode, ensuring the confidentiality and integrity of critical data stored and retrieved from databases. 
		/// This mode helps mitigate security risks, including potential replay attacks, and ensures that data remains secure even if intercepted.
		/// </remarks>
		/// <remarks>
		/// </remarks>
		/// <example>
		/// Example of usage:
		/// <code>
		///   DataDeliveryMode deliveryMode = DataDeliveryMode.ReliableEncrypted;
		///   // Further implementation details...
		/// </code>
		/// </example>
		/// <seealso cref="DataDeliveryMode"/>
		ReliableEncryptedOrdered = 2,
		/// <summary>
		/// Specifies the reliable data delivery mode, ensuring that data is not lost, duplicated, but they can arrive out of order.
		/// This mode is ideal for infrequently updated data, such as player name, health, damage, and database operations(Insecure).
		/// It's important to note that data transmitted in ReliableUnordered mode (3) is not encrypted; therefore, this mode is suitable only for non-sensitive data.
		/// </summary>
		/// <remarks>
		/// In ReliableUnordered mode, the system guarantees the integrity of the delivered data, making it well-suited for critical information
		/// that requires accurate and reliable processing. However, it should be avoided for sensitive data due to the absence of encryption.
		/// For sensitive data, the RealibleEncrypted (2) mode should be used instead.
		/// Replay attacks aren't possible in ReliableUnordered mode (1), as the system ensures that data is received in the correct state and without duplication.
		/// For sensitive data and database operations, the RealibleEncrypted (2) mode is recommended, as it provides enhanced security through encryption.
		/// </remarks>
		/// <example>
		/// Example of usage:
		/// <code>
		///   DataDeliveryMode deliveryMode = DataDeliveryMode.ReliableUnordered;
		///   // Further implementation details...
		/// </code>
		/// </example>
		/// <seealso cref="DataDeliveryMode"/>
		ReliableUnordered = 3,
		/// <summary>
		/// Similar to 'ReliableOrdered', except only the last message is considered reliable; previous messages are discarded. 
		/// For example, if you send messages 1, 2, 3, 4, 5, but only message 5 is subsequently received in the order 5, 4, 3, 2, 1, only message 5 will be delivered, while the others will be discarded for being behind the last received sequence.
		/// Use channels to separate messages by type; one type may discard messages from other types if they are in the same channel.
		/// <br></br>
		/// <br></br>
		/// Specifies the reliable data delivery mode, ensuring that data is not lost, duplicated, or received out of order.
		/// This mode is ideal for infrequently updated data, such as gun fire(shots), player name, health, damage.
		/// It's important to note that data transmitted in ReliableSequenced mode (1) is not encrypted; therefore, this mode is suitable only for non-sensitive data.
		/// </summary>
		/// <remarks>
		/// In ReliableSequenced mode, the system guarantees the integrity and orderliness of the delivered data, making it well-suited for critical information
		/// that requires accurate and reliable processing. However, it should be avoided for sensitive data due to the absence of encryption.
		/// For sensitive data, the RealibleEncrypted (2) mode should be used instead.
		/// Replay attacks aren't possible in ReliableSequenced mode (1), as the system ensures that data is received in the correct order and without duplication.
		/// </remarks>
		/// <example>
		/// Example of usage:
		/// <code>
		///   DataDeliveryMode deliveryMode = DataDeliveryMode.ReliableSequenced;
		///   // Further implementation details...
		/// </code>
		/// </example>
		/// <seealso cref="DataDeliveryMode"/>
		ReliableSequenced = 4,
		/// <summary>
		/// Specifies the reliable data delivery mode with enhanced security through encryption, ensuring data is immune to loss, duplication.
		/// This mode is specifically designed for handling sensitive data, including passwords, emails, identification, card numbers, phone numbers, and transactions.
		/// Utilizing 128-bit AES encryption, the RealibleEncrypted mode offers robust protection, although it may incur a performance cost and increased bandwidth usage.
		/// The encryption is bidirectional, implemented both on the client and server sides, and supports decryption at both ends by default (refer to the documentation for detailed information).
		/// Additionally, the RSA algorithm is employed to further strengthen the overall security of the transmitted data.
		/// <br></br>
		/// <br></br>
		/// This reliable data delivery mode employs 128-bit AES encryption and the robust RSA algorithm(2048 Bits), both widely recognized and utilized by governments, financial institutions, and organizations worldwide to safeguard sensitive information.
		/// AES and RSA are considered high-level security standards and are widely adopted across various applications, including banking systems, online financial transactions, and government communications.
		/// Moreover, AES is approved by the United States National Institute of Standards and Technology (NIST), and RSA is extensively used to ensure the authenticity, integrity, and confidentiality of data in secure networks.
		/// Therefore, by choosing the RealibleEncrypted (2) mode, you are adopting a reliable and internationally recognized approach to protect your critical data against security threats.
		/// </summary>
		/// <remarks>
		/// It is crucial to choose RealibleEncrypted (2) when dealing with sensitive information, recognizing that the additional security measures contribute to enhanced data protection.
		/// Users are advised to review the documentation for comprehensive details on the bidirectional encryption process and the integration of the RSA algorithm.
		/// For optimal security in database operations, it is highly recommended to use the RealibleEncrypted (2) mode, ensuring the confidentiality and integrity of critical data stored and retrieved from databases. 
		/// This mode helps mitigate security risks, including potential replay attacks, and ensures that data remains secure even if intercepted.
		/// </remarks>
		/// <remarks>
		/// </remarks>
		/// <example>
		/// Example of usage:
		/// <code>
		///   DataDeliveryMode deliveryMode = DataDeliveryMode.ReliableEncrypted;
		///   // Further implementation details...
		/// </code>
		/// </example>
		/// <seealso cref="DataDeliveryMode"/>
		ReliableEncryptedUnordered = 5,
		/// <summary>
		/// Similar to 'ReliablencryptedOrdered', except only the last message is considered reliable; previous messages are discarded. 
		/// For example, if you send messages 1, 2, 3, 4, 5, but only message 5 is subsequently received in the order 5, 4, 3, 2, 1, only message 5 will be delivered, while the others will be discarded for being behind the last received sequence.
		/// Use channels to separate messages by type; one type may discard messages from other types if they are in the same channel.
		/// <br></br>
		/// <br></br>
		/// Specifies the reliable data delivery mode with enhanced security through encryption, ensuring data is immune to loss, duplication, or out-of-order reception.
		/// This mode is specifically designed for handling sensitive data, including passwords, emails, identification, card numbers, phone numbers, and transactions.
		/// Utilizing 128-bit AES encryption, the RealibleEncrypted mode offers robust protection, although it may incur a performance cost and increased bandwidth usage.
		/// The encryption is bidirectional, implemented both on the client and server sides, and supports decryption at both ends by default (refer to the documentation for detailed information).
		/// Additionally, the RSA algorithm is employed to further strengthen the overall security of the transmitted data.
		/// <br></br>
		/// <br></br>
		/// This reliable data delivery mode employs 128-bit AES encryption and the robust RSA algorithm(2048 Bits), both widely recognized and utilized by governments, financial institutions, and organizations worldwide to safeguard sensitive information.
		/// AES and RSA are considered high-level security standards and are widely adopted across various applications, including banking systems, online financial transactions, and government communications.
		/// Moreover, AES is approved by the United States National Institute of Standards and Technology (NIST), and RSA is extensively used to ensure the authenticity, integrity, and confidentiality of data in secure networks.
		/// Therefore, by choosing the RealibleEncrypted (2) mode, you are adopting a reliable and internationally recognized approach to protect your critical data against security threats.
		/// </summary>
		/// <remarks>
		/// It is crucial to choose RealibleEncrypted (2) when dealing with sensitive information, recognizing that the additional security measures contribute to enhanced data protection.
		/// Users are advised to review the documentation for comprehensive details on the bidirectional encryption process and the integration of the RSA algorithm.
		/// This mode helps mitigate security risks, including potential replay attacks, and ensures that data remains secure even if intercepted.
		/// </remarks>
		/// <remarks>
		/// </remarks>
		/// <example>
		/// Example of usage:
		/// <code>
		///   DataDeliveryMode deliveryMode = DataDeliveryMode.ReliableEncrypted;
		///   // Further implementation details...
		/// </code>
		/// </example>
		/// <seealso cref="DataDeliveryMode"/>
		ReliableEncryptedSequenced = 6,
	}
}

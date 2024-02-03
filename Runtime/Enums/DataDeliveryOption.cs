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
}

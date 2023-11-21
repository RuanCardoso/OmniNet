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
            Connect = 254,
            Disconnect = 255,
        }

        /// <summary>
        /// Manages the types of data cached on the server.
        /// </summary>
        public enum DataStorageType : byte
        {
            /// <summary>
            /// Get all data marked as "Remote" from the server by its ID.
            /// </summary>
            Remote = 0,
            /// <summary>
            /// Get all data marked as "OnSerializeView" from the server by its ID.
            /// </summary>
            OnSerializeView = 1,
            /// <summary>
            /// Get all data marked as "NetworkVars" from the server by its ID.
            /// </summary>
            NetworkVars = 2,
            /// <summary>
            /// Get all data marked as "GlobalMessage" from the server by its ID.
            /// </summary>
            GlobalMessage = 3,
            /// <summary>
            /// Get all data marked as "LocalMessage" from the server by its ID.
            /// </summary>
            LocalMessage = 4,
            /// <summary>
            /// Get all data marked as " GlobalRemote" from the server by its ID.
            /// </summary>
            GlobalRemote = 5,
        }

        /// <summary>
        /// Enum that represents the data delivery mode.
        /// </summary>
        public enum DataDeliveryMode : byte
        {
            /// <summary>
            /// Unreliable data delivery mode, data may be lost, duplicated or arrive out of order.<br/>
            /// Use this mode for data that is updated frequently, such as player position, rotation, etc.<br/>
            /// </summary>
            Unsecured = 0,
            /// <summary>
            /// Reliable data delivery mode, data will not be lost, duplicated or arrive out of order.<br/>
            /// Use this mode for data that is updated infrequently, such as player name, health, damage, database operations, etc.<br/>
            /// The data is not encrypted, use this mode only if the data is not sensitive.<br/>
            /// </summary>
            Secured = 1,
            /// <summary>
            /// Reliable data delivery mode, data will not be lost, duplicated or arrive out of order.<br/>
            /// The data is encrypted, use this mode for sensitive data, such as passwords, emails, identification, card numbers, phone numbers, transactions, etc.<br/>
            /// May have a performance cost and bandwidth usage. 128-bit AES encryption is used.<br/>
            /// two-way encryptation, the data is encrypted on the client and server, and decrypted on the client and server by default. See the documentation for more information.<br/>
            /// </summary>
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
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
        internal enum NetworkEvents : byte
        {
            OnPlayerConnected,
            OnPlayerDisconnected
        }

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
            FireEvent = 24,
            Connect = 254,
            Disconnect = 255,
        }

        public enum DataStorageType : byte
        {
            Remote = 0,
            OnSerialize = 1,
            OnSync = 2,
            GlobalMessage = 3,
            LocalMessage = 4,
            GlobalRemote,
        }

        public enum DataDeliveryMode : byte
        {
            Unsecured = 0,
            Secured = 1,
        }

        public enum DataTarget : byte
        {
            Server = 0,
            Broadcast = 1,
            BroadcastExcludingSelf = 2,
            Self = 3,
        }

        public enum DataProcessingOption : byte
        {
            DoNotProcessOnServer = 0,
            ProcessOnServer = 1,
        }

        public enum DataCachingOption : byte
        {
            None = 0,
            Append = 1,
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

        internal enum EncodingType : int
        {
            UTF8,
            UTF7,
            UTF32,
            ASCII,
            Unicode,
        }

        public enum AuthorityMode
        {
            Mine,
            Server,
            Client,
            Custom
        }
    }
}
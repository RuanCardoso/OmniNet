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

namespace Neutron.Core
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
            // Bandwidth Optimize
            RemotePlayer = 5,
            RemoteDynamic = 6,
            RemoteScene = 7,
            RemoteStatic = 8,
            //......................
            Ping = 9,
            // Bandwidth Optimize
            OnSerializeStatic = 10,
            OnSerializePlayer = 11,
            OnSerializeDynamic = 12,
            OnSerializeScene = 13,
            // Bandwidth Optimize
            OnSyncBaseStatic = 14,
            OnSyncBasePlayer = 15,
            OnSyncBaseDynamic = 16,
            OnSyncBaseScene = 17,
            //......................
            GetCache = 18,
            // Bandwidth Optimize
            LocalMessageStatic = 19,
            LocalMessagePlayer = 20,
            LocalMessageDynamic = 21,
            LocalMessageScene = 22,
            //......................
            Connect = 254,
            Disconnect = 255,
        }

        public enum CacheType : byte
        {
            Remote = 0,
            OnSerialize = 1,
            OnSync = 2,
            GlobalMessage = 3,
            LocalMessage = 4,
        }

        public enum Channel : byte
        {
            Unreliable = 0,
            Reliable = 1,
        }

        public enum Target : byte
        {
            Server = 0,
            All = 1,
            Others = 2,
            Me = 3,
        }

        public enum SubTarget : byte
        {
            None = 0,
            Server = 1
        }

        public enum CacheMode : byte
        {
            None = 0,
            Append = 1,
            Overwrite = 2,
        }

        internal enum ObjectType : byte
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
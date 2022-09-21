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

#if UNITY_2021_3_OR_NEWER
using System;
using UnityEngine;

namespace Neutron.Core
{
    public static class Logger
    {
#if !UNITY_SERVER || UNITY_EDITOR
        public static void Print(object message) => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
        public static void PrintError(object message) => Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "{0}", message);
        public static void PrintWarning(object message) => Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0}", message);
        public static void Log(object message) => Debug.LogFormat(LogType.Log, LogOption.None, null, "{0}", message);
        public static void LogError(object message) => Debug.LogFormat(LogType.Error, LogOption.None, null, "{0}", message);
        public static void LogWarning(object message) => Debug.LogFormat(LogType.Warning, LogOption.None, null, "{0}", message);
        public static void LogStacktrace(Exception message) => Debug.LogException(message);
        public static void Inline(object message) => Print(string.Format("\r{0}", message));
#else
        public static void Print(object message) => Console.WriteLine(message);
        public static void PrintError(object message) => Console.WriteLine(message);
        public static void PrintWarning(object message) => Console.WriteLine(message);
        public static void Log(object message) => Console.WriteLine(message);
        public static void LogError(object message) => Console.WriteLine(message);
        public static void LogWarning(object message) => Console.WriteLine(message);
        public static void LogStacktrace(Exception message) => Console.WriteLine(message);
        public static void Inline(object message) => Console.Write("\r{0}", message);
#endif
    }
}
#endif
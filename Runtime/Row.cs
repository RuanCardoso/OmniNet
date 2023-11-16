using System;
using System.Collections.Generic;

namespace Omni.Core
{
    public class Row : Dictionary<string, object>
    {
        public new object this[string key] => base[key];
        public T Get<T>(string key) => (T)Convert.ChangeType(base[key], typeof(T));
    }
}
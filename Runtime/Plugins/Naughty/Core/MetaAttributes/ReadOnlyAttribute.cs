using System;

namespace Omni
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ReadOnlyAttribute : MetaAttribute
    {

    }
}

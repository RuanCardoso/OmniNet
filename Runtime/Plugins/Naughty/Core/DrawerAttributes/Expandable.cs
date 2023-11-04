using System;

namespace Omni
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ExpandableAttribute : DrawerAttribute
    {
    }
}

using System;

namespace Omni
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class InputAxisAttribute : DrawerAttribute
    {
    }
}

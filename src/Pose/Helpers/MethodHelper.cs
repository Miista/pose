using System;
using System.Reflection;

namespace Pose.Helpers
{
    internal static class MethodHelper
    {
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
        {
            return MethodBase.GetMethodFromHandle(handle, declaringType);
        }
    }
}
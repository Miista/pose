using System;
using System.Reflection;

namespace Pose.Extensions
{
    internal static class MethodBaseExtensions
    {
        public static bool InCoreLibrary(this MethodBase methodBase)
        {
            if (methodBase == null) throw new ArgumentNullException(nameof(methodBase));
            
            var declaringType = methodBase.DeclaringType ?? throw new Exception($"Method {methodBase.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            
            return declaringType.Assembly == typeof(Exception).Assembly;
        }
        
        public static bool IsForValueType(this MethodBase methodBase)
        {
            if (methodBase == null) throw new ArgumentNullException(nameof(methodBase));

            return methodBase.DeclaringType?.IsSubclassOf(typeof(ValueType)) ?? throw new Exception($"Method {methodBase.Name} does not have a {nameof(MethodBase.DeclaringType)}");
        }

        public static bool IsOverride(this MethodBase methodBase)
        {
            if (!(methodBase is MethodInfo methodInfo))
                return false;

            return methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;
        }
    }
}
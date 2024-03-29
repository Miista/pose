using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Pose.Extensions;

namespace Pose.Helpers
{
    internal static class StubHelper
    {
        private static readonly MethodInfo GetMethodDescriptor =
            typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Exception($"Cannot get method GetMethodDescriptor from type {nameof(DynamicMethod)}");

        public static IntPtr GetMethodPointer(MethodBase method)
        {
            if (method is DynamicMethod dynamicMethod)
            {
                return ((RuntimeMethodHandle)GetMethodDescriptor.Invoke(dynamicMethod, null)).GetFunctionPointer();
            }

            return method.MethodHandle.GetFunctionPointer();
        }

        public static object GetShimDelegateTarget(int index)
            => PoseContext.Shims[index].Replacement.Target;

        public static MethodInfo GetShimReplacementMethod(int index)
            => PoseContext.Shims[index].Replacement.Method;

        public static int GetIndexOfMatchingShim(MethodBase methodBase, Type type, object obj)
        {
            if (methodBase.IsStatic || obj == null)
                return Array.FindIndex(PoseContext.Shims, s => s.Original == methodBase);

            var index = Array.FindIndex(PoseContext.Shims,
                s => ReferenceEquals(obj, s.Instance) && s.Original == methodBase);

            if (index == -1)
                return Array.FindIndex(PoseContext.Shims,
                            s => SignatureEquals(s, type, methodBase) && s.Instance == null);

            return index;
        }

        public static int GetIndexOfMatchingShim(MethodBase methodBase, object obj)
            => GetIndexOfMatchingShim(methodBase, methodBase.DeclaringType, obj);

        public static MethodInfo DeVirtualizeMethod(object obj, MethodInfo virtualMethod)
        {
            return DeVirtualizeMethod(obj.GetType(), virtualMethod);
        }

        public static MethodInfo DeVirtualizeMethod(Type thisType, MethodInfo virtualMethod)
        {
            if (thisType == virtualMethod.DeclaringType) return virtualMethod;
            
            var bindingFlags = BindingFlags.Instance | (virtualMethod.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);
            var types = virtualMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            
            return thisType.GetMethod(virtualMethod.Name, bindingFlags, null, types, null);
        }

        public static Module GetOwningModule() => typeof(StubHelper).Module;

        public static bool IsIntrinsic(MethodBase method)
        {
            var methodIsMarkedIntrinsic = method.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Runtime.CompilerServices.IntrinsicAttribute");
            
            var declaringType = method.DeclaringType ?? throw new Exception($"Method {method.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            var declaringTypeIsMarkedIntrinsic = declaringType.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Runtime.CompilerServices.IntrinsicAttribute");

            var declaringTypeFullName = declaringType.FullName ?? throw new Exception($"Type {declaringType.Name} does not have a {nameof(Type.FullName)}");
            var declaringTypeDerivesFromIntrinsic = declaringTypeFullName.StartsWith("System.Runtime.Intrinsics");
            
            return methodIsMarkedIntrinsic || declaringTypeIsMarkedIntrinsic || declaringTypeDerivesFromIntrinsic;
        }

        public static string CreateStubNameFromMethod(string prefix, MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(prefix));

            var name = prefix;
            name += "_";
            name += method.DeclaringType?.ToString() ?? throw new Exception($"Method {method.Name} does not have a {nameof(MethodBase.DeclaringType)}");
            name += "_";
            name += method.Name;

            if (!method.IsConstructor)
            {
                var genericArguments = method.GetGenericArguments();
                if (genericArguments.Length > 0)
                {
                    name += "[";
#if NETSTANDARD2_1_OR_GREATER
                    name += string.Join(',', genericArguments.Select(g => g.Name));
#else
                    name += string.Join(",", genericArguments.Select(g => g.Name));
#endif
                    name += "]";
                }
            }

            return name;
        }

        private static bool SignatureEquals(Shim shim, Type type, MethodBase method)
        {
            if (shim.Type == null || type == shim.Type)
                return $"{shim.Type}::{shim.Original}" == $"{type}::{method}";

            if (type.IsSubclassOf(shim.Type))
            {
                if ((shim.Original.IsAbstract || !shim.Original.IsVirtual)
                        || (shim.Original.IsVirtual && !method.IsOverride()))
                {
                    return $"{shim.Original}" == $"{method}";
                }
            }

            return false;
        }
    }
}
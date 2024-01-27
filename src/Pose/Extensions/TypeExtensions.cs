﻿using System;
using System.Linq;
using System.Reflection;

namespace Pose.Extensions
{
    internal static class TypeExtensions
    {
        public static bool ImplementsInterface<TInterface>(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(TInterface).IsInterface) throw new InvalidOperationException($"{typeof(TInterface)} is not an interface.");

            return type.GetInterfaces().Any(interfaceType => interfaceType == typeof(TInterface));
        }

        public static bool HasAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            
            var compilerGeneratedAttribute = type.GetCustomAttribute<TAttribute>() ?? type.ReflectedType?.GetCustomAttribute<TAttribute>();

            return compilerGeneratedAttribute != null;
        }

        public static MethodInfo GetExplicitlyImplementedMethod<TInterface>(this Type type, string methodName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(methodName));
            
            var interfaceType = type.GetInterfaceType<TInterface>() ?? throw new Exception();
            var method = interfaceType.GetMethod(methodName) ?? throw new Exception();
            var methodDeclaringType = method.DeclaringType ?? throw new Exception($"The {methodName} method does not have a declaring type");
            var interfaceMapping = type.GetInterfaceMap(methodDeclaringType);
            var requestedTargetMethod = interfaceMapping.TargetMethods.FirstOrDefault(m => m.Name == methodName);

            return requestedTargetMethod;
        }

        private static Type GetInterfaceType<TInterface>(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(TInterface).IsInterface) throw new InvalidOperationException($"{typeof(TInterface)} is not an interface.");

            return type.GetInterfaces().FirstOrDefault(interfaceType => interfaceType == typeof(TInterface));
        }
    }
}
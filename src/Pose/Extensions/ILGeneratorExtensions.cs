using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Pose.Extensions
{
    internal static class ILGeneratorExtensions
    {
        public static byte[] GetILBytes(this ILGenerator ilGenerator)
        {
#if NET8_0_OR_GREATER
            var runtimeILGeneratorType = Type.GetType("System.Reflection.Emit.RuntimeILGenerator") ?? throw new Exception("Cannot find type System.Reflection.Emit.RuntimeILGenerator");
            var bakeByteArray = runtimeILGeneratorType.GetMethod("BakeByteArray", BindingFlags.Instance | BindingFlags.NonPublic)
                                ?? throw new Exception($"Cannot get method BakeByteArray from type {nameof(ILGenerator)}");
            var ilBytes = (byte[])bakeByteArray.Invoke(ilGenerator, null);
            return ilBytes;
#else
            var bakeByteArray = typeof(ILGenerator).GetMethod("BakeByteArray", BindingFlags.Instance | BindingFlags.NonPublic)
                                ?? throw new Exception($"Cannot get method BakeByteArray from type {nameof(ILGenerator)}");
            var ilBytes = (byte[])bakeByteArray.Invoke(ilGenerator, null);
            return ilBytes;
#endif
        }
    }
}
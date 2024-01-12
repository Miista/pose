using System.Reflection;
using System.Reflection.Emit;

namespace Pose.Extensions
{
    internal static class ILGeneratorExtensions
    {
        public static byte[] GetILBytes(this ILGenerator ilGenerator)
        {
            var bakeByteArray = typeof(ILGenerator).GetMethod("BakeByteArray", BindingFlags.Instance | BindingFlags.NonPublic);
            var ilBytes = (byte[])bakeByteArray.Invoke(ilGenerator, null);
            return ilBytes;
        }
    }
}
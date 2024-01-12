using System.Reflection;
using System.Reflection.Emit;

namespace Pose.Extensions
{
    // ReSharper disable once InconsistentNaming
    internal static class ILGeneratorExtensions
    {
        // ReSharper disable once InconsistentNaming
        public static byte[] GetILBytes(this ILGenerator ilGenerator)
        {
            var bakeByteArray = typeof(ILGenerator).GetMethod("BakeByteArray", BindingFlags.Instance | BindingFlags.NonPublic);
            var ilBytes = (byte[])bakeByteArray.Invoke(ilGenerator, null);
            return ilBytes;
        }
    }
}
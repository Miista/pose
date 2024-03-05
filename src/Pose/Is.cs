using System.Diagnostics.CodeAnalysis;

namespace Pose
{
    [ExcludeFromCodeCoverage]
    public static class Is
    {
        public static T A<T>() => default(T);
    }
}
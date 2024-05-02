using System.Diagnostics.CodeAnalysis;

namespace Pose
{
    [ExcludeFromCodeCoverage(Justification = "A simple wrapper")]
    public static class Is
    {
        public static T A<T>() => default(T);
    }
}
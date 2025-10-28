namespace Pose.Exceptions
{
    using System;
    
    public class MethodRewriteException : Exception
    {
        public MethodRewriteException(string message) : base(message) { }
    }
}
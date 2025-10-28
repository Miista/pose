namespace Pose.Exceptions
{
    using System;

    public class UnsupportedExpressionException : Exception
    {
        public UnsupportedExpressionException(string message) : base(message) { }
    }
}
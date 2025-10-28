namespace Pose.Exceptions
{
    using System;
    
    public class InvalidShimSignatureException : Exception
    {
        public InvalidShimSignatureException(string message) : base(message) { }
    }
}
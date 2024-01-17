namespace Pose.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    
    [Serializable]
    internal class InvalidShimSignatureException : Exception
    {
        public InvalidShimSignatureException() { }
        public InvalidShimSignatureException(string message) : base(message) { }
        public InvalidShimSignatureException(string message, Exception inner) : base(message, inner) { }
        
#if !NET8_0_OR_GREATER
        protected InvalidShimSignatureException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }
}
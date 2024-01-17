namespace Pose.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    
    [Serializable]
    public class MethodRewriteException : Exception
    {
        public MethodRewriteException() { }
        
#if !NET8_0_OR_GREATER
        protected MethodRewriteException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
        
        public MethodRewriteException(string message) : base(message) { }
        
        public MethodRewriteException(string message, Exception innerException) : base(message, innerException) { }
    }
}
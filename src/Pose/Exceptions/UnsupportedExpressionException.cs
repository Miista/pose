namespace Pose.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    
    public class UnsupportedExpressionException : Exception
    {
        public UnsupportedExpressionException() { }
        public UnsupportedExpressionException(string message) : base(message) { }
        public UnsupportedExpressionException(string message, Exception inner) : base(message, inner) { }
        
#if !NET8_0_OR_GREATER
        protected UnsupportedExpressionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }
}
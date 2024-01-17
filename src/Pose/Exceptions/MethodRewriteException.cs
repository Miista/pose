using System;
using System.Runtime.Serialization;

namespace Pose.Exceptions
{
    [Serializable]
    public class MethodRewriteException : Exception
    {
        public MethodRewriteException() { }
        
        protected MethodRewriteException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        
        public MethodRewriteException(string message) : base(message) { }
        
        public MethodRewriteException(string message, Exception innerException) : base(message, innerException) { }
    }
}
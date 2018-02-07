using System;
using System.Runtime.Serialization;

namespace Ps4EditLib.Exceptions
{
    public class InEqualityException : Exception
    {
        public InEqualityException()
        {
        }

        public InEqualityException(string message) : base(message)
        {
        }

        public InEqualityException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InEqualityException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
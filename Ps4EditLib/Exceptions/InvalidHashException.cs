using System;
using System.Runtime.Serialization;

namespace Ps4EditLib.Exceptions
{
    public class InvalidChecksumException : Exception
    {
        public InvalidChecksumException()
        {
        }

        public InvalidChecksumException(string message) : base(message)
        {
        }

        public InvalidChecksumException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidChecksumException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
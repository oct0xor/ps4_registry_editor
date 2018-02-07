using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ps4EditLib.Exceptions
{
    public class InvalidArgumentException : Exception
    {
        public InvalidArgumentException()
        {
        }

        public InvalidArgumentException(string message) : base(message)
        {
        }

        public InvalidArgumentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidArgumentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
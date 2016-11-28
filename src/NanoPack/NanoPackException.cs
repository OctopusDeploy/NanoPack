using System;

namespace NanoPack
{
    public class NanoPackException : Exception
    {
        public NanoPackException(string message):base(message)
        {
        }

        public NanoPackException(string message, Exception ex) : base(message, ex)
        {
        }
    }
}

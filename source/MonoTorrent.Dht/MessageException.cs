#if !DISABLE_DHT
using System;

namespace MonoTorrent.Dht
{
    internal class MessageException : Exception
    {
        private readonly ErrorCode errorCode;

        public MessageException(ErrorCode errorCode, string message) : base(message)
        {
            this.errorCode = errorCode;
        }

        public ErrorCode ErrorCode
        {
            get { return errorCode; }
        }
    }
}

#endif
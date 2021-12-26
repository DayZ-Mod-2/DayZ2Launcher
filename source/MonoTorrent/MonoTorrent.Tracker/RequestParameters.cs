using System;
using System.Collections.Specialized;
using System.Net;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Tracker
{
    public abstract class RequestParameters : EventArgs
    {
        protected internal static readonly string FailureKey = "failure reason";
        protected internal static readonly string WarningKey = "warning message";

        private readonly NameValueCollection parameters;
        private readonly BEncodedDictionary response;

        protected RequestParameters(NameValueCollection parameters, IPAddress address)
        {
            this.parameters = parameters;
            RemoteAddress = address;
            response = new BEncodedDictionary();
        }

        public abstract bool IsValid { get; }

        public NameValueCollection Parameters
        {
            get { return parameters; }
        }

        public BEncodedDictionary Response
        {
            get { return response; }
        }

        public IPAddress RemoteAddress { get; protected set; }
    }
}
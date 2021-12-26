#if !DISABLE_DHT
using MonoTorrent.Dht.Messages;
using System.Net;

namespace MonoTorrent.Dht
{
    internal class SendQueryEventArgs : TaskCompleteEventArgs
    {
        private readonly IPEndPoint endpoint;
        private readonly QueryMessage query;
        private readonly ResponseMessage response;

        public SendQueryEventArgs(IPEndPoint endpoint, QueryMessage query, ResponseMessage response)
            : base(null)
        {
            this.endpoint = endpoint;
            this.query = query;
            this.response = response;
        }

        public IPEndPoint EndPoint
        {
            get { return endpoint; }
        }

        public QueryMessage Query
        {
            get { return query; }
        }

        public ResponseMessage Response
        {
            get { return response; }
        }

        public bool TimedOut
        {
            get { return response == null; }
        }
    }
}

#endif
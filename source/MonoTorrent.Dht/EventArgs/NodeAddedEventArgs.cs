#if !DISABLE_DHT
using System;

namespace MonoTorrent.Dht
{
	internal class NodeAddedEventArgs : EventArgs
	{
		private readonly Node node;

		public NodeAddedEventArgs(Node node)
		{
			this.node = node;
		}

		public Node Node
		{
			get { return node; }
		}
	}
}

#endif
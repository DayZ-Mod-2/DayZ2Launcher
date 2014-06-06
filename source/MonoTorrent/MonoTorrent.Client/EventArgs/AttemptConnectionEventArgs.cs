using System;

namespace MonoTorrent.Client
{
	public class AttemptConnectionEventArgs : EventArgs
	{
		private readonly Peer peer;

		public AttemptConnectionEventArgs(Peer peer)
		{
			this.peer = peer;
		}

		public bool BanPeer { get; set; }

		public Peer Peer
		{
			get { return peer; }
		}
	}
}
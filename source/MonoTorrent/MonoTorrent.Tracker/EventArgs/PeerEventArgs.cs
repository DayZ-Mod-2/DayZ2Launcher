using System;

namespace MonoTorrent.Tracker
{
	public abstract class PeerEventArgs : EventArgs
	{
		private readonly Peer peer;
		private readonly SimpleTorrentManager torrent;

		protected PeerEventArgs(Peer peer, SimpleTorrentManager torrent)
		{
			this.peer = peer;
			this.torrent = torrent;
		}

		public Peer Peer
		{
			get { return peer; }
		}

		public SimpleTorrentManager Torrent
		{
			get { return torrent; }
		}
	}
}
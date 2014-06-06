using System;
using System.Collections.Generic;

namespace MonoTorrent.Tracker
{
	public class ScrapeEventArgs : EventArgs
	{
		private readonly List<SimpleTorrentManager> torrents;

		public ScrapeEventArgs(List<SimpleTorrentManager> torrents)
		{
			this.torrents = torrents;
		}

		public List<SimpleTorrentManager> Torrents
		{
			get { return torrents; }
		}
	}
}
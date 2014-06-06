using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoTorrent.Client.Tracker
{
	public class TrackerTier : IEnumerable<Tracker>
	{
		#region Private Fields

		private readonly List<Tracker> trackers;

		#endregion Private Fields

		#region Properties

		internal bool SendingStartedEvent { get; set; }

		internal bool SentStartedEvent { get; set; }

		internal List<Tracker> Trackers
		{
			get { return trackers; }
		}

		#endregion Properties

		#region Constructors

		internal TrackerTier(IEnumerable<string> trackerUrls)
		{
			Uri result;
			var trackerList = new List<Tracker>();

			foreach (string trackerUrl in trackerUrls)
			{
				// FIXME: Debug spew?
				if (!Uri.TryCreate(trackerUrl, UriKind.Absolute, out result))
				{
					Logger.Log(null, "TrackerTier - Invalid tracker Url specified: {0}", trackerUrl);
					continue;
				}

				Tracker tracker = TrackerFactory.Create(result);
				if (tracker != null)
				{
					trackerList.Add(tracker);
				}
				else
				{
					Console.Error.WriteLine("Unsupported protocol {0}", result); // FIXME: Debug spew?
				}
			}

			trackers = trackerList;
		}

		#endregion Constructors

		#region Methods

		public IEnumerator<Tracker> GetEnumerator()
		{
			return trackers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		internal int IndexOf(Tracker tracker)
		{
			return trackers.IndexOf(tracker);
		}

		public List<Tracker> GetTrackers()
		{
			return new List<Tracker>(trackers);
		}

		#endregion Methods
	}
}
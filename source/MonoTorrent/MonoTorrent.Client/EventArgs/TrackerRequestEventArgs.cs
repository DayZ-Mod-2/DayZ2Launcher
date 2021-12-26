using System;

namespace MonoTorrent.Client.Tracker
{
    public abstract class TrackerResponseEventArgs : EventArgs
    {
        private readonly TrackerConnectionID id;

        protected TrackerResponseEventArgs(Tracker tracker, object state, bool successful)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");
            if (!(state is TrackerConnectionID))
                throw new ArgumentException("The state object must be the same object as in the call to Announce", "state");
            id = (TrackerConnectionID)state;
            this.Successful = successful;
            this.Tracker = tracker;
        }

        internal TrackerConnectionID Id
        {
            get { return id; }
        }

        public object State
        {
            get { return id; }
        }

        /// <summary>
        ///     True if the request completed successfully
        /// </summary>
        public bool Successful { get; set; }

        /// <summary>
        ///     The tracker which the request was sent to
        /// </summary>
        public Tracker Tracker { get; protected set; }
    }
}
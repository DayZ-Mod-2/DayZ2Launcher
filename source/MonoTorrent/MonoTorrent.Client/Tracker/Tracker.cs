//
// Tracker.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using MonoTorrent.Common;

namespace MonoTorrent.Client.Tracker
{
    public abstract class Tracker : ITracker
    {
        private readonly Uri uri;
        private string failureMessage;
        private TimeSpan minUpdateInterval;
        private TimeSpan updateInterval;
        private string warningMessage;

        protected Tracker(Uri uri)
        {
            Check.Uri(uri);
            MinUpdateInterval = TimeSpan.FromMinutes(3);
            UpdateInterval = TimeSpan.FromMinutes(30);
            this.uri = uri;
        }

        public event EventHandler BeforeAnnounce;
        public event EventHandler<AnnounceResponseEventArgs> AnnounceComplete;
        public event EventHandler BeforeScrape;
        public event EventHandler<ScrapeResponseEventArgs> ScrapeComplete;

        public bool CanAnnounce { get; protected set; }

        public bool CanScrape { get; set; }

        public int Complete { get; protected set; }

        public int Downloaded { get; protected set; }

        public string FailureMessage
        {
            get { return failureMessage ?? ""; }
            protected set { failureMessage = value; }
        }

        public int Incomplete { get; protected set; }

        public TimeSpan MinUpdateInterval
        {
            get { return minUpdateInterval; }
            protected set { minUpdateInterval = value; }
        }

        public TrackerState Status { get; protected set; }

        public TimeSpan UpdateInterval
        {
            get { return updateInterval; }
            protected set { updateInterval = value; }
        }

        public Uri Uri
        {
            get { return uri; }
        }

        public string WarningMessage
        {
            get { return warningMessage ?? ""; }
            protected set { warningMessage = value; }
        }

        public abstract void Announce(AnnounceParameters parameters, object state);
        public abstract void Scrape(ScrapeParameters parameters, object state);

        protected virtual void RaiseBeforeAnnounce()
        {
            EventHandler h = BeforeAnnounce;
            if (h != null)
                h(this, EventArgs.Empty);
        }

        protected virtual void RaiseAnnounceComplete(AnnounceResponseEventArgs e)
        {
            EventHandler<AnnounceResponseEventArgs> h = AnnounceComplete;
            if (h != null)
                h(this, e);
        }

        protected virtual void RaiseBeforeScrape()
        {
            EventHandler h = BeforeScrape;
            if (h != null)
                h(this, EventArgs.Empty);
        }

        protected virtual void RaiseScrapeComplete(ScrapeResponseEventArgs e)
        {
            EventHandler<ScrapeResponseEventArgs> h = ScrapeComplete;
            if (h != null)
                h(this, e);
        }
    }
}
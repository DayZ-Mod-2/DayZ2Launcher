namespace MonoTorrent.Client.Tracker
{
    public class ScrapeParameters
    {
        private readonly InfoHash infoHash;


        public ScrapeParameters(InfoHash infoHash)
        {
            this.infoHash = infoHash;
        }

        public InfoHash InfoHash
        {
            get { return infoHash; }
        }
    }
}
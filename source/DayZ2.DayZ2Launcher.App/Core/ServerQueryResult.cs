using System.Collections.Generic;
using System.Net;

namespace DayZ2.DayZ2Launcher.App.Core
{
    public class ServerQueryResult
    {
        public IPAddress IP { get; set; }
        public ushort Port { get; set; }
        public long Ping { get; set; }
        public SortedDictionary<string, string> Settings { get; set; }
        public List<Player> Players { get; set; }
    }
}
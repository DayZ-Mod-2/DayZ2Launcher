namespace DayZ2.DayZ2Launcher.App.Core
{
	public class ServerUpdated
	{
		public ServerUpdated(Server server, bool suppressRefresh, bool isRemoved = false)
		{
			Server = server;
			SuppressRefresh = suppressRefresh;
			IsRemoved = isRemoved;
		}

		public Server Server { get; set; }
		public bool SuppressRefresh { get; set; }
		public bool IsRemoved { get; set; }
	}
}

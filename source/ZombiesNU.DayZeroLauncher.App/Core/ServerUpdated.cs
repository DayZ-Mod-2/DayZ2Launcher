using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerUpdated
	{
		public Server Server { get; set; }
		public bool SupressRefresh { get; set; }
		public bool IsRemoved { get; set; }

		public ServerUpdated(Server server, bool supressRefresh, bool isRemoved = false)
		{
			this.Server = server;
			this.SupressRefresh = supressRefresh;
			this.IsRemoved = isRemoved;
		}
	}
}
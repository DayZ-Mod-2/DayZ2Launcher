using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class Filters
	{

	}

	public struct Player
	{
		public string Name;
		public int Score;
		public string Duration;
	}

	public enum ServerDifficulty
	{
		Recruit = 0,
		Regular = 1,
		Veteran = 2,
		Mercenary = 3
	}

	public enum ServerState
	{
		None,
		SelectingMission,
		EditingMission,
		AssigningRoles,
		SendingMission,
		LoadingGame,
		Briefing,
		Playing,
		Debriefing,
		MissionAborted
	}

	public enum ServerPlatform
	{
		Undefined = 0,
		Windows = 'w',
		Linux = 'l'
	}

	public enum ServerPerspective
	{
		FirstPerson,
		ThirdPerson
	}

	public class Server
	{
		public string Name;
		public readonly string Hostname;
		public readonly ushort GamePort;
		public readonly ushort QueryPort;
		public bool IsResponding;
		public string Version;
		public string RequiredVersion;
		public string RequiredBuildNo;
		public bool EqualModRequired;
		public bool Lock;
		public bool VerifySignatures;
		public bool Battleye = false;
		public bool Dedicated;
		public long? Ping;
		public int Slots;
		public int PlayerCount;
		public int FreeSlots => Slots - PlayerCount;
		public IList<Player> Players;
		public IList<string> Mods;
		public ServerDifficulty Difficulty;
		public ServerPerspective Perspective;
		public ServerState State;
		public string GameType;
		public string Language;
		public string LatLong;
		public ServerPlatform Platform = ServerPlatform.Undefined;
		public string ContentLoadedHash;
		public string Country;
		public string TimeLeft;
		public bool IsFavorite;

		public event EventHandler<EventArgs> RefreshStarted;
		public event EventHandler<EventArgs> RefreshFinished;
		public event EventHandler<EventArgs> Refreshed;

		public Server(ServerListInfo info)
		{
			Hostname = info.Hostname;
			GamePort = info.Port;
			QueryPort = (ushort)(info.Port + 1);
			Mods = info.Mods;
		}

		public async Task RefreshAsync(CancellationToken cancellationToken)
		{
			try
			{
				RefreshStarted?.Invoke(this, EventArgs.Empty);
				await ArmaServerQuery.Run(this, cancellationToken);
				IsResponding = true;
			}
			catch (TimeoutException)
			{
				IsResponding = false;
			}
			finally
			{
				RefreshFinished?.Invoke(this, EventArgs.Empty);
			}
			Refreshed?.Invoke(this, EventArgs.Empty);
		}
	}


	public class ServerDiscoveredEventArgs : EventArgs
	{
		public Server Server { get; private set; }

		public ServerDiscoveredEventArgs(Server server)
		{
			Server = server;
		}
	}

	public class ServerList
	{
		public List<Server> Servers { get; private set; } = new();

		public event EventHandler<ServerDiscoveredEventArgs> ServerDiscovered;

		public void SetServers(IList<ServerListInfo> servers)
		{
			Servers.Clear();
			foreach (ServerListInfo info in servers)
			{
				Server server = new Server(info);
				Servers.Add(server);
				ServerDiscovered?.Invoke(this, new ServerDiscoveredEventArgs(server));
			}
		}

		public async Task RefreshAllAsync(IProgress<int> progress, CancellationToken cancellationToken)
		{
			int i = 0;
			async Task Refresh(Server s)
			{
				await s.RefreshAsync(cancellationToken);
				progress.Report(++i);
			}

			await Task.WhenAll(Servers.Select(Refresh));
		}
	}
}

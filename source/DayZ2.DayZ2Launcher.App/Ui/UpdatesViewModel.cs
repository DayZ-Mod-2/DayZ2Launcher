using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using DayZ2.DayZ2Launcher.App.Core;
using UpdateStatus = DayZ2.DayZ2Launcher.App.Core.UpdateStatus;
#pragma warning disable CS4014  // running async from sync

namespace DayZ2.DayZ2Launcher.App.Ui
{
	public class UpdatesViewModel : ViewModelBase
	{
		private readonly CancellationToken m_cancellationToken;

		private readonly LauncherUpdater m_launcherUpdater = new LauncherUpdater();
		private readonly ModUpdater m_modUpdater = new ModUpdater();
		private readonly ServerUpdater m_serverUpdater = new ServerUpdater();
		private readonly MotdUpdater m_motdUpdater = new MotdUpdater();

		public UpdatesViewModel(GameLauncher_old gameLauncher)
		{
			// TODO: when to cancel this?
			var source = new CancellationTokenSource();
			m_cancellationToken = source.Token;

			CalculatedGameSettings = CalculatedGameSettings.Current;

			Task.Run(async () =>
			{
				while (true)
				{
					DayZTorrentStatus = m_modUpdater.CurrentStatus();
					await Task.Delay(100, m_cancellationToken);
				}
			}, m_cancellationToken);

			// TODO: maybe check for updates on a timer too
			async Task Init()
			{
				await CheckForUpdatesAsync();
				await m_modUpdater.StartAsync("dayz2", m_cancellationToken);  // TODO: mod name
			}
			Init();
		}

		private string m_motd;
		public string Motd
		{
			get => m_motd;
			set
			{
				m_motd = value;
				PropertyHasChanged(nameof(Motd));
			}
		}

		private IList<ServerListInfo> m_servers;
		public IList<ServerListInfo> Servers
		{
			get => m_servers;
			set
			{
				m_servers = value;
				PropertyHasChanged(nameof(Servers));
			}
		}

		public struct UpdateInfo
		{
			public readonly UpdateStatus Status;
			public readonly string Text;

			public UpdateInfo(UpdateStatus status, string text)
			{
				Status = status;
				Text = text;
			}
		}

		UpdateInfo m_overallStatus = new UpdateInfo(UpdateStatus.Checking, null);
		public UpdateInfo OverallStatus
		{
			get => m_overallStatus;
			set
			{
				m_overallStatus = value;
				PropertyHasChanged(nameof(OverallStatus));
			}
		}

		void UpdateOverallStatus()
		{
			var statusList = new List<UpdateInfo>() { LauncherStatus, DayZStatus };
			UpdateInfo current = new UpdateInfo(UpdateStatus.UpToDate, "");
			foreach (var status in statusList)
			{
				switch (status.Status)
				{
					case UpdateStatus.UpToDate:
						break;
					case UpdateStatus.Checking:
					case UpdateStatus.OutOfDate:
						if (current.Status != UpdateStatus.Error)
						{
							current = status;
						}

						break;
					case UpdateStatus.Error:
						OverallStatus = current;
						return;
				}
			}
			OverallStatus = current;
		}

		UpdateInfo m_launcherStatus = new UpdateInfo(UpdateStatus.Checking, null);
		public UpdateInfo LauncherStatus
		{
			get => m_launcherStatus;
			set
			{
				m_launcherStatus = value;
				UpdateOverallStatus();
				PropertyHasChanged(nameof(LauncherStatus));
			}
		}

		UpdateInfo m_dayzStatus = new UpdateInfo(UpdateStatus.Checking, null);
		public UpdateInfo DayZStatus
		{
			get => m_dayzStatus;
			set
			{
				m_dayzStatus = value;
				UpdateOverallStatus();
				PropertyHasChanged(nameof(DayZStatus));
			}
		}

		private string m_launcherLatestVersion;
		public string LauncherLatestVersion
		{
			get => m_launcherLatestVersion;
			set
			{
				m_launcherLatestVersion = value;
				PropertyHasChanged(nameof(LauncherLatestVersion));
			}
		}

		private string m_launcherCurrentVersion;
		public string LauncherCurrentVersion
		{
			get => m_launcherCurrentVersion;
			set
			{
				m_launcherCurrentVersion = value;
				PropertyHasChanged(nameof(LauncherCurrentVersion));
			}
		}

		private string m_dayzLatestVersion;
		public string DayZLatestVersion
		{
			get => m_dayzLatestVersion;
			set
			{
				m_dayzLatestVersion = value;
				PropertyHasChanged(nameof(DayZLatestVersion));
			}
		}

		private string m_dayzCurrentVersion;
		public string DayZCurrentVersion
		{
			get => m_dayzCurrentVersion;
			set
			{
				m_dayzCurrentVersion = value;
				PropertyHasChanged(nameof(DayZCurrentVersion));
			}
		}

		string m_dayzTorrentStatus = "";
		public string DayZTorrentStatus
		{
			get => m_dayzTorrentStatus;
			set
			{
				m_dayzTorrentStatus = value;
				PropertyHasChanged(nameof(DayZTorrentStatus));
			}
		}

		public LocalMachineInfo LocalMachineInfo { get; private set; }
		public CalculatedGameSettings CalculatedGameSettings { get; private set; }
		public ListCollectionView DayZVersionStats { get; private set; }

		private bool m_canInstallLauncher;
		public bool CanInstallLauncher
		{
			get => m_canInstallLauncher;
			private set
			{
				m_canInstallLauncher = value;
				PropertyHasChanged(nameof(CanInstallLauncher));
			}
		}

		private bool m_canRestartLauncher;
		public bool CanRestartLauncher
		{
			get => m_canRestartLauncher;
			private set
			{
				m_canRestartLauncher = value;
				PropertyHasChanged(nameof(CanRestartLauncher));
			}
		}

		private bool m_canInstallMod;
		public bool CanInstallMod
		{
			get => m_canInstallMod;
			private set
			{
				m_canInstallMod = value;
				PropertyHasChanged(nameof(CanInstallMod));
			}
		}

		private bool m_canCheckForUpdates;
		public bool CanCheckForUpdates
		{
			get => m_canCheckForUpdates;
			private set
			{
				m_canCheckForUpdates = value;
				PropertyHasChanged(nameof(CanCheckForUpdates));
			}
		}

		private bool m_canLaunchGame;
		public bool CanLaunchGame
		{
			get => m_canLaunchGame;
			private set
			{
				m_canLaunchGame = value;
				PropertyHasChanged(nameof(CanLaunchGame));
			}
		}

		private bool m_canVerifyIntegrity = true;
		public bool CanVerifyIntegrity
		{
			get => m_canVerifyIntegrity;
			private set
			{
				m_canVerifyIntegrity = value;
				PropertyHasChanged(nameof(CanVerifyIntegrity));
			}
		}

		private bool m_isVisible;
		public bool IsVisible
		{
			get => m_isVisible;
			set
			{
				m_isVisible = value;
				PropertyHasChanged(nameof(IsVisible));
			}
		}

		/*
		public void Handle(ServerUpdated message)
		{
			VersionStatistic existingDayZStatistic = null;
			string dayZVersion = null;
			if (message.Server.DayZVersion != null)
			{
				dayZVersion = message.Server.DayZVersion;
				if (_rawDayZVersionStats != null)
					existingDayZStatistic =
						_rawDayZVersionStats.FirstOrDefault(x => x.Version.Equals(dayZVersion, StringComparison.OrdinalIgnoreCase));
			}

			//If we've seen this server (or its gone), decrement what it was last time
			bool serverWasProcessed = _processedServers.ContainsKey(message.Server);
			if (serverWasProcessed || message.IsRemoved)
			{
				if (existingDayZStatistic != null)
					existingDayZStatistic.Count--;
			}

			if (_rawDayZVersionStats == null)
				_rawDayZVersionStats = new ObservableCollection<VersionStatistic>();

			if (existingDayZStatistic == null && !message.IsRemoved)
			{
				if (dayZVersion != null)
					_rawDayZVersionStats.Add(new VersionStatistic { Version = dayZVersion, Count = 1, Parent = this });
			}
			else if (existingDayZStatistic != null)
			{
				if (!message.IsRemoved)
					existingDayZStatistic.Count++;

				_rawDayZVersionStats.Remove(existingDayZStatistic);
				_rawDayZVersionStats.Add(existingDayZStatistic);
			}

			if (!serverWasProcessed && !message.IsRemoved)
			{
				_processedServers.Add(message.Server, new VersionSnapshot(message.Server));
				ProcessedCount++;
			}
		}
		*/

		private async Task ReconfigureTorrentEngineAsync()
		{
			await m_modUpdater.ReconfigureTorrentEngineAsync();
		}

		public void ReconfigureTorrentEngine()
		{
			ReconfigureTorrentEngineAsync();
		}

		private async Task CheckForLauncherUpdatesAsync()
		{
			await m_launcherUpdater.CheckForUpdateAsync(m_cancellationToken);
			CanInstallLauncher = m_launcherUpdater.Status == UpdateStatus.OutOfDate;
			LauncherLatestVersion = m_launcherUpdater.LatestVersion.ToString();
			LauncherCurrentVersion = m_launcherUpdater.CurrentVersion.ToString();
			LauncherStatus = new UpdateInfo(m_launcherUpdater.Status, null);
		}

		private async Task CheckForModUpdatesAsync()
		{
			CanInstallMod = false;
			await m_modUpdater.CheckForUpdateAsync("dayz2", m_cancellationToken);
			CanVerifyIntegrity = m_modUpdater.Status == UpdateStatus.UpToDate && !m_modUpdater.IsRunning;
			CanInstallMod = m_modUpdater.Status == UpdateStatus.OutOfDate && !m_modUpdater.IsRunning;
			DayZLatestVersion = m_modUpdater.LatestVersion.ToString();
			DayZCurrentVersion = m_modUpdater.CurrentVersion.ToString();
			DayZStatus = new UpdateInfo(m_modUpdater.Status, null);
		}

		private async Task CheckForMotdUpdatesAsync()
		{
			if (await m_motdUpdater.CheckForUpdateAsync(m_cancellationToken))
			{
				Motd = m_motdUpdater.Motd;
			}
		}

		private async Task CheckForServerListUpdates()
		{
			if (await m_serverUpdater.CheckForUpdateAsync(m_cancellationToken))
			{
				Servers = m_serverUpdater.ServerList;
			}
		}

		private async Task CheckForUpdatesAsync()
		{
			try
			{
				CanCheckForUpdates = false;
				await Task.WhenAll(
					CheckForLauncherUpdatesAsync(),
					CheckForModUpdatesAsync(),
					CheckForMotdUpdatesAsync(),
					CheckForServerListUpdates());
				await Task.Delay(100, m_cancellationToken);  // give it a tiny cooldown to stop users spamming it
			}
			catch (Exception ex)
			{
				DayZStatus = new UpdateInfo(UpdateStatus.Error, ex.Message);
			}
			finally
			{
				CanCheckForUpdates = true;
			}
		}

		private async Task InstallLatestModVersionAsync()
		{
			try
			{
				CanInstallMod = false;
				CanLaunchGame = false;
				CanVerifyIntegrity = false;
				m_modUpdater.IsRunning = true;
				await m_modUpdater.UpdateAsync("dayz2", m_cancellationToken);  // TODO: mod name
				DayZCurrentVersion = m_modUpdater.CurrentVersion.ToString();
			}
			catch (Exception ex)
			{
				DayZStatus = new UpdateInfo(UpdateStatus.Error, ex.Message);
			}
			finally
			{
				m_modUpdater.IsRunning = false;
				CanLaunchGame = true;
				CanVerifyIntegrity = true;
			}
		}

		public void CheckForUpdates()
		{
			CheckForUpdatesAsync();
		}

		public void InstallLatestModVersion()
		{
			InstallLatestModVersionAsync();
		}

		private async Task VerifyIntegrityAsync()
		{
			try
			{
				CanVerifyIntegrity = false;
				m_modUpdater.IsRunning = true;
				await m_modUpdater.VerifyIntegrityAsync("dayz2", m_cancellationToken);  // TODO: mod name
				await Task.Delay(500, m_cancellationToken);  // give it a tiny cooldown to stop users spamming it
			}
			finally
			{
				m_modUpdater.IsRunning = false;
				CanVerifyIntegrity = true;
			}
		}

		public void VerifyIntegrity() => VerifyIntegrityAsync();
	}
}

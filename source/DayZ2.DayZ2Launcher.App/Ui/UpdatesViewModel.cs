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

		private readonly GameLauncher m_gameLauncher;
		private readonly LauncherUpdater m_launcherUpdater = new();
		private readonly ModUpdater m_modUpdater;
		private readonly ServerUpdater m_serverUpdater = new();
		private readonly MotdUpdater m_motdUpdater = new();

		public UpdatesViewModel(GameLauncher gameLauncher, ModUpdater modUpdater, AppCancellation cancellation)
		{
			m_gameLauncher = gameLauncher;
			m_modUpdater = modUpdater;
			m_cancellationToken = cancellation.Token;

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
			private set => SetValue(ref m_motd, value);
		}

		private IList<ServerListInfo> m_servers;
		public IList<ServerListInfo> Servers
		{
			get => m_servers;
			set => SetValue(ref m_servers, value);
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

		UpdateInfo m_overallStatus = new(UpdateStatus.Checking, null);
		public UpdateInfo OverallStatus
		{
			get => m_overallStatus;
			set => SetValue(ref m_overallStatus, value);
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

		UpdateInfo m_launcherStatus = new(UpdateStatus.Checking, null);
		public UpdateInfo LauncherStatus
		{
			get => m_launcherStatus;
			set => SetValue(ref m_launcherStatus, value);
		}

		UpdateInfo m_dayzStatus = new(UpdateStatus.Checking, null);
		public UpdateInfo DayZStatus
		{
			get => m_dayzStatus;
			set
			{
				m_dayzStatus = value;
				UpdateOverallStatus();
				OnPropertyChanged(new[] { nameof(DayZStatus) });
			}
		}

		private string m_launcherLatestVersion;
		public string LauncherLatestVersion
		{
			get => m_launcherLatestVersion;
			set => SetValue(ref m_launcherLatestVersion, value);
		}

		private string m_launcherCurrentVersion;
		public string LauncherCurrentVersion
		{
			get => m_launcherCurrentVersion;
			set => SetValue(ref m_launcherCurrentVersion, value);
		}

		private string m_dayzLatestVersion;
		public string DayZLatestVersion
		{
			get => m_dayzLatestVersion;
			set => SetValue(ref m_dayzLatestVersion, value);
		}

		private string m_dayzCurrentVersion;
		public string DayZCurrentVersion
		{
			get => m_dayzCurrentVersion;
			set => SetValue(ref m_dayzCurrentVersion, value);
		}

		string m_dayzTorrentStatus = "";
		public string DayZTorrentStatus
		{
			get => m_dayzTorrentStatus;
			set => SetValue(ref m_dayzTorrentStatus, value);
		}

		public LocalMachineInfo LocalMachineInfo { get; private set; }
		public CalculatedGameSettings CalculatedGameSettings { get; private set; }
		public ListCollectionView DayZVersionStats { get; private set; }

		private bool m_canInstallLauncher;
		public bool CanInstallLauncher
		{
			get => m_canInstallLauncher;
			private set => SetValue(ref m_canInstallLauncher, value);
		}

		private bool m_canRestartLauncher;
		public bool CanRestartLauncher
		{
			get => m_canRestartLauncher;
			private set => SetValue(ref m_canRestartLauncher, value);
		}

		private bool m_canInstallMod;
		public bool CanInstallMod
		{
			get => m_canInstallMod;
			private set => SetValue(ref m_canInstallMod, value);
		}

		private bool m_canCheckForUpdates;
		public bool CanCheckForUpdates
		{
			get => m_canCheckForUpdates;
			private set => SetValue(ref m_canCheckForUpdates, value);
		}

		private bool m_canVerifyIntegrity = true;
		public bool CanVerifyIntegrity
		{
			get => m_canVerifyIntegrity;
			private set => SetValue(ref m_canVerifyIntegrity, value);
		}

		private bool m_isVisible;
		public bool IsVisible
		{
			get => m_isVisible;
			set => SetValue(ref m_isVisible, value);
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
			m_gameLauncher.CanLaunch = m_modUpdater.Status == UpdateStatus.UpToDate;
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
			}
			catch (Exception ex)
			{
				DayZStatus = new UpdateInfo(UpdateStatus.Error, ex.Message);
			}
			finally
			{
				await Task.Delay(100, m_cancellationToken); // give it a tiny cooldown to stop users spamming it
				CanCheckForUpdates = true;
			}
		}

		private async Task InstallLatestModVersionAsync()
		{
			try
			{
				CanInstallMod = false;
				m_gameLauncher.CanLaunch = false;
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
				m_gameLauncher.CanLaunch = true;
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
				m_gameLauncher.CanLaunch = false;
				await m_modUpdater.VerifyIntegrityAsync("dayz2", m_cancellationToken);  // TODO: mod name
			}
			finally
			{
				await Task.Delay(500, m_cancellationToken);  // give it a tiny cooldown to stop users spamming it
				m_gameLauncher.CanLaunch = true;
				m_modUpdater.IsRunning = false;
				CanVerifyIntegrity = true;
			}
		}

		public void VerifyIntegrity() => VerifyIntegrityAsync();
	}
}

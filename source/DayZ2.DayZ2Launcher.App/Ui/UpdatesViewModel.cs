using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using DayZ2.DayZ2Launcher.App.Core;
using UpdateStatus = DayZ2.DayZ2Launcher.App.Core.UpdateStatus;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	public class UpdatesViewModel : ViewModelBase
	{
		const string DefaultModName = "dayz2";

		private readonly CancellationToken m_cancellationToken;

		private readonly GameLauncher m_gameLauncher;
		private readonly LauncherUpdater m_launcherUpdater = new();
		private readonly ModUpdater m_modUpdater;
		private readonly ServerUpdater m_serverUpdater;
		private readonly MotdUpdater m_motdUpdater;

		public AppActions AppActions { get; set; }

		public UpdatesViewModel(
			GameLauncher gameLauncher, ModUpdater modUpdater,
			AppCancellation cancellation, MotdUpdater motdUpdater,
			ServerUpdater serverUpdater, AppActions actions)
		{
			m_gameLauncher = gameLauncher;
			m_modUpdater = modUpdater;
			m_motdUpdater = motdUpdater;
			m_serverUpdater = serverUpdater;
			m_cancellationToken = cancellation.Token;
			AppActions = actions;

			m_gameLauncher.GameClosed += delegate(object sender, GameLauncher.GameClosedEventArgs args)
			{
				using var guard = AppActions.Actions.Acquire();
				AppActions.Actions[AppActions.CanLaunch] = true;
			};
			m_gameLauncher.GameLaunched += delegate(object sender, GameLauncher.GameLaunchedEventArgs args)
			{
				using var guard = AppActions.Actions.Acquire();
				AppActions.Actions[AppActions.CanLaunch] = false;
			};

			AppActions.Actions[AppActions.CanLaunch] = m_modUpdater.IsInstalled(DefaultModName);

			CalculatedGameSettings = CalculatedGameSettings.Current;

			async void ProgressAsync()
			{
				while (!m_cancellationToken.IsCancellationRequested)
				{
					DayZTorrentStatus = m_modUpdater.CurrentStatus();
					await Task.Delay(100, m_cancellationToken);
				}
			}
			ProgressAsync();

			// TODO: maybe check for updates on a timer too
			async void Init()
			{
				using var guard = AppActions.Actions.Acquire();
				await CheckForUpdatesAsync();
				await m_modUpdater.StartAsync(m_cancellationToken);
				await CheckForModUpdatesAsync();
				m_modUpdater.CleanupExtraFiles();
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
			set => SetValue(ref m_dayzStatus, value);
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
			set
			{
				m_gameLauncher.DayZCurrentVersion = value;
				SetValue(ref m_dayzCurrentVersion, value);
			}
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

		/*
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

		private bool m_canVerifyIntegrity;
		public bool CanVerifyIntegrity
		{
			get => m_canVerifyIntegrity;
			private set => SetValue(ref m_canVerifyIntegrity, value);
		}
		*/

		private bool m_isVisible;
		public bool IsVisible
		{
			get => m_isVisible;
			set => SetValue(ref m_isVisible, value);
		}

		private async void ReconfigureTorrentEngineAsync()
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
			// CanInstallLauncher = m_launcherUpdater.Status == UpdateStatus.OutOfDate;
			LauncherLatestVersion = m_launcherUpdater.LatestVersion.ToString();
			LauncherCurrentVersion = m_launcherUpdater.CurrentVersion.ToString();
			LauncherStatus = new UpdateInfo(m_launcherUpdater.Status, null);
		}

		private async Task CheckForModUpdatesAsync()
		{
			await m_modUpdater.CheckForUpdateAsync(DefaultModName, m_cancellationToken);
			// CanVerifyIntegrity = m_modUpdater.Status == UpdateStatus.UpToDate && !m_modUpdater.IsRunning;
			// CanInstallMod = m_modUpdater.Status == UpdateStatus.OutOfDate && !m_modUpdater.IsRunning;

			UpdateStatus dayz2Status = m_modUpdater.GetModStatus(DefaultModName);

			AppActions.Actions[AppActions.CanVerifyIntegrity] = m_modUpdater.IsDownloadComplete(DefaultModName);
			AppActions.Actions[AppActions.CanInstallMod] = dayz2Status == UpdateStatus.OutOfDate;

			DayZLatestVersion = m_modUpdater.GetLatestModVersion(DefaultModName).ToString();
			DayZCurrentVersion = m_modUpdater.GetCurrentModVersion(DefaultModName).ToString();
			DayZStatus = new UpdateInfo(dayz2Status, null);
			AppActions.Actions[AppActions.CanLaunch] = dayz2Status == UpdateStatus.UpToDate;
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
			using var guard = AppActions.Actions.Acquire();

			try
			{
				await Task.WhenAll(
					// CheckForLauncherUpdatesAsync(),
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
			}
		}

		public async void CheckForUpdates()
		{
			await CheckForUpdatesAsync();
		}

		private async void InstallLatestModVersionAsync()
		{
			using var guard = AppActions.Actions.Acquire();

			try
			{
				await GameLauncher.CloseGameAsync(m_cancellationToken);
				await m_modUpdater.UpdateAsync(DefaultModName, m_cancellationToken);  // TODO: mod name
				await CheckForModUpdatesAsync();

				AppActions.Actions[AppActions.CanLaunch] = true;
				AppActions.Actions[AppActions.CanVerifyIntegrity] = true;
			}
			catch (Exception ex)
			{
				DayZStatus = new UpdateInfo(UpdateStatus.Error, ex.Message);
			}
			finally
			{
				AppActions.Actions[AppActions.CanLaunch] = true;
			}
		}

		public void InstallLatestModVersion()
		{
			InstallLatestModVersionAsync();
		}

		private async void VerifyIntegrityAsync()
		{
			using var guard = AppActions.Actions.Acquire();

			try
			{
				AppActions.Actions[AppActions.CanLaunch] = false;
				await GameLauncher.CloseGameAsync(m_cancellationToken);
				await m_modUpdater.VerifyIntegrityAsync(DefaultModName, m_cancellationToken);  // TODO: mod name
			}
			finally
			{
				await Task.Delay(500, m_cancellationToken);  // give it a tiny cooldown to stop users spamming it
				AppActions.Actions[AppActions.CanLaunch] = true;
			}
		}

		public void VerifyIntegrity() => VerifyIntegrityAsync();
	}
}

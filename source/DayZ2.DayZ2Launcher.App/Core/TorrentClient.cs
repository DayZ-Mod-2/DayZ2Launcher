using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class TorrentClient : IAsyncDisposable
	{
		public enum Status
		{
			Seeding,
			Downloading,
			Stopped,
			Checking,
			Error
		}

		public TorrentClient()
		{
			m_engine = new ClientEngine(GetEngineSettings());
			App.Current.OnShutdown(this);
		}

		readonly ClientEngine m_engine;

		public struct Progress
		{
			public long DownloadSpeed { get; internal set; }
			public long UploadSpeed { get; internal set; }
			public int SeedCount { get; internal set; }
			public int LeechCount { get; internal set; }
			public int AvailablePeerCount { get; internal set; }
			public int TorrentCount { get; internal set; }
			public double DownloadProgress { get; internal set; }
			public double DownloadedSize { get; internal set; }
			public double TotalSize { get; internal set; }
			public double HashedSize { get; internal set; }
			public double HashingProgress { get; internal set; }
		}

		public Progress CalculateProgress()
		{
			Progress progress = new Progress
			{
				DownloadSpeed = m_engine.TotalDownloadSpeed,
				UploadSpeed = m_engine.TotalUploadSpeed,
				SeedCount = 0,
				LeechCount = 0,
				AvailablePeerCount = 0,
				DownloadProgress = 0,
				DownloadedSize = 0,
				TotalSize = 0,
				HashedSize = 0,
				HashingProgress = 0,
				TorrentCount = m_engine.Torrents.Count
			};

			foreach (TorrentManager tm in m_engine.Torrents)
			{
				progress.AvailablePeerCount += tm.Peers.Available;
				progress.SeedCount += tm.Peers.Seeds;
				progress.LeechCount += tm.Peers.Leechs;
				progress.TotalSize += tm.Size;
				progress.DownloadedSize += tm.Progress / 100.0 * tm.Size;
				progress.HashedSize += tm.Torrent.Size * Convert.ToInt64(tm.Progress / 100);
			}

			if (progress.TotalSize > 0)
			{
				progress.HashingProgress = progress.HashedSize / progress.TotalSize;
				progress.DownloadProgress = progress.DownloadedSize / progress.TotalSize;
			}

			return progress;
		}

		public Status CurrentStatus()
		{
			bool anyError = false;
			bool anyDownloading = false;
			bool anyChecking = false;
			bool anySeeding = false;

			foreach (TorrentManager torrent in m_engine.Torrents)
			{
				switch (torrent.State)
				{
					case TorrentState.Downloading:
						anyDownloading = true;
						break;
					case TorrentState.Seeding:
						anySeeding = true;
						break;
					case TorrentState.Hashing:
						anyChecking = true;
						break;
					case TorrentState.Error:
						anyError = true;
						break;
				}
			}

			if (anyError)
			{
				return Status.Error;
			}

			if (anyDownloading)
			{
				return Status.Downloading;
			}

			if (anyChecking)
			{
				return Status.Checking;
			}

			if (anySeeding)
			{
				return Status.Seeding;
			}

			return Status.Stopped;
		}

		Task m_torrentTask;
		CancellationTokenSource m_cancellationTokenSource;

		class EngineTorrent
		{
			public TorrentManager Manager { get; }
			public TaskCompletionSource TaskCompletionSource { get; }

			public EngineTorrent(TorrentManager manager)
			{
				Manager = manager;
				TaskCompletionSource = new TaskCompletionSource();
			}

			public bool CheckCompletion()
			{
				if (Manager.Complete && !TaskCompletionSource.Task.IsCompleted)
				{
					Debug.WriteLine($"Torrent completed: {Manager.Torrent.Source}");
					TaskCompletionSource.SetResult();
					return true;
				}
				return false;
			}
		}

		readonly Dictionary<string, EngineTorrent> m_torrents = new();

#pragma warning disable CS1998
		public async Task StartAsync()
#pragma warning restore CS1998
		{
			if (m_cancellationTokenSource == null)
			{
				m_cancellationTokenSource = new CancellationTokenSource();
				m_torrentTask = RunTorrents(m_cancellationTokenSource.Token);
			}
		}

		public async Task StopAsync()
		{
			await m_engine.SaveStateAsync();

			if (m_cancellationTokenSource != null)
			{
				m_cancellationTokenSource.Cancel();

				try
				{
					await m_torrentTask;
				}
				catch (OperationCanceledException)
				{
				}

				m_cancellationTokenSource = null;
			}
		}

		public string[] GetTorrentFiles(string torrentFile)
		{
			lock (m_torrents)
			{
				if (m_torrents.ContainsKey(torrentFile))
				{
					return m_torrents[torrentFile].Manager.Files.Select(f => f.Path).ToArray();
				}
			}

			return new string[]{};
		}

		public async Task VerifyTorrentsAsync(bool autoStart = true)
		{
			await Task.WhenAll(m_engine.Torrents.Select(async t =>
			{
				await t.StopAsync();
				await t.HashCheckAsync(autoStart);
			}));
		}

		public async Task<Task> AddTorrentAsync(string torrentFile, string outputPath, CancellationToken cancellationToken)
		{
			if (m_torrents.ContainsKey(torrentFile))
				throw new ArgumentException("Torrent already added.", nameof(torrentFile));

			Torrent torrent = await Torrent.LoadAsync(torrentFile);
			TorrentManager manager = await m_engine.AddAsync(torrent, outputPath, GetTorrentSettings());

			EngineTorrent engineTorrent = new EngineTorrent(manager);

			if (!engineTorrent.CheckCompletion())
			{
				manager.TorrentStateChanged += (object sender, TorrentStateChangedEventArgs e) =>
				{
					engineTorrent.CheckCompletion();
				};
			}

			m_torrents.Add(torrentFile, engineTorrent);

			await manager.StartAsync();

			return engineTorrent.TaskCompletionSource.Task;
		}

		public async Task<bool> RemoveTorrentAsync(string torrentFile, CancellationToken cancellationToken)
		{
			if (m_torrents.ContainsKey(torrentFile))
			{
				TorrentManager torrent = m_torrents[torrentFile].Manager;
				if (torrent.State != TorrentState.Stopped && torrent.State != TorrentState.Stopping)
				{
					await torrent.StopAsync();
				}
				await m_engine.RemoveAsync(torrent);
				m_torrents.Remove(torrentFile);
				return true;
			}

			return false;
		}

		public async Task ReconfigureEngineAsync()
		{
			TorrentOptions torrentOptions = UserSettings.Current.TorrentOptions;

			await m_engine.UpdateSettingsAsync(GetEngineSettings());

			foreach (TorrentManager torrentManager in m_engine.Torrents)
			{
				var torrentSettings = new TorrentSettingsBuilder()
				{
					UploadSlots = torrentOptions.NumULSlotsNormalized
				}.ToSettings();
				await torrentManager.UpdateSettingsAsync(torrentSettings);
			}
		}

		async Task RunTorrents(CancellationToken cancellationToken)
		{
			await m_engine.StartAllAsync();

			try
			{
				while (true)
				{
					await AnnounceAsync(cancellationToken);
					await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
				}
			}
			finally
			{
				await m_engine.StopAllAsync();
			}
		}

		async Task AnnounceAsync(CancellationToken cancellationToken)
		{
			foreach (TorrentManager torrentManager in m_engine.Torrents)
			{
				await torrentManager.DhtAnnounceAsync();
				await torrentManager.LocalPeerAnnounceAsync();
				await torrentManager.TrackerManager.AnnounceAsync(cancellationToken);
			}
		}

		private EngineSettings GetEngineSettings()
		{
			TorrentOptions torrentOptions = UserSettings.Current.TorrentOptions;
			return new EngineSettingsBuilder()
			{
				AllowedEncryption = new List<EncryptionType> { EncryptionType.PlainText, EncryptionType.RC4Full, EncryptionType.RC4Header },
				AllowLocalPeerDiscovery = true,
				AllowPortForwarding = torrentOptions.EnableUpnp,
				AutoSaveLoadDhtCache = true,
				AutoSaveLoadFastResume =
					!UserSettings.Current.TorrentOptions.DisableFastResume,
				CacheDirectory = UserSettings.TorrentJunkPath,
				DhtPort = torrentOptions.ListeningPort,
				ListenPort = torrentOptions.ListeningPort,
				MaximumConnections = torrentOptions.MaxDLConnsNormalized,
				MaximumDownloadSpeed = torrentOptions.MaxDLSpeed * 1024,
				MaximumHalfOpenConnections = 10,
				MaximumUploadSpeed = torrentOptions.MaxULSpeed * 1024,
			}.ToSettings();
		}

		private TorrentSettings GetTorrentSettings()
		{
			return new TorrentSettingsBuilder()
			{
				UploadSlots = UserSettings.Current.TorrentOptions.NumULSlotsNormalized,
				AllowDht = true,
				AllowPeerExchange = true
			}.ToSettings();
		}

		public ValueTask DisposeAsync()
		{
			return new ValueTask(StopAsync());
		}
	}
}

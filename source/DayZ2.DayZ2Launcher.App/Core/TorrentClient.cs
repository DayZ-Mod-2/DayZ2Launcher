using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class TorrentClient
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
		}

		private readonly ClientEngine m_engine;

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
		private CancellationTokenSource m_cancellationTokenSource;

		public struct EngineTorrent
		{
			public TorrentManager Manager { get; private set; }
			// TODO: make this just TaskCompletionSource after .NET upgrade
			public TaskCompletionSource<object> CompletionTask { get; private set; }

			public EngineTorrent(TorrentManager manager)
			{
				Manager = manager;
				CompletionTask = new TaskCompletionSource<object>();
			}
		}

		private readonly Dictionary<string, EngineTorrent> m_torrents = new Dictionary<string, EngineTorrent>();

		public async Task StartAsync()
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

		public EngineTorrent[] Torrents()
		{
			return m_torrents.Values.ToArray();
		}

		public async Task VerifyTorrentsAsync()
		{
			foreach (TorrentManager torrent in m_engine.Torrents)
			{
				// have to stop it to check
				await torrent.StopAsync();
				await torrent.HashCheckAsync(true);
			}
		}

		public async Task AddTorrentAsync(string torrentFile, CancellationToken cancellationToken)
		{
			Torrent torrent = await Torrent.LoadAsync(torrentFile);
			TorrentManager manager = await m_engine.AddAsync(torrent, UserSettings.ContentPackedDataPath, GetTorrentSettings());
			manager.TorrentStateChanged += OnTorrentStateChanged;
			await manager.StartAsync();

			lock (m_torrents)
			{
				if (m_torrents.ContainsKey(torrentFile))
					m_torrents[torrentFile] = new EngineTorrent(manager);
				else
					m_torrents.Add(torrentFile, new EngineTorrent(manager));
			}
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

		private void OnTorrentStateChanged(object obj, TorrentStateChangedEventArgs args)
		{
			if (args.TorrentManager.Complete)
			{
				if (UserSettings.Current.TorrentOptions.StopSeeding)
				{
					args.TorrentManager.StopAsync();
				}
				
				lock (m_torrents)
				{
					EngineTorrent[] engineTorrents = m_torrents
						.Where(t => t.Value.Manager.InfoHash == args.TorrentManager.InfoHash)
						.Select(t => t.Value)
						.ToArray();

					if (engineTorrents.Any())
					{
						if (!engineTorrents[0].CompletionTask.Task.IsCompleted)
							engineTorrents[0].CompletionTask.SetResult(null);
					}
				}

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
					!UserSettings.Current.TorrentOptions.DisableFastResume, // TODO: disable on fullSystemCheck?
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
	}
}

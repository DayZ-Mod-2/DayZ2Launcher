using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DayZ2.DayZ2Launcher.App.Ui;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;

namespace DayZ2.DayZ2Launcher.App.Core
{
    class TorrentUpdater
    {
        public TorrentUpdater(string versionString, List<MetaAddon> addOns, bool fullSystemCheck,
            TorrentLauncher downloader, DayZUpdater updater, bool errorMsgsOnly)
        {
            _versionString = versionString;
            _addOnTorrents = new List<AddOnTorrent>();
            _fullSystemCheck = fullSystemCheck;
            _downloader = downloader;
            _updater = updater;
            _errorMsgsOnly = errorMsgsOnly;

            string torrentsDir = null;
            try
            {
                torrentsDir = Path.Combine(UserSettings.ContentMetaPath, versionString);
                {
                    var dirInfo = new DirectoryInfo(torrentsDir);
                    if (!dirInfo.Exists)
                        dirInfo = Directory.CreateDirectory(torrentsDir);
                }
            }
            catch (Exception ex)
            {
                updater.Status = "Error creating torrents directory";
                downloader.Status = ex.Message;
                downloader.IsRunning = false;

                return;
            }

            // create AddOnTorrents
            foreach (MetaAddon metaAddOn in addOns)
            {
                _addOnTorrents.Add(new AddOnTorrent()
                {
                    Meta = metaAddOn,
                    TorrentFileName = Path.Combine(torrentsDir, $"{metaAddOn.Name}-{metaAddOn.Version}.torrent")
                });
            }

            // delete extra .torrent files
            foreach (string torrentPath in Directory.GetFiles(torrentsDir, "*.torrent", SearchOption.TopDirectoryOnly))
            {
                if (_addOnTorrents.None(t => t.TorrentFileName.Equals(torrentPath, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        var fileInfo = new FileInfo(torrentPath);
                        if (fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                            fileInfo.Refresh();
                        }
                        fileInfo.Delete();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            for (int i = 0; i < _addOnTorrents.Count; i++)
            {
                int idxCopy = i;
                AddOnTorrent newAddOn = _addOnTorrents[i];
                try
                {
                    var wc = new HashWebClient();
                    wc.DownloadFileCompleted += (sender, args) => { TorrentFileDownloadComplete(sender, args, idxCopy); };
                    wc.BeginDownload(newAddOn.Meta.Torrent, newAddOn.TorrentFileName);
                }
                catch (Exception ex)
                {
                    updater.Status = "Error starting torrent file download";
                    downloader.Status = ex.Message;
                    downloader.IsRunning = false;
                    return;
                }
            }
        }

        private class AddOnTorrent
        {
            public MetaAddon Meta;
            public string TorrentFileName;
            public bool TorrentFileDownloadComplete;
        }

        public delegate void StatusUpdate(TorrentState currentState, double currentProgress);

        private static ClientEngine Engine { get; set; }

        public static StatusUpdate StatusCallbacks = (currState, currProgress) => { };
        private readonly List<AddOnTorrent> _addOnTorrents;
        private readonly TorrentLauncher _downloader;
        private readonly bool _errorMsgsOnly;
        private readonly bool _fullSystemCheck;
        private readonly DayZUpdater _updater;
        private readonly string _versionString;

        public static TorrentState CurrentState()
        {
            if (Engine == null)
            {
                return TorrentState.Stopped;
            }

            if (Engine.Torrents.Any(m => m.State == TorrentState.Downloading))
            {
                return TorrentState.Downloading;
            }

            if (Engine.Torrents.Any(m => m.State == TorrentState.Hashing))
            {
                return TorrentState.Hashing;
            }

            return TorrentState.Stopped;
        }

        // gets triggered when the *.torrent file completes downloading
        private void TorrentFileDownloadComplete(Object sender, AsyncCompletedEventArgs args, int addOnIndex)
        {
            string errorMessage = null;
            if (args.Cancelled)
            {
                errorMessage = "Async operation cancelled";
            }
            else if (args.Error != null)
            {
                errorMessage = args.Error.Message;
            }

            if (errorMessage != null)
            {
                _updater.Status = "Torrent file download error";
                _downloader.Status = errorMessage;
                _downloader.IsRunning = false;
                return;
            }

            AddOnTorrent addOnStuff = _addOnTorrents[addOnIndex];
            addOnStuff.TorrentFileDownloadComplete = true;

            // if all torrent files are successfully finished, start the torrents thread
            if (_addOnTorrents.All(t => t.TorrentFileDownloadComplete))
            {
                var tokenSource = new CancellationTokenSource();
                AppDomain.CurrentDomain.ProcessExit += delegate { tokenSource.Cancel(); };
                Task.Run(() => RunTorrents(tokenSource.Token), tokenSource.Token).Start();
            }
        }

        public static Task StopAllTorrents()
        {
            if (Engine == null)
            {
                return Task.CompletedTask;
            }

            return Task.WhenAll(Engine.Torrents
                .Where(n => n.State != TorrentState.Stopped && n.State != TorrentState.Stopping)
                .Select(n => n.StopAsync()));
        }

        private async void OnTorrentStateChanged(object sender, TorrentStateChangedEventArgs e)
        {
            // if the torrent is stopped or has an error, remove it from the engine
            if (e.NewState == TorrentState.Error || e.NewState == TorrentState.Stopped)
            {
                await e.TorrentManager.StopAsync();
                await Engine.RemoveAsync(e.TorrentManager);
                return;
            }

            // if the torrent is finished and seeding, check if all torrents are complete now
            if (e.TorrentManager.Complete && e.NewState == TorrentState.Seeding)
            {
                bool allSeeding = Engine.Torrents.All(t => t.Complete && t.State == TorrentState.Seeding);
                if (allSeeding)
                {
                    _updater.Status = DayZLauncherUpdater.STATUS_UPTODATE;
                    _downloader.IsRunning = false;
                    StatusCallbacks(TorrentState.Stopped, 1);
                }
            }
        }

        public static void Shutdown()
        {
            if (Engine != null)
            {
                Task.WaitAll(StopAllTorrents());
                Engine.Dispose();
                Engine = null;
            }

            foreach (TraceListener lst in Debug.Listeners)
            {
                lst.Flush();
                lst.Close();
            }
        }

        private static EngineSettings GetEngineSettings()
        {
            TorrentOptions torrentOptions = UserSettings.Current.TorrentOptions;
            return new EngineSettingsBuilder()
            {
                AllowedEncryption = new List<EncryptionType> { EncryptionType.PlainText },
                AllowLocalPeerDiscovery = true,
                AllowPortForwarding = true,
                AutoSaveLoadDhtCache = true,
                AutoSaveLoadFastResume = !UserSettings.Current.TorrentOptions.DisableFastResume,  // TODO: disable on fullSystemCheck?
                CacheDirectory = UserSettings.TorrentJunkPath,
                DhtPort = torrentOptions.ListeningPort,
                ListenPort = torrentOptions.ListeningPort,
                MaximumConnections = torrentOptions.MaxDLConnsNormalized,
                MaximumDownloadSpeed = torrentOptions.MaxDLSpeed * 1024,
                MaximumHalfOpenConnections = 10,
                MaximumUploadSpeed = torrentOptions.MaxULSpeed * 1024,
            }.ToSettings();
        }

        public static async void ReconfigureEngine()
        {
            TorrentOptions torrentOptions = UserSettings.Current.TorrentOptions;

            if (Engine == null)
            {
                InitializeEngine(torrentOptions);
                return;
            }

            EngineSettings engineSettings = GetEngineSettings();

            await Engine.UpdateSettingsAsync(engineSettings);

            List<Task> tasks = new List<Task>();
            foreach (TorrentManager torrentManager in Engine.Torrents)
            {
                var torrentSettings = new TorrentSettingsBuilder()
                {
                    UploadSlots = torrentOptions.NumULSlotsNormalized
                }.ToSettings();
                tasks.Add(torrentManager.UpdateSettingsAsync(torrentSettings));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private static void InitializeEngine(TorrentOptions torrentOptions)
        {
            /*
            var dhtListener = DhtListenerFactory.CreateUdp(new IPEndPoint(IPAddress.Any, torrentOptions.ListeningPort));
            var dhtEngine = new DhtEngine(dhtListener);

            var nodes = ReadOnlyMemory<byte>.Empty;
            if (File.Exists(GetDhtNodesFileName()))
            {
                try
                {
                    nodes = File.ReadAllBytes(GetDhtNodesFileName());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Error loading dht nodes file '{GetDhtNodesFileName()}', reason: {ex}");
                }
            }

            await dhtEngine.StartAsync(nodes.ToArray());
            */

            Engine = new ClientEngine(GetEngineSettings());
        }

        async void RunTorrents(CancellationToken cancellationToken)
        {
            TorrentOptions torrentOptions = UserSettings.Current.TorrentOptions;

            if (Engine == null)
            {
                // first engine launch
                InitializeEngine(torrentOptions);
            }
            else
            {
                // engine was already created, lets stop all torrents
                await StopAllTorrents();
            }

            // Create the default settings which a torrent will have.
            var defaultTorrentSettings = new TorrentSettingsBuilder()
            {
                UploadSlots = torrentOptions.NumULSlotsNormalized,
                AllowDht = true,
                AllowPeerExchange = true
            }.ToSettings();

            var torrentFiles = new List<string>();
            // load all torrents into the engine
            foreach (AddOnTorrent addOnTorrent in _addOnTorrents)
            {
                Torrent torrent = null;
                try
                {
                    torrent = await Torrent.LoadAsync(File.ReadAllBytes(addOnTorrent.TorrentFileName));
                }
                catch (Exception ex)
                {
                    _updater.Status = "Error loading torrent file";
                    _downloader.Status = ex.Message;
                    _downloader.IsRunning = false;
                    return;
                }
                foreach (TorrentFile torrentFile in torrent.Files)
                {
                    torrentFiles.Add(Path.Combine(UserSettings.ContentPackedDataPath, torrentFile.Path));
                }

                TorrentManager tm = null;
                try
                {
                    TorrentManager oldManager = Engine.Torrents.FirstOrDefault(t => t.InfoHash == torrent.InfoHash);
                    if (oldManager != null)
                    {
                        await oldManager.StopAsync();
                        await Engine.RemoveAsync(torrent);
                    }
                    tm = await Engine.AddAsync(torrent, UserSettings.ContentPackedDataPath, defaultTorrentSettings);
                }
                catch (Exception ex)
                {
                    // only if critical failure
                    if (tm == null)
                    {
                        _updater.Status = "Error creating torrent manager";
                        _downloader.Status = ex.Message;
                        _downloader.IsRunning = false;
                        return;
                    }
                }
            }

            if (!Engine.Torrents.Any())
            {
                _updater.Status = "Torrent engine error";
                _downloader.Status = "No torrents have been found";
                _downloader.IsRunning = false;
                return;
            }

            // delete files in the save path which don't belong to any torrent
            try
            {
                foreach (string file in Directory.GetFiles(UserSettings.ContentPackedDataPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var fileInfo = new FileInfo(file);
                    if (torrentFiles.None(tf => tf.Equals(fileInfo.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        // this is an unwanted file
                        if (fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                            fileInfo.Refresh();
                        }
                        fileInfo.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                _updater.Status = "Error deleting unwanted file";
                _downloader.Status = ex.Message;
                _downloader.IsRunning = false;
                return;
            }

            try
            {
                File.WriteAllText(UserSettings.ContentCurrentTagFile, _versionString);
                CalculatedGameSettings.Current.Update();
            }
            catch (Exception ex)
            {
                _updater.Status = "Tag file write error";
                _downloader.Status = ex.Message;
                _downloader.IsRunning = false;
                return;
            }

            // start the torrents
            foreach (TorrentManager torrentManager in Engine.Torrents)
            {
                torrentManager.TorrentStateChanged += OnTorrentStateChanged;
                await torrentManager.StartAsync();
            }

            DateTime lastAnnounce = DateTime.MinValue;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                IList<TorrentManager> engineTorrents = Engine.Torrents;

                if (engineTorrents.All(t => t.State == TorrentState.Stopped))
                {
                    break;
                }

                // announce the torrents
                if (lastAnnounce < DateTime.Now.AddMinutes(-1))
                {
                    foreach (TorrentManager torrentManager in engineTorrents)
                    {
                        await torrentManager.TrackerManager.AnnounceAsync(cancellationToken);
                        lastAnnounce = DateTime.Now;
                    }
                }

                TorrentState totalState = TorrentState.Stopped;
                if (engineTorrents.Any(m => m.State == TorrentState.Hashing))
                    totalState = TorrentState.Hashing;
                else if (engineTorrents.Any(m => m.State == TorrentState.Downloading))
                    totalState = TorrentState.Downloading;
                else if (engineTorrents.Any(m => m.State == TorrentState.Seeding))
                    totalState = TorrentState.Seeding;

                // generate status text
                string status;
                try
                {
                    switch (totalState)
                    {
                        case TorrentState.Downloading:
                        {
                            double totalToDownload = 0;
                            double totalDownloaded = 0;
                            double totalDownloadSpeed = 0;
                            double totalUploadSpeed = 0;
                            int totalDownloadConns = 0;
                            int totalUploadConns = 0;
                            foreach (TorrentManager m in engineTorrents)
                            {
                                totalToDownload += m.Torrent.Size;
                                totalDownloaded += m.Torrent.Size * (m.Progress / 100);
                                totalDownloadSpeed += m.Monitor.DownloadSpeed;
                                totalUploadSpeed += m.Monitor.UploadSpeed;
                                totalDownloadConns += m.OpenConnections;
                                totalUploadConns += m.UploadingTo;
                            }

                            double totalDownloadProgress = totalDownloaded / totalToDownload;

                            string statusText =
                                engineTorrents.Any(m => m.State == TorrentState.Downloading && m.Peers.Seeds > 0)
                                    ? "Downloading"
                                    : "Finding peers";

                            status = $"Status: {statusText}";
                            status += $"\nProgress: {totalDownloadProgress * 100:0.00}%";
                            status += $"\nDownload({totalDownloadConns}): {totalDownloadSpeed / 1024.0:0.00} KiB/s";
                            status += $"\nUpload({totalUploadConns}): {totalUploadSpeed / 1024.0:0.00} KiB/s";

                            StatusCallbacks(TorrentState.Downloading, totalDownloadProgress);
                            break;
                        }
                        case TorrentState.Seeding:
                        {
                            double totalUploadSpeed = 0;
                            int totalUploadPeers = 0;
                            foreach (TorrentManager tm in engineTorrents)
                            {
                                totalUploadSpeed += tm.Monitor.UploadSpeed;
                                totalUploadPeers += tm.UploadingTo;
                            }

                            status = $"Seeding({totalUploadPeers}): {totalUploadSpeed / 1024.0:0.00} KiB/s";
                            StatusCallbacks(TorrentState.Seeding, 1);

                            if (UserSettings.Current.TorrentOptions.StopSeeding)
                            {
                                await Engine.StopAllAsync();
                                // TODO: maybe remove them to close the file handle
                            }

                            break;
                        }
                        case TorrentState.Hashing:
                        {
                            double totalHashingBytes = 0;
                            double totalHashedBytes = 0;
                            foreach (TorrentManager m in engineTorrents)
                            {
                                totalHashingBytes += m.Torrent.Size;
                                if (m.State == TorrentState.Hashing)
                                    totalHashedBytes += m.Torrent.Size * (m.Progress / 100);
                                else
                                    totalHashedBytes += m.Torrent.Size;
                            }
                            double totalHashProgress = totalHashedBytes / totalHashingBytes;
                            status = $"Checking files ({totalHashProgress * 100:0.00}%)";

                            StatusCallbacks(TorrentState.Hashing, totalHashProgress);
                            break;
                        }
                        default:
                            status = totalState.ToString();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                }

                _downloader.Status = status;

                await Task.Delay(50, cancellationToken);
            }

            Shutdown();
        }
    }
}

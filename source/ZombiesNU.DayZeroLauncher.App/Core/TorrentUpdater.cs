using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using MonoTorrent.Common;
using MonoTorrent.Client;
using System.Net;
using System.Diagnostics;
using System.Threading;
using MonoTorrent.BEncoding;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Client.Tracker;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;
using System.ComponentModel;


namespace zombiesnu.DayZeroLauncher.App.Core
{
    class TorrentUpdater
    {
        static string basePath;
        static string dhtNodeFile;
        static string downloadsPath;
        static string fastResumeFile;
        static string torrentsPath;
        static ClientEngine engine;				// The engine used for downloading
        static List<TorrentManager> torrents;	// The list where all the torrentManagers will be stored that the engine gives us
        DayZInstaller installer;
        DayZUpdater updater;

        public static TorrentState CurrentState()
        {
            if (torrents == null)
                return TorrentState.Stopped;
            if (torrents.Exists(m => m.State == TorrentState.Downloading))
                return TorrentState.Downloading;
            if (torrents.Exists(m => m.State == TorrentState.Hashing))
                return TorrentState.Hashing;
            return TorrentState.Stopped;
        }

        public static int GetCurrentSpeed()
        {
            if (torrents == null)
                return 0;

            int totalDownloadSpeed = 0;
            foreach (var tm in torrents)
            {
                totalDownloadSpeed += tm.Monitor.DownloadSpeed;
            }
            return totalDownloadSpeed / 1024;
        }

        public static double GetCurrentProgress()
        {
            double totalProgress = 0.0;
            foreach (TorrentManager m in torrents)
            {
                totalProgress += m.Progress;
            }
            return totalProgress / torrents.Count;

        }

        static TorrentUpdater()
        {
            basePath  = Environment.CurrentDirectory;
            dhtNodeFile = Path.Combine(basePath, "DhtNodes");
            downloadsPath = CalculatedGameSettings.Current.Arma2OAPath;
            torrentsPath = Path.Combine(basePath, "Torrents");
            fastResumeFile = Path.Combine(torrentsPath, "fastresume.data");
        }

        public TorrentUpdater(string torrentLink, DayZInstaller installer, DayZUpdater updater)
        {
            this.installer = installer;
            this.updater = updater;
            torrents = new List<TorrentManager>();							// This is where we will store the torrentmanagers

            if (!Directory.Exists(torrentsPath))
                Directory.CreateDirectory(torrentsPath);
            else
            {
                Directory.Delete(torrentsPath, true); // Delete old torrents
                Directory.CreateDirectory(torrentsPath);
            }

            ExtendedWebClient wc = new ExtendedWebClient(new Uri(torrentLink));
            try
            {
                var torrentLinks = torrentLink.Split(';');
                int i = 1;
                foreach (string torrent in torrentLinks)
                {
                    wc.DownloadFile(torrent, Path.Combine(torrentsPath, "DayZero-" + updater.LatestVersion + "-" + i + ".torrent"));
                    i++;
                }
            }
            catch (Exception)
            {
                updater.Status = "Could not download torrent.";
                CalculatedGameSettings.Current.Update();
                installer.Status = "";
                installer.IsRunning = false;
                return;
            }

            // We need to cleanup correctly when the user closes the window by using ctrl-c
            // or an unhandled exception happens
            Console.CancelKeyPress += delegate { shutdown(); };
            AppDomain.CurrentDomain.ProcessExit += delegate { shutdown(); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Console.WriteLine(e.ExceptionObject); shutdown(); };
            Thread.GetDomain().UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Console.WriteLine(e.ExceptionObject); shutdown(); };
        }

        public void RemoveReadOnly(Torrent torrent)
        {
            try
            {
                var files = Directory.GetFiles(Path.Combine(downloadsPath, torrent.Name), "*.*", SearchOption.AllDirectories);
                foreach (string torrentFile in files)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(torrentFile);
                        fileInfo.IsReadOnly = false;
                        fileInfo.Refresh();
                    }
                    catch
                    {
                        // File prob. dont exist. ReadOnly not a problem in that case.
                    }
                }
            } catch
            {
                // Directory Problem.. Doesnt exist. Not a problem in that case.
            }
        }

        public TorrentUpdater(string torrentLink)
        {
            torrents = new List<TorrentManager>();							// This is where we will store the torrentmanagers
            // If the torrentsPath does not exist, we want to create it
            if (!Directory.Exists(torrentsPath))
                Directory.CreateDirectory(torrentsPath);
            else
            {
                Directory.Delete(torrentsPath, true); // Delete old torrents
                Directory.CreateDirectory(torrentsPath);
            }

            ExtendedWebClient wc = new ExtendedWebClient(new Uri(torrentLink));
            try
            {
                var torrentLinks = torrentLink.Split(';');
                int i = 1;
                foreach (string torrent in torrentLinks)
                {
                    wc.DownloadFile(torrent, Path.Combine(torrentsPath, "DayZero-" + i + ".torrent"));
                    i++;
                }
            }
            catch (Exception)
            {
                return;
            }


            // We need to cleanup correctly when the user closes the window by using ctrl-c
            // or an unhandled exception happens
            Console.CancelKeyPress += delegate { shutdown(); };
            AppDomain.CurrentDomain.ProcessExit += delegate { shutdown(); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Console.WriteLine(e.ExceptionObject); shutdown(); };
            Thread.GetDomain().UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Console.WriteLine(e.ExceptionObject); shutdown(); };
        }

        public void StartTorrents(int maxUpload)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() => StartEngine(maxUpload));
        }


        private void StartEngine(int maxUpload)
        {
            int port = 54321;
            Torrent torrent = null;

            // Create the settings which the engine will use
            // downloadsPath - this is the path where we will save all the files to
            // port - this is the port we listen for connections on
            EngineSettings engineSettings = new EngineSettings(downloadsPath, port);
            engineSettings.PreferEncryption = true;
            engineSettings.AllowedEncryption = EncryptionTypes.RC4Full | EncryptionTypes.RC4Header;

            // Create the default settings which a torrent will have.
            // 4 Upload slots - a good ratio is one slot per 5kB of upload speed
            // 50 open connections - should never really need to be changed
            // Unlimited download speed - valid range from 0 -> int.Max
            // Unlimited upload speed - valid range from 0 -> int.Max
            TorrentSettings torrentDefaults = new TorrentSettings(100, 150, 0, maxUpload);

            // Create an instance of the engine.
            engine = new ClientEngine(engineSettings);
            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port));
            byte[] nodes = null;
            try
            {
                nodes = File.ReadAllBytes(dhtNodeFile);
            }
            catch
            {
                Console.WriteLine("No existing dht nodes could be loaded");
            }

            DhtListener dhtListner = new DhtListener(new IPEndPoint(IPAddress.Any, port));
            DhtEngine dht = new DhtEngine(dhtListner);
            engine.RegisterDht(dht);
            dhtListner.Start();
            engine.DhtEngine.Start(nodes);

            // If the SavePath does not exist, we want to create it.
            if (!Directory.Exists(engine.Settings.SavePath))
                Directory.CreateDirectory(engine.Settings.SavePath);

            // If the torrentsPath does not exist, we want to create it
            if (!Directory.Exists(torrentsPath))
                Directory.CreateDirectory(torrentsPath);

            BEncodedDictionary fastResume;
            try
            {
                fastResume = BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(fastResumeFile));
            }
            catch
            {
                fastResume = new BEncodedDictionary();
            }

            // For each file in the torrents path that is a .torrent file, load it into the engine.
            foreach (string file in Directory.GetFiles(torrentsPath))
            {
                if (file.EndsWith(".torrent"))
                {
                    try
                    {
                        // Load the .torrent from the file into a Torrent instance
                        // You can use this to do preprocessing should you need to
                        torrent = Torrent.Load(file);
                        RemoveReadOnly(torrent);
                    }
                    catch (Exception e)
                    {
                        Console.Write("Couldn't decode {0}: ", file);
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    // When any preprocessing has been completed, you create a TorrentManager
                    // which you then register with the engine.
                    TorrentManager manager = new TorrentManager(torrent, downloadsPath, torrentDefaults);
                    //if (fastResume.ContainsKey(torrent.InfoHash.ToHex()))
                    //    manager.LoadFastResume(new FastResume((BEncodedDictionary)fastResume[torrent.InfoHash.ToHex()]));
                    engine.Register(manager);

                    // Store the torrent manager in our list so we can access it later
                    torrents.Add(manager);
                    manager.PeersFound += new EventHandler<PeersAddedEventArgs>(manager_PeersFound);
                }
            }

            // If we loaded no torrents, just exist. The user can put files in the torrents directory and start
            // the client again
            if (torrents.Count == 0)
            {
                Console.WriteLine("No torrents found in the Torrents directory");
                Console.WriteLine("Exiting...");
                engine.Dispose();
                return;
            }

            // For each torrent manager we loaded and stored in our list, hook into the events
            // in the torrent manager and start the engine.
            foreach (TorrentManager manager in torrents)
            {
                // Every time a piece is hashed, this is fired.
                manager.PieceHashed += delegate(object o, PieceHashedEventArgs e)
                {
                };

                // Every time the state changes (Stopped -> Seeding -> Downloading -> Hashing) this is fired
                manager.TorrentStateChanged += OnTorrentStateChanged;

                // Every time the tracker's state changes, this is fired
                foreach (TrackerTier tier in manager.TrackerManager)
                {
                }
                // Start the torrentmanager. The file will then hash (if required) and begin downloading/seeding
                manager.Start();
            }

            // While the torrents are still running, print out some stats to the screen.
            // Details for all the loaded torrent managers are shown.
            int i = 0;
            bool running = true;
            StringBuilder sb = new StringBuilder(1024);
            DateTime lastAnnounce = DateTime.Now;
            bool firstRun = true;
            while (running)
            {
                if (firstRun || lastAnnounce < DateTime.Now.AddMinutes(-1))
                {
                    foreach (TorrentManager tm in torrents)
                    {
                        tm.TrackerManager.Announce();
                    }
                    lastAnnounce = DateTime.Now;
                    firstRun = false;
                }
                if ((i++) % 10 == 0)
                {
                    sb.Remove(0, sb.Length);
                    running = torrents.Exists(delegate(TorrentManager m) { return m.State != TorrentState.Stopped; });

                    TorrentState totalState = torrents.Exists(m => m.State == TorrentState.Hashing) ? TorrentState.Hashing : torrents.Exists(m => m.State == TorrentState.Downloading) ? TorrentState.Downloading : TorrentState.Seeding;

                    string status = "";
                    switch (totalState)
                    {
                        case TorrentState.Hashing:
                            double totalHashProgress = 0;
                            foreach (TorrentManager m in torrents)
                            {
                                totalHashProgress += (m.State == TorrentState.Hashing) ? m.Progress : 100;
                            }
                            totalHashProgress = totalHashProgress / torrents.Count;
                            status = String.Format("Checking files ({0:0.00}%)", totalHashProgress);
                            break;
                        case TorrentState.Seeding:
                            status = "";
                            break;
                        default:
                            double totalDownloadProgress = 0;
                            double totalDownloaded = 0;
                            double totalToDownload = 0;
                            double totalDownloadSpeed = 0;
                            long totalDownloadSize = 0;
                            foreach (TorrentManager m in torrents)
                                totalDownloadSize += m.Torrent.Size;

                            foreach (TorrentManager m in torrents)
                            {
                                totalDownloaded += m.Torrent.Size / 1024 * (m.Progress / 100);
                                totalToDownload += m.Torrent.Size / 1024;
                                totalDownloadSpeed += m.Monitor.DownloadSpeed;

                            }
                            totalDownloadProgress = (totalDownloaded / totalToDownload) * 100;
                            status = "Status: " + (torrents.Exists(m => m.State == TorrentState.Downloading && m.GetPeers().Count > 0) ? "Downloading" : "Finding peers");
                            status += "\n" + String.Format("Progress: {0:0.00}%", totalDownloadProgress);
                            status += "\n" + String.Format("D/L Speed: {0:0.00} kB/s", totalDownloadSpeed / 1024.0);
                            break;
                    }
                    if (installer != null)
                        installer.Status = status;

                    #region OLDPROGRESS
                    //foreach (TorrentManager manager in torrents)
                    //{
                    //    AppendSeperator(sb);
                    //    AppendFormat(sb, "State:           {0}", manager.State);
                    //    AppendFormat(sb, "Name:            {0}", manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name);
                    //    AppendFormat(sb, "Progress:           {0:0.00}", manager.Progress);
                    //    string status = "";
                    //    switch (manager.State)
                    //    {
                    //        case TorrentState.Hashing:
                    //            status = String.Format("Checking files ({0:0.00}%)", manager.Progress);
                    //            break;
                    //        case TorrentState.Seeding: status = ""; break;
                    //        default: 
                    //            status = "Status: " + (manager.GetPeers().Count == 0 ? "Finding Peers" : "Downloading");
                    //            status += "\n" + String.Format("Progress: {0:0.00}%", manager.Progress);
                    //            status += "\n" + String.Format("D/L Speed: {0:0.00} kB/s", manager.Monitor.DownloadSpeed / 1024.0);
                    //            break;
                    //    }
                    //    if (installer != null)
                    //        installer.Status = status;
                    //    AppendFormat(sb, "Download Speed:     {0:0.00} kB/s", manager.Monitor.DownloadSpeed / 1024.0);
                    //    AppendFormat(sb, "Upload Speed:       {0:0.00} kB/s", manager.Monitor.UploadSpeed / 1024.0);
                    //    AppendFormat(sb, "Total Downloaded:   {0:0.00} MB", manager.Monitor.DataBytesDownloaded / (1024.0 * 1024.0));
                    //    AppendFormat(sb, "Total Uploaded:     {0:0.00} MB", manager.Monitor.DataBytesUploaded / (1024.0 * 1024.0));
                    //    MonoTorrent.Client.Tracker.Tracker tracker = manager.TrackerManager.CurrentTracker;
                    //    AppendFormat(sb, "Tracker Status:     {0}", tracker == null ? "<no tracker>" : tracker.Status.ToString());
                    //    AppendFormat(sb, "Warning Message:    {0}", tracker == null ? "<no tracker>" : tracker.WarningMessage);
                    //    AppendFormat(sb, "Failure Message:    {0}", tracker == null ? "<no tracker>" : tracker.FailureMessage);
                    //}
                    ////Console.Clear();
                    //Console.WriteLine(sb.ToString());
                    #endregion
                }

                System.Threading.Thread.Sleep(500);
            }
        }

        private void StopTorrents()
        {
            foreach (TorrentManager manager in torrents)
            {
                manager.Stop();
            }
        }

        public void OnTorrentStateChanged(object sender, TorrentStateChangedEventArgs e)
        {

            if (e.TorrentManager.Progress == 100.0 && e.NewState == TorrentState.Seeding)
            {
                bool lastFile = !torrents.Exists(delegate(TorrentManager m) { return m.Progress != 100; });
                if (lastFile)
                {
                    if (installer != null)
                        installer.Status = "Installing..";
                    if (updater != null)
                        updater.Status = DayZeroLauncherUpdater.STATUS_UPTODATE;
                }

                foreach (string f in Directory.GetFiles(Path.Combine(downloadsPath, e.TorrentManager.Torrent.Name), "*.*", SearchOption.AllDirectories))
                {
                    if (e.TorrentManager.Torrent.Files.None(t => t.FullPath.ToLower() == f.ToLower()))
                    {
                        File.Delete(f);
                    }
                }
                if (lastFile)
                {
                    if (installer != null)
                    {
                        installer.Status = "Ready to Play";
                    }
                    StopTorrents();

                    CalculatedGameSettings.Current.Update();
                }
            }
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static void AppendSeperator(StringBuilder sb)
        {
            AppendFormat(sb, "", null);
            AppendFormat(sb, "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - -", null);
            AppendFormat(sb, "", null);
        }

        private static void AppendFormat(StringBuilder sb, string str, params object[] formatting)
        {
            if (formatting != null)
                sb.AppendFormat(str, formatting);
            else
                sb.Append(str);
            sb.AppendLine();
        }

        static void manager_PeersFound(object sender, PeersAddedEventArgs e)
        {
        }

        private static void shutdown()
        {
            BEncodedDictionary fastResume = new BEncodedDictionary();
            for (int i = 0; i < torrents.Count; i++)
            {
                torrents[i].Stop(); ;
                while (torrents[i].State != TorrentState.Stopped)
                {
                    Console.WriteLine("{0} is {1}", torrents[i].Torrent.Name, torrents[i].State);
                    Thread.Sleep(250);
                }

                fastResume.Add(torrents[i].Torrent.InfoHash.ToHex(), torrents[i].SaveFastResume().Encode());
            }

            File.WriteAllBytes(dhtNodeFile, engine.DhtEngine.SaveNodes());
            File.WriteAllBytes(fastResumeFile, fastResume.Encode());
            engine.Dispose();

            foreach (TraceListener lst in Debug.Listeners)
            {
                lst.Flush();
                lst.Close();
            }

            System.Threading.Thread.Sleep(2000);
        }


    }
}

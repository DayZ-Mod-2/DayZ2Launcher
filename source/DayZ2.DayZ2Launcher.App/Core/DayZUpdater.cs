using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;

namespace DayZ2.DayZ2Launcher.App.Core
{
    public class DayZUpdater : BindableBase
    {
        private bool _isChecking;
        private HashWebClient.RemoteFileInfo _lastModsJsonLoc;
        private ModsMeta.ModInfo _latestModVersion;
        private string _status;

        public DayZUpdater(GameLauncher gameLauncher)
        {
            Launcher = gameLauncher;
            Downloader = new TorrentLauncher(Launcher);
            Downloader.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IsRunning")
                {
                    PropertyHasChanged("InstallButtonVisible");
                }
                else if (args.PropertyName == "Status")
                {
                    if (Downloader.Status == DayZLauncherUpdater.STATUS_INSTALLCOMPLETE)
                    {
                        CheckForUpdates(_lastModsJsonLoc);
                    }
                }
            };

            Launcher.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IsLaunching")
                {
                    Execute.OnUiThread(() => PropertyHasChanged("InstallButtonVisible", "VerifyButtonVisible", "ClearButtonVisible"));
                }
            };
        }

        public TorrentLauncher Downloader { get; }

        private GameLauncher Launcher { get; }

        public bool VersionMismatch
        {
            get
            {
                if (CalculatedGameSettings.Current.ModContentVersion == null)
                    return true;
                if (LatestVersion == null)
                    return false;

                return !CalculatedGameSettings.Current.ModContentVersion.Equals(LatestVersion, StringComparison.OrdinalIgnoreCase);
            }
        }

        public string LatestVersion
        {
            get
            {
                if (_latestModVersion != null)
                    return _latestModVersion.Version;

                return null;
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                Execute.OnUiThread(
                    () => PropertyHasChanged("Status", "VersionMismatch", "InstallButtonVisible", "VerifyButtonVisible", "ClearButtonVisible"));
            }
        }

        public bool InstallButtonVisible => VersionMismatch && !_isChecking && !Downloader.RunningForVersion(LatestVersion) && !Launcher.IsLaunching;

        public bool VerifyButtonVisible => !VersionMismatch && !Downloader.IsRunning && !Launcher.IsLaunching;

        public bool ClearButtonVisible => !Unpacker.IsClear() && !Launcher.IsLaunching;

        public void ClearModDir()
        {
            Unpacker.Clear();
            Execute.OnUiThread(() => PropertyHasChanged("ClearButtonVisible"));
        }

        public void CheckForUpdates(HashWebClient.RemoteFileInfo mods)
        {
            if (_isChecking)
                return;

            _lastModsJsonLoc = mods;
            _isChecking = true;

            new Thread(() =>
            {
                try
                {
                    string modsFileName = ModsMeta.GetFileName();
                    ModsMeta modsInfo = null;

                    HashWebClient.DownloadWithStatusDots(mods, modsFileName, DayZLauncherUpdater.STATUS_CHECKINGFORUPDATES,
                        newStatus => { Status = newStatus; },
                        (wc, fileInfo, destPath) => { modsInfo = ModsMeta.LoadFromFile(modsFileName); });

                    if (modsInfo != null)
                    {
                        Status = DayZLauncherUpdater.STATUS_CHECKINGFORUPDATES;
                        Thread.Sleep(100);

                        try
                        {
                            ModsMeta.ModInfo theMod =
                                modsInfo.Mods.Single(x => x.Version.Equals(modsInfo.LatestModVersion, StringComparison.OrdinalIgnoreCase));
                            SetLatestModVersion(theMod);

                            string currVersion = CalculatedGameSettings.Current.ModContentVersion;
                            if (!theMod.Version.Equals(currVersion, StringComparison.OrdinalIgnoreCase))
                            {
                                Status = DayZLauncherUpdater.STATUS_OUTOFDATE;

                                //this lets them seed/repair version they already have if it's not discontinued
                                ModsMeta.ModInfo currMod =
                                    modsInfo.Mods.SingleOrDefault(x => x.Version.Equals(currVersion, StringComparison.OrdinalIgnoreCase));
                                if (currMod != null)
                                    DownloadSpecificVersion(currMod, false);
                                else //try getting it from file cache (necessary for switching branches)
                                    DownloadLocalVersion(currVersion, false);
                            }
                            else
                            {
                                Status = DayZLauncherUpdater.STATUS_UPTODATE;
                                DownloadLatestVersion(false);
                            }
                        }
                        catch (Exception)
                        {
                            Status = "Could not determine revision";
                        }
                    }
                }
                finally
                {
                    _isChecking = false;
                }
            }).Start();
        }

        private void SetLatestModVersion(ModsMeta.ModInfo newModInfo)
        {
            _latestModVersion = newModInfo;
            Execute.OnUiThread(
                () => PropertyHasChanged("LatestVersion", "VersionMismatch", "InstallButtonVisible", "VerifyButtonVisible", "ClearButtonVisible"));
        }

        public void DownloadLatestVersion(bool forceFullSystemsCheck)
        {
            if (LatestVersion == null)
            {
                MessageBox.Show("Please check for new versions first", "Unable to determine latest version", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Downloader.StartFromNetContent(_latestModVersion.Version, forceFullSystemsCheck, _latestModVersion.Archive, this,
                false);
        }

        public void DownloadSpecificVersion(ModsMeta.ModInfo modInfo, bool forceFullSystemsCheck)
        {
            Downloader.StartFromNetContent(modInfo.Version, forceFullSystemsCheck, modInfo.Archive, this, true);
        }

        public void DownloadLocalVersion(string versionString, bool forceFullSystemsCheck)
        {
            Downloader.StartFromContentFile(versionString, forceFullSystemsCheck, this);
        }

        public class ModsMeta
        {
            [JsonProperty("latest")] public string LatestModVersion = null;
            [JsonProperty("mods")] public List<ModInfo> Mods = null;

            public static string GetFileName()
            {
                string modsFileName = Path.Combine(UserSettings.ContentMetaPath, "index.json");
                return modsFileName;
            }

            public static ModsMeta LoadFromFile(string fileFullPath)
            {
                var modsInfo = JsonConvert.DeserializeObject<ModsMeta>(File.ReadAllText(fileFullPath));
                return modsInfo;
            }

            public class ModInfo
            {
                [JsonProperty("archive")] public HashWebClient.RemoteFileInfo Archive = null;
                [JsonProperty("version")] public string Version = null;
            }
        }
    }
}
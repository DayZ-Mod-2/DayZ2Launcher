using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using DayZ2.DayZ2Launcher.App.Ui;

namespace DayZ2.DayZ2Launcher.App.Core
{
    public class Arma2Updater : BindableBase
    {
        private Arma2Installer _installer;
        private bool _isChecking;
        private HashWebClient.RemoteFileInfo _lastPatchesJsonLoc;
        private PatchesMeta.PatchInfo _latestServerVersion;
        private string _status;

        public Arma2Updater()
        {
            Installer = new Arma2Installer();
            Installer.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IsRunning")
                {
                    PropertyHasChanged("InstallButtonVisible");
                }
                else if (args.PropertyName == "Status")
                {
                    if (Installer.Status == DayZLauncherUpdater.STATUS_INSTALLCOMPLETE)
                    {
                        CheckForUpdates(_lastPatchesJsonLoc);
                    }
                }
            };
        }

        public Arma2Installer Installer
        {
            get { return _installer; }
            set
            {
                _installer = value;
                PropertyHasChanged("Installer");
            }
        }

        public int? LatestVersion
        {
            get
            {
                if (_latestServerVersion != null)
                    return _latestServerVersion.Version;

                return null;
            }
        }

        public bool VersionMismatch
        {
            get
            {
                bool mismatch = true;

                GameVersions versions = CalculatedGameSettings.Current.Versions;
                if (versions != null)
                {
                    if ((versions.Retail.BuildNo ?? 0) >= (LatestVersion ?? 0))
                        mismatch = false;
                    else if ((versions.Beta.BuildNo ?? 0) == (LatestVersion ?? 0))
                        mismatch = false;
                }

                return mismatch;
            }
        }

        public bool InstallButtonVisible
        {
            get { return VersionMismatch && !_isChecking && !Installer.IsRunning; }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                Execute.OnUiThread(() => PropertyHasChanged("Status", "VersionMismatch", "InstallButtonVisible"));
            }
        }

        public void CheckForUpdates(HashWebClient.RemoteFileInfo patches)
        {
            if (_isChecking)
                return;

            _lastPatchesJsonLoc = patches;
            _isChecking = true;

            Status = DayZLauncherUpdater.STATUS_CHECKINGFORUPDATES;
            new Thread(() =>
            {
                try
                {
                    string patchesFileName = PatchesMeta.GetFileName();
                    PatchesMeta patchInfo = null;

                    HashWebClient.DownloadWithStatusDots(patches, patchesFileName, DayZLauncherUpdater.STATUS_CHECKINGFORUPDATES,
                        newStatus => { Status = newStatus; },
                        (wc, fileInfo, destPath) => { patchInfo = PatchesMeta.LoadFromFile(patchesFileName); });

                    if (patchInfo != null)
                    {
                        Status = DayZLauncherUpdater.STATUS_CHECKINGFORUPDATES;
                        Thread.Sleep(100);

                        try
                        {
                            PatchesMeta.PatchInfo thePatch = patchInfo.Updates.Where(x => x.Version == patchInfo.LatestRevision).Single();
                            SetLatestServerVersion(thePatch);

                            Status = VersionMismatch ? DayZLauncherUpdater.STATUS_OUTOFDATE : DayZLauncherUpdater.STATUS_UPTODATE;
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

        public void InstallLatestVersion(UpdatesView view)
        {
            Installer.DownloadAndInstall(_latestServerVersion.Version, _latestServerVersion.SteamBeta, _latestServerVersion.SteamBuild, view);
        }

        private void SetLatestServerVersion(PatchesMeta.PatchInfo newPatchInfo)
        {
            _latestServerVersion = newPatchInfo;
            Execute.OnUiThread(() => PropertyHasChanged("LatestVersion", "VersionMismatch", "InstallButtonVisible"));
        }

        private class PatchesMeta
        {
            [JsonProperty("patches")] public readonly List<PatchInfo> Updates = null;
            [JsonProperty("latest")] public int LatestRevision = 0;

            public static string GetFileName()
            {
                string patchesFileName = Path.Combine(UserSettings.PatchesPath, "index.json");
                return patchesFileName;
            }

            public static PatchesMeta LoadFromFile(string fullFilePath)
            {
                var patchInfo = JsonConvert.DeserializeObject<PatchesMeta>(File.ReadAllText(fullFilePath));
                return patchInfo;
            }

            public class PatchInfo
            {
                [JsonProperty("steambeta")] public bool SteamBeta = false;
                [JsonProperty("steambuild")] public string SteamBuild = "";
                [JsonProperty("version")] public int Version = 0;
            }
        }
    }
}
using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class DayZUpdater : BindableBase
	{
		private Version _latestVersion;
		private bool _isChecking;
		private string _status;
        public const string dayZeroTorrentFileUrl = "http://www.zombies.nu/dayzerotorrent.txt";
        public const string dayZeroVersionUrl = "http://www.zombies.nu/dayzeroversion.txt";
        public const string deletedFilesUrl = "http://www.zombies.nu/oldfiles.txt";

		public DayZUpdater()
		{
			Installer = new DayZInstaller();
			Installer.PropertyChanged += (sender, args) =>
			                             	{
												if(args.PropertyName == "IsRunning")
												{
													PropertyHasChanged("InstallButtonVisible");
												}
												else if(args.PropertyName == "Status")
												{
													if(Installer.Status == "Install complete")
													{
														CheckForUpdates();
													}
												}
			                             	};
            string responseBody;
            if (!GameUpdater.HttpGet(dayZeroVersionUrl, out responseBody))
            {
                Status = "Zombies.nu not responding";
                return;
            }
            Version version;
            if (Version.TryParse(responseBody, out version))
            {
                if (version.Equals(CalculatedGameSettings.Current.DayZVersion)) // If version is up to date. Seed.
                {
                    string torrentUrl;
                    if (!GameUpdater.HttpGet(dayZeroTorrentFileUrl, out torrentUrl))
                    {
                        Status = "Zombies.nu not responding";
                        return;
                    }
                    TorrentUpdater seeder = new TorrentUpdater(responseBody, torrentUrl); // Sets up launcher to start seeding current build.
                    seeder.StartTorrents(1);
                }
            }

		}

		public DayZInstaller Installer { get; set; }

		public bool VersionMismatch
		{
			get
			{
				if(CalculatedGameSettings.Current.DayZVersion == null)
					return true;
				if(LatestVersion == null)
					return false;

                return !CalculatedGameSettings.Current.DayZVersion.Equals(LatestVersion);
			}
		}
	
		public void CheckForUpdates()
		{
			if(_isChecking)
				return;

			_isChecking = true;

			Status = DayZeroLauncherUpdater.STATUS_CHECKINGFORUPDATES;

			string responseBody;
			Version latestVersion = null;

			new Thread(() =>
			           	{
			           		try
			           		{
								Thread.Sleep(750);  //In case this happens so fast the UI looks like it didn't work
                                if (!GameUpdater.HttpGet(dayZeroVersionUrl, out responseBody))
			           			{
			           				Status = "Zombies.nu not responding";
			           				return;
			           			}
			           			Version version;
			           			if (Version.TryParse(responseBody, out version))
			           			{
			           				latestVersion = version;
                                    if (!latestVersion.Equals(CalculatedGameSettings.Current.DayZVersion))
			           				{
			           					Status = DayZeroLauncherUpdater.STATUS_OUTOFDATE;
			           				}
			           				else
			           				{
			           					Status = DayZeroLauncherUpdater.STATUS_UPTODATE;
			           				}
			           			}
			           			else
			           			{
			           				Status = "Could not determine version from filenames";

			           			}
			           		}
			       			catch(Exception)
							{
								Status = "Error getting version";
							}
							finally
							{
								_isChecking = false;
								LatestVersion = latestVersion;
							}
						}).Start();
		}

		public Version LatestVersion
		{
			get { return _latestVersion; }
			set
			{
				_latestVersion = value;
				Execute.OnUiThread(() => PropertyHasChanged("LatestVersion", "VersionMismatch", "InstallButtonVisible"));			
			}
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

		public bool InstallButtonVisible
		{
			get { return VersionMismatch && !_isChecking && !Installer.IsRunning; }
		}

		public void InstallLatestVersion()
		{
            string responseBody;
            if (!GameUpdater.HttpGet(dayZeroTorrentFileUrl, out responseBody))
            {
                Status = "Zombies.nu not responding";
                return;
            }

            Installer.DownloadAndInstall(responseBody, deletedFilesUrl, this);
		}
	}
}
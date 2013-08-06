using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class Arma2Updater : BindableBase
	{
		private string _latestDownloadUrl;
		private int? _latestVersion;
		private bool _isChecking;
		private string _status;
        public const string ArmaBetaListingUrl = "http://www.zombies.nu/armaversion.txt";

		public Arma2Updater()
		{
			Installer = new Arma2Installer();
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
		}

		public bool VersionMismatch
		{
			get
			{
				if(CalculatedGameSettings.Current.Arma2OABetaVersion == null)
					return true;
				if(LatestVersion == null)
					return false;
				
				return CalculatedGameSettings.Current.Arma2OABetaVersion.Revision != LatestVersion;
			}
		}

		public bool InstallButtonVisible
		{
			get { return VersionMismatch && !_isChecking && !Installer.IsRunning; }
		}

		public void CheckForUpdates()
		{
			if(_isChecking)
				return;

			_isChecking = true;

			Status = DayZeroLauncherUpdater.STATUS_CHECKINGFORUPDATES;

			string responseBody;
            int latestRevision = 0;

			new Thread(() =>
			           	{
							try
							{
								Thread.Sleep(750);  //In case this happens so fast the UI looks like it didn't work
								if(!GameUpdater.HttpGet(ArmaBetaListingUrl, out responseBody))
								{
									Status = "Zombies.nu not responding";
									return;
								}
                                _latestDownloadUrl = String.Format("http://www.arma2.com/downloads/update/beta/ARMA2_OA_Build_{0}.zip", responseBody);
                                Int32.TryParse(responseBody, out latestRevision);
								if(latestRevision != 0)
								{
									if(LocalMachineInfo.Current.Arma2OABetaVersion == null || LocalMachineInfo.Current.Arma2OABetaVersion.Revision != latestRevision)
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
									Status = "Coult not determine revision";
								}
				
							}
							catch(Exception)
							{
								Status = "Error getting version";
							}
							finally
							{
								_isChecking = false;
								LatestVersion = (int?) latestRevision;
							}
						}).Start();
		}

		public void InstallLatestVersion()
		{
			Installer.DownloadAndInstall(_latestDownloadUrl);
		}

		private Arma2Installer _installer;
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
	}
}
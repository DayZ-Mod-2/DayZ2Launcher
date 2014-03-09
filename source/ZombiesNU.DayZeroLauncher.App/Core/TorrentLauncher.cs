using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using zombiesnu.DayZeroLauncher.App.Ui;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class MetaAddon
	{
		[JsonProperty("addon")]
		public string Name;

		[JsonProperty("torrent")]
		public HashWebClient.RemoteFileInfo Torrent;

		[JsonProperty("name")]
		public string Description;

		[JsonProperty("version")]
		public string Version;

		[JsonProperty("installer")]
		public string InstallerName;
	}

	public class TorrentLauncher : BindableBase
	{
		private GameLauncher _gameLauncher = null;
		private TorrentUpdater _torrentUpdater = null;

		public TorrentLauncher(GameLauncher launcher)
		{
			_gameLauncher = launcher;
		}

		public void DownloadAndInstall(string versionString, bool forceFullSystemsCheck, HashWebClient.RemoteFileInfo jsonIndex, DayZUpdater updater)
		{
			if (jsonIndex == null)
			{
				MessageBox.Show("No version index specified, please Check first.", "Error initiating torrent download", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (string.IsNullOrWhiteSpace(versionString))
			{
				MessageBox.Show("Invalid version specified for download", "Error initiating torrent download", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (IsRunning)
				return;

			IsRunning = true;
            updater.Status = DayZeroLauncherUpdater.STATUS_DOWNLOADING;
			Status = "Initiating Download...";

			string metaJsonFilename = MetaModDetails.GetFileName(versionString);

			var wc = new HashWebClient();
			wc.DownloadFileCompleted += (sender, args) =>
				{
					if (args.Cancelled)
					{
						Status = updater.Status = "Async operation cancelled";
						IsRunning = false;
						_gameLauncher.SetModDetails(null,true,null);
					}
					else if (args.Error != null)
					{
						updater.Status = "Error downloading content index file";
						Status = args.Error.Message;
						IsRunning = false;
						_gameLauncher.SetModDetails(null, false, args.Error);
					}
					else
					{
						MetaModDetails modDetails = null;
						bool fullSystemCheck = true;
						try
						{
							modDetails = MetaModDetails.LoadFromFile(metaJsonFilename);
							if (!forceFullSystemsCheck)
							{
								CalculatedGameSettings.Current.Update();
								if (versionString.Equals(CalculatedGameSettings.Current.ModContentVersion, StringComparison.OrdinalIgnoreCase))
									fullSystemCheck = false;
							}
						}
						catch (Exception ex)
						{
							updater.Status = "Error parsing content index file";
							Status = ex.Message;
							IsRunning = false;
							_gameLauncher.SetModDetails(null, false, ex);
						}

						if (modDetails != null)
						{
							_gameLauncher.SetModDetails(modDetails);
							_torrentUpdater = new TorrentUpdater(versionString, modDetails.AddOns, fullSystemCheck, this, updater); //this automatically starts it's async thread
						}
					}
				};
			wc.BeginDownload(jsonIndex, metaJsonFilename);
		}

		private bool _isRunning;
        public bool IsRunning
		{
			get { return _isRunning; }
			set
			{
				_isRunning = value;
				PropertyHasChanged("IsRunning");
			}
		}

		private string _status;
		public string Status
		{
			get { return _status; }
			set
			{
				_status = value;
				Execute.OnUiThread(() => PropertyHasChanged("Status"));
			}
		}
	}
}
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class Arma2Updater : BindableBase
	{
		private bool _isChecking;
		private HashWebClient.RemoteFileInfo _lastPatchesJsonLoc;

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
													if (Installer.Status == DayZeroLauncherUpdater.STATUS_INSTALLCOMPLETE)
													{
														CheckForUpdates(_lastPatchesJsonLoc);
													}
												}
			                             	};
		}

		private class PatchesMeta
		{
			public class PatchInfo
			{
				[JsonProperty("version")]
				public int Version = 0;

				[JsonProperty("archive")]
				public HashWebClient.RemoteFileInfo Archive = null;
			}

			[JsonProperty("patches")]
			public List<PatchInfo> Updates = null;
			[JsonProperty("latest")]
			public int LatestRevision = 0;

			static public string GetFileName()
			{
				string patchesFileName = Path.Combine(UserSettings.PatchesPath, "index.json");
				return patchesFileName;
			}

			static public PatchesMeta LoadFromFile(string fullFilePath)
			{
				PatchesMeta patchInfo = JsonConvert.DeserializeObject<PatchesMeta>(File.ReadAllText(fullFilePath));
				return patchInfo;
			}
		}

		public void CheckForUpdates(HashWebClient.RemoteFileInfo patches)
		{
			if(_isChecking)
				return;

			_lastPatchesJsonLoc = patches;
			_isChecking = true;

			Status = DayZeroLauncherUpdater.STATUS_CHECKINGFORUPDATES;
			new Thread(() =>
			{
				try
				{
					string patchesFileName = PatchesMeta.GetFileName();
					PatchesMeta patchInfo = null;					

					HashWebClient.DownloadWithStatusDots(patches, patchesFileName, DayZeroLauncherUpdater.STATUS_CHECKINGFORUPDATES,
						(newStatus) =>
							{
								Status = newStatus;
							},
						(wc, fileInfo, destPath) =>
							{
								patchInfo = PatchesMeta.LoadFromFile(patchesFileName);
							});

					if (patchInfo != null)
					{
						Status = DayZeroLauncherUpdater.STATUS_CHECKINGFORUPDATES;
						Thread.Sleep(100);

						try
						{
							PatchesMeta.PatchInfo thePatch = patchInfo.Updates.Where(x => x.Version == patchInfo.LatestRevision).Single();
							SetLatestServerVersion(thePatch);

							if (LocalMachineInfo.Current.Arma2OABetaVersion == null ||
								LocalMachineInfo.Current.Arma2OABetaVersion.Revision != thePatch.Version)
							{
								Status = DayZeroLauncherUpdater.STATUS_OUTOFDATE;
							}
							else
								Status = DayZeroLauncherUpdater.STATUS_UPTODATE;
						}
						catch (Exception) { Status = "Could not determine revision"; }
					}
				}
				finally { _isChecking = false; }
			}).Start();
		}

		public void InstallLatestVersion()
		{
			Installer.DownloadAndInstall(_latestServerVersion.Version,_latestServerVersion.Archive);
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

		PatchesMeta.PatchInfo _latestServerVersion = null;
		private void SetLatestServerVersion(PatchesMeta.PatchInfo newPatchInfo)
		{
			_latestServerVersion = newPatchInfo;
			Execute.OnUiThread(() => PropertyHasChanged("LatestVersion", "VersionMismatch", "InstallButtonVisible"));
		}
		public int? LatestVersion
		{
			get 
			{
				if (_latestServerVersion != null)
					return _latestServerVersion.Version;

				return new Nullable<int>();
			}
		}
		
		public bool VersionMismatch
		{
			get
			{
				if (CalculatedGameSettings.Current.Arma2OABetaVersion == null)
					return true;
				if (LatestVersion == null)
					return false;

				return CalculatedGameSettings.Current.Arma2OABetaVersion.Revision != LatestVersion;
			}
		}

		public bool InstallButtonVisible
		{
			get { return VersionMismatch && !_isChecking && !Installer.IsRunning; }
		}

		private string _status;
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
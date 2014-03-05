using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Windows;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class DayZeroLauncherUpdater : BindableBase
	{
		private string _status;
		private Version _latestVersion;
		public static readonly string STATUS_CHECKINGFORUPDATES = "Checking for updates...";
		public static readonly string STATUS_DOWNLOADING = "Downloading...";
		public static readonly string STATUS_UPTODATE = "Up to date";
		public static readonly string STATUS_OUTOFDATE = "Out of date";
		public static readonly string STATUS_UPDATEREQUIRED = "Update required!";
		public static readonly string STATUS_RESTARTREQUIRED = "Restart required";
		public static readonly string STATUS_EXTRACTING = "Extracting...";
		public static readonly string STATUS_INSTALLING = "Installing...";
		public static readonly string STATUS_INSTALLCOMPLETE = "Install complete";

		public Version LatestVersion
		{
			get { return _latestVersion; }
			set
			{
				_latestVersion = value;
				Execute.OnUiThread(() => PropertyHasChanged("LatestVersion"));				
			}
		}

		public string Status
		{
			get { return _status; }
			set
			{
				_status = value;
				Execute.OnUiThread(() =>
					{
						PropertyHasChanged("Status");
						PropertyHasChanged("UpdatePending");
						PropertyHasChanged("RestartPending");
					});				
			}
		}

		public bool UpdatePending
		{
			get { return (Status == STATUS_UPDATEREQUIRED || Status == STATUS_OUTOFDATE); }
		}

		public bool RestartPending
		{
			get { return (Status == STATUS_RESTARTREQUIRED); }
		}

		public bool VersionMismatch
		{
			get
			{
				if(LatestVersion == null)
					return false;
				
				return !LocalMachineInfo.Current.DayZeroLauncherVersion.Equals(LatestVersion);
			}
		}

		private bool isChecking = false;
		private bool isUpdating = false;
		private ApplicationDeployment deployment = null;

		private void VersionCheckComplete(object sender, CheckForUpdateCompletedEventArgs e)
		{
			deployment.CheckForUpdateCompleted -= VersionCheckComplete;
			isChecking = false;

			string errMsg = null;
			if (e.Cancelled)
				errMsg = "Update check cancelled";
			else if (e.Error != null)
				errMsg = e.Error.Message;

			if (errMsg != null)
			{
				Status = errMsg;
				LatestVersion = null;
				return;
			}

			if (e.UpdateAvailable)
			{
				LatestVersion = e.AvailableVersion;
				if (e.IsUpdateRequired)
					Status = STATUS_UPDATEREQUIRED;
				else
					Status = STATUS_OUTOFDATE;
			}
			else
			{
				LatestVersion = LocalMachineInfo.Current.DayZeroLauncherVersion;
				Status = STATUS_UPTODATE;
			}
		}

		public void CheckForUpdate()
		{
			if (RestartPending)
				return; //dont let them ruin it

			if (ApplicationDeployment.IsNetworkDeployed)
			{
				if (deployment != null)
				{
					if (isUpdating)
						deployment.UpdateAsyncCancel(); ;
					if (isChecking)
						deployment.CheckForUpdateAsyncCancel();

					deployment = null;
					isUpdating = false;
					isChecking = false;
				}

				deployment = ApplicationDeployment.CurrentDeployment;
				deployment.CheckForUpdateCompleted += VersionCheckComplete;
				Status = STATUS_CHECKINGFORUPDATES;
				isChecking = true;
				deployment.CheckForUpdateAsync();
			}
			else
			{
				deployment = null;
				Status = STATUS_UPTODATE;
				LatestVersion = null;
			}
		}

		private string GetProgressString(DeploymentProgressState state)
		{
			if (state == DeploymentProgressState.DownloadingApplicationFiles)
				return "files";
			else if (state == DeploymentProgressState.DownloadingApplicationInformation)
				return "metadata";
			else
				return "manifest";
		}

		private void UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
		{
			Status = String.Format("Downloading {0} ({1}%)...", GetProgressString(e.State), e.ProgressPercentage); 
		}

		private void UpdateCompleted(object sender, AsyncCompletedEventArgs e)
		{
			deployment.CheckForUpdateCompleted -= VersionCheckComplete;
			deployment.UpdateProgressChanged -= UpdateProgressChanged;
			isUpdating = false;

			string errMsg = null;
			if (e.Cancelled)
				errMsg = "Update install cancelled";
			else if (e.Error != null)
				errMsg = e.Error.Message;

			if (errMsg != null)
			{
				Status = errMsg;
				return;
			}

			Status = STATUS_RESTARTREQUIRED;
		}

		public void UpdateToLatest()
		{
			if (deployment == null)
				return;

			if (isChecking || isUpdating)
				return;

			deployment.UpdateCompleted += UpdateCompleted;
			deployment.UpdateProgressChanged += UpdateProgressChanged;
			isUpdating = true;
			deployment.UpdateAsync();
		}

		public void RestartNewVersion()
		{
			if (deployment != null)
			{
				System.Windows.Forms.Application.Restart();
				Application.Current.Shutdown();
			}
		}
	}
}
using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Windows.Forms;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class LauncherUpdater : BindableBase
	{
		public static readonly string STATUS_CHECKINGFORUPDATES = "Checking for updates...";
		public static readonly string STATUS_DOWNLOADING = "Downloading...";
		public static readonly string STATUS_UPTODATE = "Up to date";
		public static readonly string STATUS_OUTOFDATE = "Out of date";
		public static readonly string STATUS_UPDATEREQUIRED = "Update required!";
		public static readonly string STATUS_RESTARTREQUIRED = "Restart required";
		public static readonly string STATUS_EXTRACTING = "Extracting...";
		public static readonly string STATUS_INSTALLING = "Installing...";
		public static readonly string STATUS_INSTALLCOMPLETE = "Install complete";
		private Version _latestVersion;
		private string _status;
		private ApplicationDeployment deployment;
		private bool isChecking;
		private bool isUpdating;

		public Version LatestVersion
		{
			get => _latestVersion;
			set
			{
				_latestVersion = value;
				Execute.OnUiThread(() => PropertyHasChanged("LatestVersion"));
			}
		}

		public string Status
		{
			get => _status;
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

		public bool UpdatePending => (Status == STATUS_UPDATEREQUIRED || Status == STATUS_OUTOFDATE);

		public bool RestartPending => (Status == STATUS_RESTARTREQUIRED);

		public bool VersionMismatch
		{
			get
			{
				if (LatestVersion == null)
					return false;

				return !LocalMachineInfo.Current.DayZLauncherVersion.Equals(LatestVersion);
			}
		}

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
				LatestVersion = LocalMachineInfo.Current.DayZLauncherVersion;
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
						deployment.UpdateAsyncCancel();

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
			if (state == DeploymentProgressState.DownloadingApplicationInformation)
				return "metadata";
			return "manifest";
		}

		private void UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
		{
			Status = $"Downloading {GetProgressString(e.State)} ({e.ProgressPercentage}%)...";
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
				Application.Restart();
				System.Windows.Application.Current.Shutdown();
			}
		}
	}
}
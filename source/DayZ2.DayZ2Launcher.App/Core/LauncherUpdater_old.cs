using System;
using System.ComponentModel;
//using System.Deployment.Application;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class LauncherUpdater_old : BindableBase
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

		public UpdateStatus Status { get; private set; }
		public SemanticVersion CurrentVersion { get; private set; }
		public SemanticVersion LatestVersion { get; private set; }

		//private ApplicationDeployment m_deployment;
		private TaskCompletionSource<object> m_checkForUpdate;
		private bool isChecking;
		private bool isUpdating;

		public Task CheckForUpdateAsync(CancellationToken cancellationToken)
		{
			/*
			if (ApplicationDeployment.IsNetworkDeployed)
			{
				m_deployment = ApplicationDeployment.CurrentDeployment;
				if (m_checkForUpdate == null)
				{
					m_checkForUpdate = new TaskCompletionSource<object>();
					m_deployment.CheckForUpdateCompleted += CheckForUpdateCompleted;
					cancellationToken.Register(() =>
					{
						if (m_checkForUpdate != null)
						{
							m_deployment.CheckForUpdateAsyncCancel();
							m_checkForUpdate.SetCanceled(cancellationToken);
						}
					});
					m_deployment.CheckForUpdateAsync();
				}
				return m_checkForUpdate.Task;
			}
			*/

			// don't check if its not clickonce
			var task = new TaskCompletionSource<object>();
			string version = LocalMachineInfo.Current.LauncherVersion.ToString();
			Status = UpdateStatus.UpToDate;
			CurrentVersion = SemanticVersion.Parse(version);
			LatestVersion = CurrentVersion;
			task.SetResult(null);

			return task.Task;
		}

		/*
		void CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
		{
			Status = e.IsUpdateRequired ? UpdateStatus.OutOfDate : UpdateStatus.UpToDate;
			CurrentVersion = SemanticVersion.Parse(m_deployment.CurrentVersion.ToString());
			LatestVersion = SemanticVersion.Parse(e.AvailableVersion.ToString());
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
			// Status = $"Downloading {GetProgressString(e.State)} ({e.ProgressPercentage}%)...";
		}

		private void UpdateCompleted(object sender, AsyncCompletedEventArgs e)
		{
			m_deployment.CheckForUpdateCompleted -= CheckForUpdateCompleted;
			m_deployment.UpdateProgressChanged -= UpdateProgressChanged;
			isUpdating = false;

			string errMsg = null;
			if (e.Cancelled)
				errMsg = "Update install cancelled";
			else if (e.Error != null)
				errMsg = e.Error.Message;

			if (errMsg != null)
			{
				// Status = errMsg;
				return;
			}

			// Status = STATUS_RESTARTREQUIRED;
		}

		public void UpdateToLatest()
		{
			if (m_deployment == null)
				return;

			if (isChecking || isUpdating)
				return;

			m_deployment.UpdateCompleted += UpdateCompleted;
			m_deployment.UpdateProgressChanged += UpdateProgressChanged;
			isUpdating = true;
			m_deployment.UpdateAsync();
		}

		public void RestartNewVersion()
		{
			if (m_deployment != null)
			{
				Application.Restart();
				System.Windows.Application.Current.Shutdown();
			}
		}
		*/
	}
}

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Ionic.Zip;
using NLog;

namespace zombiesnu.DayZeroLauncher.Updater
{
	public class DownloadAndExtracter
	{
		private readonly Version _serverVersion;
		private readonly Uri _serverZipUri;
		public static readonly string PENDING_UPDATE_DIRECTORYNAME = "_pendingupdate";
		private readonly string _currentDirectory;

		public DownloadAndExtracter(Version serverVersion)
		{
			_serverVersion = serverVersion;
			//_serverZipUri = new Uri(String.Format("http://www.roflkopter.dk/Debug.zip", _serverVersion));
            _serverZipUri = new Uri("http://krasnostav.zombies.nu/archive/DayZeroLauncher.msi");
			//var uniqueToken = Guid.NewGuid().ToString();
            //_tempDownloadFileLocation = DownloadAndExtracter.GetTempPath() + uniqueToken + ".zip";
            //_tempExtractedLocation = DownloadAndExtracter.GetTempPath() + uniqueToken;
			_currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			//_targetSwapDirectory = Path.Combine(_currentDirectory, PENDING_UPDATE_DIRECTORYNAME);
		}

        //public static string GetTempPath()
        //{
        //    var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    var currentInfo = new DirectoryInfo(current);
        //    var tempPath = Path.Combine(currentInfo.Parent.FullName, @"Temp\");
        //    if(!Directory.Exists(tempPath))
        //        Directory.CreateDirectory(tempPath);
        //    return tempPath;
        //}


		public event EventHandler<ExtractCompletedArgs> ExtractComplete;

		public void DownloadAndExtract()
		{
			var pendingUpdateVersion = GetPendingUpdateVersion();
			if(pendingUpdateVersion != null && pendingUpdateVersion >= _serverVersion)
			{
                ExtractComplete(this, new ExtractCompletedArgs());
				return;
			}

			var checkForUpdateClient = new WebClient();
			checkForUpdateClient.DownloadFileCompleted += DownloadFileComplete;
            if (!Directory.Exists(Path.Combine(_currentDirectory, PENDING_UPDATE_DIRECTORYNAME)))
                Directory.CreateDirectory(Path.Combine(_currentDirectory, PENDING_UPDATE_DIRECTORYNAME));
            checkForUpdateClient.DownloadFileAsync(_serverZipUri, Path.Combine(Path.Combine(_currentDirectory, PENDING_UPDATE_DIRECTORYNAME), "DayZeroLauncher.msi"));
		}

		private Version GetPendingUpdateVersion()
		{
            var pendingUpdateDayZeroLauncherFile = new FileInfo(Path.Combine(Path.Combine(_currentDirectory, PENDING_UPDATE_DIRECTORYNAME), "DayZeroLauncher.msi"));
			if(!pendingUpdateDayZeroLauncherFile.Exists)
				return null;

			return AssemblyName.GetAssemblyName(pendingUpdateDayZeroLauncherFile.FullName).Version;
		}

		private void DownloadFileComplete(object sender, AsyncCompletedEventArgs args)
		{
			if(args.Error != null)
			{
				return;
			}
            ExtractComplete(this, new ExtractCompletedArgs());
		}
	}
}
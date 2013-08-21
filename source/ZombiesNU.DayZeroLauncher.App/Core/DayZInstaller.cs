using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using zombiesnu.DayZeroLauncher.App.Ui;
using SharpCompress.Common;
using SharpCompress.Reader;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class DayZInstaller : ViewModelBase
	{
		private string _latestDownloadUrl;
        private string _deletedFilesUrl;
		private string _status;
		private bool _isRunning;

		public void DownloadAndInstall(string latestDownloadUrl, string deletedFilesUrl, DayZUpdater updater)
		{
			_latestDownloadUrl = latestDownloadUrl;
            _deletedFilesUrl = deletedFilesUrl;

			if(string.IsNullOrEmpty(CalculatedGameSettings.Current.DayZPath)
			   || string.IsNullOrEmpty(_latestDownloadUrl))
			{
				return;
			}

			CalculatedGameSettings.Current.DayZPath.MakeSurePathExists();

			IsRunning = true;
            updater.Status = DayZeroLauncherUpdater.STATUS_DOWNLOADING;
			Status = "Initiating Download ..";

            System.Threading.Tasks.Task.Factory.StartNew(() => GetDayZFiles(updater));
		}

        private void GetDayZFiles(DayZUpdater updater)
        {
            TorrentUpdater tu = new TorrentUpdater(_latestDownloadUrl, this, updater);
            tu.StartTorrents(1000);
        }

#region OLD DOWNLOADER
        //private void GetDayZFiles()
        //{
        //    var files = new List<string>();
        //    string responseBody;
        //    if(!GameUpdater.HttpGet(_latestDownloadUrl, out responseBody))
        //    {
        //        Status = "Error getting files";
        //        return;
        //    }
        //    var fileMatches = Regex.Matches(responseBody, @"<a\s+href\s*=\s*(?:'|"")([^'""]+\.[^'""]{3})(?:'|"")", RegexOptions.IgnoreCase);
        //    foreach(Match match in fileMatches)
        //    {
        //        if(!match.Success)
        //        {
        //            continue;
        //        }
        //        var file = match.Groups[1].Value;
        //        if(string.IsNullOrEmpty(file))
        //        {
        //            continue;
        //        }

        //        files.Add(file);
        //    }
        //    _dayZFiles = files;

        //    ProcessNext();
        //}
#endregion


        public bool IsRunning
		{
			get { return _isRunning; }
			set
			{
				_isRunning = value;
				PropertyHasChanged("IsRunning");
			}
		}

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
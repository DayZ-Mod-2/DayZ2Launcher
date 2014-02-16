using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using SharpCompress.Common;
using SharpCompress.Reader;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class Arma2Installer : BindableBase
	{
		private string _latestDownloadUrl;
		private string _downloadedFileLocation;
		private string _extractedLocation;
		private string _status;
		private bool _isRunning;

		private void BeginDownload(WebClient wc, Uri downloadUrl)
		{
			wc.DownloadProgressChanged += (sender, args) =>
			{
				Status = string.Format("Downloading... {0}%", args.ProgressPercentage);
			};
			wc.DownloadFileCompleted += (sender, args) =>
			{
				if (args.Error != null)
				{
					Status = "Error downloading";
					IsRunning = false;
					return;
				}
				ExtractFile();
			};

			wc.DownloadFileAsync(downloadUrl,_downloadedFileLocation);
		}

		public void DownloadAndInstall(string latestDownloadUrl)
		{
			_latestDownloadUrl = latestDownloadUrl;

			if(string.IsNullOrEmpty(_latestDownloadUrl))
				return;

			var latestArma2OABetaFile = Path.GetFileName(_latestDownloadUrl);
			if(string.IsNullOrEmpty(latestArma2OABetaFile))
				return;

			IsRunning = true;
			Status = "Getting file info...";

			var extension = Path.GetExtension(latestArma2OABetaFile);
			_downloadedFileLocation = Path.Combine(UserSettings.PatchesPath,latestArma2OABetaFile);;
			_extractedLocation = Path.Combine(UserSettings.PatchesPath,latestArma2OABetaFile.Replace(extension, ""));

			using(var webClient = new WebClient())
			{
				var uri = new Uri(latestDownloadUrl);
				if (File.Exists(_downloadedFileLocation))
				{
					var fi = new FileInfo(_downloadedFileLocation);

					webClient.OpenReadCompleted += (sender, args) =>
					{
						WebClient wc = (WebClient)sender;
						Stream webStream = args.Result;
						if (webStream != null)
						{
							webStream.Close(); webStream.Dispose();
							webStream = null;
						}

						if (args.Cancelled || args.Error != null)
						{
							Status = "GET cancelled";
							if (args.Error != null)
								Status = "Error: " + args.Error.ToString();

							IsRunning = false;
							return;
						}

						long newFileLen = Convert.ToInt64(wc.ResponseHeaders["Content-Length"]);
						if (newFileLen == fi.Length)
						{
							Status = "Using local file";
							ExtractFile();
						}
						else
						{
							wc.CancelAsync();
							BeginDownload(wc, uri);
						}
					};
					webClient.OpenReadAsync(uri);
				}
				else
					BeginDownload(webClient, uri);
			}
		}

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

		private void ExtractFile()
		{
			new Thread(() =>
			           	{
			           		try
			           		{
			           			Status = "Extracting";
			           			Directory.CreateDirectory(_extractedLocation);
			           			using (var stream = File.OpenRead(_downloadedFileLocation))
			           			{
			           				using (var reader = ReaderFactory.Open(stream))
			           				{
			           					while (reader.MoveToNextEntry())
			           					{
			           						if (reader.Entry.IsDirectory)
			           						{
			           							continue;
			           						}
			           						var fileName = Path.GetFileName(reader.Entry.FilePath);
			           						if (string.IsNullOrEmpty(fileName))
			           						{
			           							continue;
			           						}
			           						reader.WriteEntryToDirectory(_extractedLocation,
			           						                             ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
			           						if (fileName.EndsWith(".exe"))
			           						{
			           							var p = new Process
			           							        	{
			           							        		StartInfo =
			           							        			{
			           							        				CreateNoWindow = false,
			           							        				UseShellExecute = true,
			           							        				WorkingDirectory = _extractedLocation,
			           							        				FileName = Path.Combine(_extractedLocation, fileName)
			           							        			}
			           							        	};
			           							p.Start();
			           							Status = "Installing";
			           							p.WaitForExit();
			           						}
			           					}
			           				}
			           			}

			           			Status = "Install complete";
			           		}
			           		catch (Exception)
			           		{
			           			Status = "Could not complete";
			           			IsRunning = false;
			           		}

			           		try { Directory.Delete(_extractedLocation, true); }
			           		catch (Exception) {}

			           		IsRunning = false;
			           	}).Start();

		}
	}
}
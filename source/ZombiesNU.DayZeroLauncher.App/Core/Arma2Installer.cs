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

		public void DownloadAndInstall(int revision, HashWebClient.RemoteFileInfo archiveInfo)
		{
			IsRunning = true;
			Status = "Getting file info...";

			string extractedFolderLocation = Path.Combine(UserSettings.PatchesPath, revision.ToString());
			string zipFileLocation = extractedFolderLocation + ".zip";

			var wc = new HashWebClient();
			wc.DownloadProgressChanged += (sender, args) =>
			{
				Status = string.Format("Downloading... {0}%", args.ProgressPercentage);
			};
			wc.DownloadFileCompleted += (sender, args) =>
			{
				if (args.Error != null)
				{
					Status = "Error: " + args.Error.Message;
					IsRunning = false;
					return;
				}
				ExtractFile(zipFileLocation,extractedFolderLocation);
			};
			wc.BeginDownload(archiveInfo, zipFileLocation);
		}

		private void ExtractFile(string zipFilename, string outputFolder)
		{
			new Thread(() =>
			{
				try
				{
					Status = DayZeroLauncherUpdater.STATUS_EXTRACTING;
					Directory.CreateDirectory(outputFolder);
					using (var stream = File.OpenRead(zipFilename))
					{
						using (var reader = ReaderFactory.Open(stream))
						{
							while (reader.MoveToNextEntry())
							{
								if (reader.Entry.IsDirectory)
									continue;

								var fileName = Path.GetFileName(reader.Entry.FilePath);
								if (string.IsNullOrEmpty(fileName))
									continue;


								reader.WriteEntryToDirectory(outputFolder, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
								if (fileName.EndsWith(".exe"))
								{
									var p = new Process
												{
													StartInfo =
														{
															CreateNoWindow = false,
															UseShellExecute = true,
															WorkingDirectory = outputFolder,
															FileName = Path.Combine(outputFolder, fileName)
														}
												};
									p.Start();
									Status = DayZeroLauncherUpdater.STATUS_INSTALLING;
									p.WaitForExit();
								}
							}
						}
					}

					Status = DayZeroLauncherUpdater.STATUS_INSTALLCOMPLETE;
					Execute.OnUiThreadSync(() => CalculatedGameSettings.Current.Update(), null, System.Windows.Threading.DispatcherPriority.Input);
				}
				catch (Exception)
				{
					Status = "Could not complete";
					IsRunning = false;
				}

				try { Directory.Delete(outputFolder, true); }
				catch (Exception) { }

				IsRunning = false;
			}).Start();
		}
	}
}
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Threading;
using SharpCompress.Common;
using SharpCompress.Reader;

using zombiesnu.DayZeroLauncher.App.Ui.Controls;
using zombiesnu.DayZeroLauncher.App.Ui;

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

        public void DownloadAndInstall(int revision, HashWebClient.RemoteFileInfo archiveInfo, bool steamBeta, UpdatesView view)
        {
            if (steamBeta)
            {
                int appId = 219540;
                string gameName = "Arma 2: Operation Arrowhead Beta";
                DirectoryInfo armaPath = null;

                try
                {
                    armaPath = new DirectoryInfo(CalculatedGameSettings.Current.Arma2OAPath);
                }
                catch (ArgumentException aex)
                {
                    var overridenPath = string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.Arma2OADirectoryOverride);

                    Execute.OnUiThreadSync(() =>
                    {
                        InfoPopup popup = new InfoPopup("Invalid Path To Arma2: OA", MainWindow.GetWindow(view));
                        popup.Headline.Content = "Game path could not be located";
                        popup.SetMessage(overridenPath ? "Invalid Game override path, please enter a new game path or remove it" : "Game could not located via the registry, please enter an override path");

                        popup.Show();
                    }, null, System.Windows.Threading.DispatcherPriority.Input);

                    return;
                }

                for (armaPath = armaPath.Parent; armaPath != null; armaPath = armaPath.Parent)
                {
                    if (armaPath.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                    {
                        string manifestName = "appmanifest_" + appId.ToString() + ".acf";
                        string fullManifestPath = Path.Combine(armaPath.FullName, manifestName);
                        if (!File.Exists(fullManifestPath))
                        {
                            Execute.OnUiThreadSync(() =>
                            {
                                InfoPopup popup = new InfoPopup("User intervention required", MainWindow.GetWindow(view));
                                popup.Headline.Content = "Game update using Steam";
                                popup.SetMessage(gameName + " is not installed.\n" +
                                                    "Please install it from the Library tab.\n" +
                                                    "Or by clicking on the following link:");
                                popup.SetLink("steam://install/" + appId.ToString() + "/", "Install " + gameName);
                                popup.Closed += (sender, args) => view.CheckForUpdates();
                                popup.Show();
                            }, null, System.Windows.Threading.DispatcherPriority.Input);

                            return;
                        }
                        else if (File.Exists(fullManifestPath))
                        {
                            Execute.OnUiThreadSync(() =>
                            {
                                InfoPopup popup = new InfoPopup("User intervention required", MainWindow.GetWindow(view));
                                popup.Headline.Content = "Game update using Steam";
                                popup.SetMessage(gameName + " needs to be updated.\n" +
                                                    "Please update it by verifying the files.\n" +
                                                    "Or by clicking on the following link:");
                                popup.SetLink("steam://validate/" + appId.ToString() + "/", "Update " + gameName);
                                popup.Closed += (sender, args) => view.CheckForUpdates();
                                popup.Show();
                            }, null, System.Windows.Threading.DispatcherPriority.Input);

                            return;
                        }
                        break;
                    }
                }
                if (armaPath == null)
                {
                    MessageBox.Show("Patching failed, '" + gameName + "' is not located inside a SteamLibrary folder.",
                        "Patch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
            else
            {
                DownloadAndInstall(revision, archiveInfo);
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
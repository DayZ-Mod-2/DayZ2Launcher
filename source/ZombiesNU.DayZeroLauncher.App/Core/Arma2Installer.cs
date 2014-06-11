using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SharpCompress.Common;
using SharpCompress.Reader;
using SteamKit2;
using zombiesnu.DayZeroLauncher.App.Ui;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class Arma2Installer : BindableBase
	{
		private bool _isRunning;

		private string _status;

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

		public void DownloadAndInstall(int revision, HashWebClient.RemoteFileInfo archiveInfo, bool steamBeta,
			string steamBuild, UpdatesView view)
		{
			if (steamBeta)
			{
				const int appId = 33930;
				string gameName = "ArmA 2: Operation Arrowhead";
				DirectoryInfo armaPath = null;

				try
				{
					armaPath = new DirectoryInfo(CalculatedGameSettings.Current.Arma2OAPath);
				}
				catch (ArgumentException)
				{
					bool overridenPath = !string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.Arma2OADirectoryOverride);

					Execute.OnUiThreadSync(() =>
					{
						var popup = new InfoPopup("Invalid path", MainWindow.GetWindow(view));
						popup.Headline.Content = "Game could not be found";
						popup.SetMessage(overridenPath
							? "Please verify your game override paths."
							: "Complete install by starting game from Steam.");

						popup.Show();
					}, null, DispatcherPriority.Input);

					return;
				}

				for (armaPath = armaPath.Parent; armaPath != null; armaPath = armaPath.Parent)
				{
					if (armaPath.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
					{
						string manifestName = "appmanifest_" + appId.ToString() + ".acf";
						string fullManifestPath = Path.Combine(armaPath.FullName, manifestName);
						if (File.Exists(fullManifestPath))
						{
							// Kill Steam so we can edit the game configuration.
							using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
							{
								var perm = RegistryKeyPermissionCheck.Default;
								var rights = RegistryRights.QueryValues;
								Int32 steamPid;

								try
								{
									using (RegistryKey steamKey = baseKey.OpenSubKey("SOFTWARE\\Valve\\Steam", perm, rights))
									{
										steamPid = (int)steamKey.GetValue("SteamPID", "");
										steamKey.Close();
									}
								}
								catch (Exception)
								{
									MessageBox.Show("Unable to find Steam Process ID.",
										"Patch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
									return;
								} //no steam key found

								try
								{
									Process steam = Process.GetProcessById(steamPid);
									steam.Kill();
									steam.WaitForExit();
								}
								catch (Exception) {}
							}

							var acfKeys = new KeyValue();
							var reader = new StreamReader(fullManifestPath);
							var acfReader = new KVTextReader(acfKeys, reader.BaseStream);
							reader.Close();
							KeyValue currentBuild = acfKeys.Children.FirstOrDefault(k => k.Name == "buildid");
							if (!String.IsNullOrEmpty(currentBuild.Value))
							{
								if (Equals(currentBuild.Value, steamBuild))
								{
									Execute.OnUiThreadSync(() =>
									{
										var popup = new InfoPopup("User intervention required", MainWindow.GetWindow(view));
										popup.Headline.Content = "Game update using Steam";
										popup.SetMessage(gameName + " might be corrupted.\n" +
										                 "Please click the following link to validate:");
										popup.SetLink("steam://validate/" + appId.ToString() + "/", "Update " + gameName);
										popup.Closed += (sender, args) => view.CheckForUpdates();
										popup.Show();
									}, null, DispatcherPriority.Input);
								}
								else
								{
									KeyValue gameState = acfKeys.Children.FirstOrDefault(k => k.Name == "StateFlags");
									if (!String.IsNullOrEmpty(gameState.Value))
									{
										currentBuild.Value = steamBuild;
										gameState.Value = "2";
										acfKeys.SaveToFile(fullManifestPath, false);

										Thread.Sleep(1000);

										Execute.OnUiThreadSync(() =>
										{
											var popup = new InfoPopup("User intervention required", MainWindow.GetWindow(view));
											popup.Headline.Content = "Game update using Steam";
											popup.SetMessage(gameName + " switched to BETA.\n" +
											                 "Please click the following link to validate:");
											popup.SetLink("steam://validate/" + appId + "/", "Update " + gameName);
											popup.Closed += (sender, args) => view.CheckForUpdates();
											popup.Show();
										}, null, DispatcherPriority.Input);
									}
								}
							}
							else
							{
								MessageBox.Show("Patching failed, '" + gameName + "' is not located inside a SteamLibrary folder.",
									"Patch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
								return;
							}

							return;
						}
						else
						{
							Execute.OnUiThreadSync(() =>
							{
								var popup = new InfoPopup("User intervention required", MainWindow.GetWindow(view));
								popup.Headline.Content = "Game update using Steam";
								popup.SetMessage(gameName + " is not installed.\n" +
								                 "Please install it from the Library tab.\n" +
								                 "Or by clicking on the following link:");
								popup.SetLink("steam://install/" + appId.ToString() + "/", "Install " + gameName);
								popup.Closed += (sender, args) => view.CheckForUpdates();
								popup.Show();
							}, null, DispatcherPriority.Input);

							return;
						}
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
			wc.DownloadProgressChanged +=
				(sender, args) => { Status = string.Format("Downloading... {0}%", args.ProgressPercentage); };
			wc.DownloadFileCompleted += (sender, args) =>
			{
				if (args.Error != null)
				{
					Status = "Error: " + args.Error.Message;
					IsRunning = false;
					return;
				}
				ExtractFile(zipFileLocation, extractedFolderLocation);
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
					using (FileStream stream = File.OpenRead(zipFilename))
					{
						using (IReader reader = ReaderFactory.Open(stream))
						{
							while (reader.MoveToNextEntry())
							{
								if (reader.Entry.IsDirectory)
									continue;

								string fileName = Path.GetFileName(reader.Entry.FilePath);
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
					Execute.OnUiThreadSync(() => CalculatedGameSettings.Current.Update(), null, DispatcherPriority.Input);
				}
				catch (Exception)
				{
					Status = "Could not complete";
					IsRunning = false;
				}

				try
				{
					Directory.Delete(outputFolder, true);
				}
				catch (Exception)
				{
					Status = "Could not complete";
					IsRunning = false;
				}

				IsRunning = false;
			}).Start();
		}
	}
}
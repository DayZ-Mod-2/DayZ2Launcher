using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using Newtonsoft.Json;
using DayZ2.DayZ2Launcher.App.Ui;
using DayZ2.DayZ2Launcher.App.Ui.Controls;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class MetaGameType
	{
		[JsonProperty("addons")] public List<string> AddOnNames;
		[JsonProperty("extensions")] public List<string> ExtensionNames;
		[JsonProperty("ident")] public string Ident;
		[JsonProperty("launchable")] public bool IsLaunchable;
		[JsonProperty("name")] public string Name;
	}

	public class GameLauncher_old : ViewModelBase,
		IHandle<GameLauncher_old.LaunchStartGameEvent>
	{
		private bool _isLaunching;
		private LaunchProgress _dlgWindow;
		private ObservableCollection<ButtonInfo> _launchButtons = new ObservableCollection<ButtonInfo>();

		private LaunchStartGameEvent _queuedLaunchEvt;

		public GameLauncher_old()
		{
		}

		public ObservableCollection<ButtonInfo> LaunchButtons
		{
			get => _launchButtons;
			set
			{
				_launchButtons = value;
				PropertyHasChanged("LaunchButtons");
			}
		}

		public bool IsLaunching
		{
			get => _isLaunching;
			private set
			{
				PropertyHasChanged("IsLaunching");
				_isLaunching = value;
			}
		}

		public void Handle(LaunchStartGameEvent launchEvt)
		{
			if (LaunchFromEvent(launchEvt) == false)
				_queuedLaunchEvt = launchEvt;
		}

		protected MetaGameType FindGameTypeWithIdent(string gameTypeIdent)
		{
			MetaGameType gameType = new MetaGameType();
				/*
				ModDetails.GameTypes.FirstOrDefault(
					x => { return String.Equals(x.Ident, gameTypeIdent, StringComparison.OrdinalIgnoreCase); });
			if (gameType == null)
				throw new GameTypeNotFound(gameTypeIdent);
				*/
			return gameType;
		}

		private bool LaunchFromEvent(LaunchStartGameEvent launchEvt)
		{
			MetaGameType gt;
			try
			{
				gt = FindGameTypeWithIdent(launchEvt.GameType.Trim());
			}
			catch (Exception)
			{
				gt = null;
			}

			if (gt == null) //defer for later when we know of this server
				return false;
			Execute.OnUiThread(() => LaunchGame(launchEvt.SourceWindow, gt.Ident),
				launchEvt.SourceWindow.Dispatcher, DispatcherPriority.Input);
			return true;
		}

		public void LaunchGame(Window parentWnd, string gameTypeIdent)
		{
			_queuedLaunchEvt = null;
			BeginLaunchProcess(parentWnd, null, gameTypeIdent);
		}

		public void JoinServer(Window parentWnd, Server server)
		{
			_queuedLaunchEvt = null;
			BeginLaunchProcess(parentWnd, server, server.Mod);
		}

		public event EventHandler GameLaunched;

		private void BeginLaunchProcess(Window parentWnd, Server server, string gameTypeIdent)
		{
			MetaGameType gameType;
			try
			{
				gameType = FindGameTypeWithIdent(gameTypeIdent);
			}
			catch (GameTypeNotFound nfe)
			{
				MessageBox.Show("Could not find gameType '" + nfe.GameType + "'",
					"Internal error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			catch (NullReferenceException)
			{
				MessageBox.Show("Version metadata not available. Please check for updates first.",
					"Unknown current version", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (_dlgWindow != null)
				return;

			IsLaunching = true;
			_dlgWindow = new LaunchProgress(parentWnd, gameType);
			_dlgWindow.Closed += (object sender, EventArgs e) =>
			{
				IsLaunching = false;
				if (_dlgWindow.InstallSuccessful)
				{
					new Thread(() =>
					{
						bool launchedGame = ActuallyLaunchGame(parentWnd, server, gameType);
						if (launchedGame)
						{
							GameLaunched(this, null);
							// TorrentUpdater.StopAllTorrents();

							if (UserSettings.Current.GameOptions.CloseDayZLauncher)
							{
								Thread.Sleep(1000);
								Environment.Exit(0);
							}
						}
					}).Start();
				}
				_dlgWindow = null;
			};
			_dlgWindow.Show();
		}

		private static bool ActuallyLaunchGame(Window parentWnd, Server server, MetaGameType gameType)
		{
			CloseGame();
			var arguments = new StringBuilder();

			string exePath;
			string gameName;
			try
			{
				GameVersions versions = CalculatedGameSettings.Current.Versions;
				GameVersion bestVer = versions.BestVersion;
				int bestRev = bestVer.BuildNo ?? 0;
				if (bestRev <= 0)
					throw new NullReferenceException();

				exePath = bestVer.ExePath;
				gameName = "Arma 2: Operation Arrowhead";

				bool beServer = server != null && server.ProtectionEnabled;
				if (bestVer.BuildNo >= 125402)  // Beta where BE launcher was introduced.
				{
					if (server == null || beServer)
					{
						string bePath = Path.Combine(CalculatedGameSettings.Current.Arma2OAPath, "ArmA2OA_BE.exe");
						if (File.Exists(bePath))
						{
							exePath = bePath;
							arguments.Append(" 0 0");
						}
					}
				}
			}
			catch (NullReferenceException)
			{
				MessageBox.Show("Could not find an appropriate version of the game.",
					"Game launch error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			if (UserSettings.Current.GameOptions.LaunchUsingSteam)
			{
				exePath = Path.Combine(LocalMachineInfo.Current.SteamPath, "steam.exe");
				if (!File.Exists(exePath))
				{
					MessageBox.Show("Could not find Steam, please adjust your options or check your Steam installation.",
						"Steam launch error", MessageBoxButton.OK, MessageBoxImage.Error);
					return false;
				}

				DirectoryInfo steamPath;
				const int appId = 33930; // ArmA2OA
				const string manifestName = "appmanifest_33930.acf";

				steamPath = new DirectoryInfo(LocalMachineInfo.Current.SteamPath);
				string steamAppsDir = Path.Combine(steamPath.FullName, "SteamApps");
				string fullManifestPath = Path.Combine(steamAppsDir, manifestName);

				// We did not find the manifest in the default steam library folder...
				if (!File.Exists(fullManifestPath))
				{
					// Try to calculate the
					DirectoryInfo pathInfo;

					try
					{
						pathInfo = new DirectoryInfo(CalculatedGameSettings.Current.Arma2OAPath);
					}
					catch (ArgumentException)
					{
						bool overridenPath = string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.Arma2OADirectoryOverride);

						Execute.OnUiThreadSync(() =>
						{
							var popup = new InfoPopup("Invalid Path To Arma2: OA", parentWnd);
							popup.Headline.Content = "Game path could not be located";
							popup.SetMessage(overridenPath
								? "Invalid Game override path, please enter a new game path or remove it"
								: "Game could not located via the registry, please enter an override path");

							popup.Show();
						}, null, DispatcherPriority.Input);

						return false;
					}

					for (pathInfo = pathInfo.Parent; pathInfo != null; pathInfo = pathInfo.Parent)
					{
						if (pathInfo.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
						{
							fullManifestPath = Path.Combine(pathInfo.FullName, manifestName);
							break;
						}
					}
				}

				if (!File.Exists(fullManifestPath))
				{
					Execute.OnUiThreadSync(() =>
					{
						var popup = new InfoPopup("User intervention required", parentWnd);
						popup.Headline.Content = "Game couldn't be launched";
						popup.SetMessage("According to Steam,\n" +
											gameName + " is not installed.\n" +
											"Please install it from the Library tab.\n" +
											"Or by clicking on the following link:");
						popup.SetLink("steam://install/" + appId + "/", "Install " + gameName);
						popup.Show();
					}, null, DispatcherPriority.Input);

					return false;
				}

				if (string.IsNullOrWhiteSpace(steamPath.FullName))
				{
					MessageBox.Show("Steam launch impossible, '" + gameName + "' isn't located inside a SteamLibrary folder.",
						"Game launch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return false;
				}

				arguments.Append(" -applaunch " + appId);
			}

			if (UserSettings.Current.GameOptions.MultiGpu)
			{
				arguments.Append(" -winxp");
			}

			if (UserSettings.Current.GameOptions.WindowedMode)
			{
				arguments.Append(" -window");
			}

			if (!string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.AdditionalStartupParameters))
			{
				arguments.Append(" " + UserSettings.Current.GameOptions.AdditionalStartupParameters);
			}

			arguments.Append(" -noSplash -noFilePatching");
			if (server != null)
			{
				arguments.Append(" -connect=" + server.JoinAddress.ToString());
				arguments.Append(" -port=" + server.JoinPort.ToString());
				if (!string.IsNullOrWhiteSpace(server.Password))
					arguments.Append(" -password=" + server.Password);
			}

			var modArgSb = new StringBuilder($"-mod={CalculatedGameSettings.Current.Arma2Path};Expansion;ca");

			/*
			IEnumerable<string> addOnNames = addOns.Select(x => x.Name).AsEnumerable();
			foreach (string addon in addOnNames)
			{
				string fullPath = Path.Combine(UserSettings.ContentDataPath, addon);
				fullPath = fullPath.Replace('/', '\\');

				string rootPath = CalculatedGameSettings.Current.Arma2OAPath;
				rootPath = rootPath.Replace('/', '\\');
				if (!rootPath.EndsWith("\\"))
					rootPath += "\\";

				if (fullPath.StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase))
					fullPath = fullPath.Substring(rootPath.Length);

				modArgSb.Append(";");
				modArgSb.Append(fullPath);
			}
			*/

			string fullPath = Path.Combine(UserSettings.ContentDataPath, gameType.Ident);
			fullPath = fullPath.Replace('/', '\\');
			modArgSb.Append(";");
			modArgSb.Append(fullPath);

			arguments.Append(" \"" + modArgSb + "\"");
			modArgSb.Clear();
			modArgSb = null;

			try
			{
				var p = new Process
				{
					StartInfo =
					{
						UseShellExecute = true,
						FileName = exePath,
						Arguments = arguments.ToString(),
						WorkingDirectory = CalculatedGameSettings.Current.Arma2OAPath,
					}
				};
				p.Start();
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				arguments.Clear();
			}

			return true;
		}

		protected static void CloseGame()
		{
			foreach (Process process in Process.GetProcessesByName("arma2oa"))
			{
				process.Kill();
				process.WaitForExit();
			}
		}

		public class ButtonInfo : BindableBase
		{
			private string _argument;
			private string _text;

			public ButtonInfo(string text, string argument)
			{
				Text = text;
				Argument = argument;
			}

			public string Text
			{
				get { return _text; }
				set
				{
					_text = value;
					PropertyHasChanged("Text");
				}
			}

			public string Argument
			{
				get { return _argument; }
				set
				{
					_argument = value;
					PropertyHasChanged("Argument");
				}
			}
		}

		protected class GameTypeNotFound : Exception
		{
			public GameTypeNotFound(string gameTypeIdent) : base("Could not find gameType")
			{
				GameType = gameTypeIdent;
			}

			public string GameType { get; set; }
		}

		public class LaunchStartGameEvent : MainWindow.LaunchRoutedCommand
		{
			public string GameType;

			public LaunchStartGameEvent(string gameType, NameValueCollection data, Window mainWnd)
				: base(data, mainWnd)
			{
				GameType = gameType;
			}
		}
	}

	public class JoinServerException : Exception
	{
		public JoinServerException(string fileName, string arguments, string workingDirectory, Exception exception) : base(
			"There was an error launching the game.\r\n"
			+ "File Name:" + fileName + "\r\n"
			+ "Arguments:" + arguments + "\r\n"
			+ "Working Directory:" + workingDirectory,
			exception)
		{
		}
	}
}
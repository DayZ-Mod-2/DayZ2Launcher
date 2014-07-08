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
using zombiesnu.DayZeroLauncher.App.Ui;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class MetaGameType
	{
		[JsonProperty("addons")] public List<string> AddOnNames;
		[JsonProperty("ident")] public string Ident;

		[JsonProperty("launchable")] public bool IsLaunchable;
		[JsonProperty("name")] public string Name;
	}

	public class GameLauncher : ViewModelBase,
		IHandle<GameLauncher.LaunchStartGameEvent>
	{
		private LaunchProgress _dlgWindow;
		private ObservableCollection<ButtonInfo> _launchButtons = new ObservableCollection<ButtonInfo>();

		private MetaModDetails _modDetails;
		private LaunchStartGameEvent _queuedLaunchEvt;

		public GameLauncher()
		{
			ModDetailsChanged +=
				(sender, args) => ModDetailsChangedHandler((MetaModDetails) args.UserState, args.Cancelled, args.Error);
		}

		public ObservableCollection<ButtonInfo> LaunchButtons
		{
			get { return _launchButtons; }
			set
			{
				_launchButtons = value;
				PropertyHasChanged("LaunchButtons");
			}
		}

		public MetaModDetails ModDetails
		{
			get { return _modDetails; }
		}

		public void Handle(LaunchStartGameEvent launchEvt)
		{
			if (LaunchFromEvent(launchEvt) == false)
				_queuedLaunchEvt = launchEvt;
		}

		protected MetaGameType FindGameTypeWithIdent(string gameTypeIdent)
		{
			MetaGameType gameType =
				ModDetails.GameTypes.FirstOrDefault(
					x => { return String.Equals(x.Ident, gameTypeIdent, StringComparison.OrdinalIgnoreCase); });
			if (gameType == null)
				throw new GameTypeNotFound(gameTypeIdent);

			return gameType;
		}

		public void SetModDetails(MetaModDetails newModDetails, bool cancelled = false, Exception ex = null)
		{
			if (newModDetails != null)
			{
				_modDetails = newModDetails;

				Execute.OnUiThread(() =>
				{
					LaunchButtons.Clear();
					foreach (MetaGameType gameType in _modDetails.GameTypes)
					{
						if (!gameType.IsLaunchable)
							continue;

						LaunchButtons.Add(new ButtonInfo("Launch " + gameType.Name, gameType.Ident));
					}
				});
			}

			ModDetailsChanged(this, new AsyncCompletedEventArgs(ex, cancelled, newModDetails));
		}

		public event AsyncCompletedEventHandler ModDetailsChanged;

		private void ModDetailsChangedHandler(MetaModDetails newModDetails, bool cancelled = false, Exception ex = null)
		{
			if (newModDetails != null && cancelled == false && ex == null) //only if we received some fresh new info
			{
				if (_queuedLaunchEvt != null)
				{
					LaunchStartGameEvent theEvt = _queuedLaunchEvt;
					_queuedLaunchEvt = null; //dont try twice with stale launch

					LaunchFromEvent(theEvt);
				}
			}
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

		protected void BeginLaunchProcess(Window parentWnd, Server server, string gameTypeIdent)
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
				MessageBox.Show("Version metadata not avaialable. Please check for updates first.",
					"Unknown current version", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (_dlgWindow != null)
				return;

			var culledAddons = new List<MetaAddon>();
			culledAddons.AddRange(
				ModDetails.AddOns.Where(
					x =>
					{
						return gameType.AddOnNames.Count(y => { return x.Name.Equals(y, StringComparison.OrdinalIgnoreCase); }) > 0;
					})
					.AsEnumerable());

			IEnumerable<MetaPlugin> enabledPlugins =
				ModDetails.Plugins.Where(
					x => UserSettings.Current.EnabledPlugins.Count(y => y.Equals(x.Ident, StringComparison.OrdinalIgnoreCase)) > 0)
					.AsEnumerable();
			foreach (MetaPlugin plugin in enabledPlugins)
			{
				MetaAddon addonForPlugin =
					ModDetails.AddOns.SingleOrDefault(x => x.Name.Equals(plugin.Addon, StringComparison.OrdinalIgnoreCase));
				if (addonForPlugin == null)
				{
					MessageBox.Show(String.Format("Could not find addon '{0}' referenced by plugin '{1}'", plugin.Addon, plugin.Ident),
						"Internal error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				culledAddons.Add(addonForPlugin);
			}
			_dlgWindow = new LaunchProgress(parentWnd, gameType, culledAddons);
			_dlgWindow.Closed += (object sender, EventArgs e) =>
			{
				if (_dlgWindow.InstallSuccessfull)
				{
					new Thread(() =>
					{
						bool launchedGame = ActuallyLaunchGame(parentWnd, server, culledAddons);
						if (launchedGame)
						{
							TorrentUpdater.StopAllTorrents();

							if (UserSettings.Current.GameOptions.CloseDayZeroLauncher)
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

		protected static bool ActuallyLaunchGame(Window parentWnd, Server server, IEnumerable<MetaAddon> addOns)
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
				if (bestVer.Equals(versions.Beta))
				{
					gameName = "Arma 2: Operation Arrowhead Beta";
				}
				else
				{
					gameName = "Arma 2: Operation Arrowhead";
				}

				if (bestVer.BuildNo >= 125402) // Beta where BE launcher was introduced.
				{
					string bePath = Path.Combine(CalculatedGameSettings.Current.Arma2OAPath, "ArmA2OA_BE.exe");
					if (File.Exists(bePath))
					{
						exePath = bePath;
						arguments.Append(" 0 0");
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

				int appId = 33930;

				DirectoryInfo pathInfo = null;

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
						string manifestName = "appmanifest_" + appId + ".acf";
						string fullManifestPath = Path.Combine(pathInfo.FullName, manifestName);
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
						break;
					}
				}
				if (pathInfo == null)
				{
					MessageBox.Show("Steam launch impossible, '" + gameName + "' isn't located inside a SteamLibrary folder.",
						"Game launch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return false;
				}
				pathInfo = null;

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

			var modArgSb = new StringBuilder(String.Format("-mod={0};Expansion;ca", CalculatedGameSettings.Current.Arma2Path));

			IEnumerable<string> addOnNames = addOns.Select(x => x.Name).AsEnumerable();
			foreach (string addon in addOnNames)
			{
				string fullPath = Path.Combine(CalculatedGameSettings.Current.AddonsPath, addon);
				fullPath.Replace('/', '\\');

				string rootPath = CalculatedGameSettings.Current.Arma2OAPath;
				rootPath.Replace('/', '\\');
				if (!rootPath.EndsWith("\\"))
					rootPath += "\\";

				if (fullPath.StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase))
					fullPath = fullPath.Substring(rootPath.Length);

				modArgSb.Append(";");
				modArgSb.Append(fullPath);
			}
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
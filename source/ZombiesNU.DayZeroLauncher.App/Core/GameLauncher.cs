using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Threading;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using zombiesnu.DayZeroLauncher.App.Ui;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using Caliburn.Micro;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class MetaGameType
	{
		[JsonProperty("ident")]
		public string Ident;

		[JsonProperty("addons")]
		public List<string> AddOnNames;

		[JsonProperty("name")]
		public string Name;

		[JsonProperty("launchable")]
		public bool IsLaunchable;
	}

	public class GameLauncher : ViewModelBase,
		IHandle<GameLauncher.LaunchStartGameEvent>
	{
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
			public LaunchStartGameEvent(string gameType, NameValueCollection data, Window mainWnd)
				: base(data, mainWnd)
			{
				this.GameType = gameType;
			}

			public string GameType;
		}

		public class ButtonInfo : BindableBase
		{
			public ButtonInfo(string text, string argument)
			{
				Text = text;
				Argument = argument;
			}

			private string _text = null;
			public string Text
			{
				get { return _text; }
				set
				{
					_text = value;
					PropertyHasChanged("Text");
				}
			}

			private string _argument = null;
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

		public GameLauncher()
		{
			ModDetailsChanged += (sender, args) => ModDetailsChangedHandler((MetaModDetails)args.UserState, args.Cancelled, args.Error);
		}

		private ObservableCollection<ButtonInfo> _launchButtons = new ObservableCollection<ButtonInfo>();
		public ObservableCollection<ButtonInfo> LaunchButtons
		{
			get { return _launchButtons; }
			set
			{
				_launchButtons = value;
				PropertyHasChanged("LaunchButtons");
			}
		}

		private MetaModDetails _modDetails = null;
		public MetaModDetails ModDetails
		{
			get { return _modDetails; }
		}

		protected MetaGameType FindGameTypeWithIdent(string gameTypeIdent)
		{
			var gameType = ModDetails.GameTypes.FirstOrDefault(x => { return String.Equals(x.Ident, gameTypeIdent, StringComparison.OrdinalIgnoreCase); });
			if (gameType == null)
				throw new GameTypeNotFound(gameTypeIdent);

			return gameType;
		}

		public void SetModDetails(MetaModDetails newModDetails, bool cancelled=false, Exception ex=null)
		{				
			if (newModDetails != null)
			{
				_modDetails = newModDetails;

				Execute.OnUiThread(() =>
					{
						LaunchButtons.Clear();
						foreach (var gameType in _modDetails.GameTypes)
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

		private LaunchStartGameEvent _queuedLaunchEvt = null;
		private void ModDetailsChangedHandler(MetaModDetails newModDetails, bool cancelled = false, Exception ex = null)
		{
			if (newModDetails != null && cancelled == false && ex == null) //only if we received some fresh new info
			{
				if (_queuedLaunchEvt != null)
				{
					var theEvt = _queuedLaunchEvt;
					_queuedLaunchEvt = null; //dont try twice with stale launch

					LaunchFromEvent(theEvt);
				}
			}
		}

		private bool LaunchFromEvent(LaunchStartGameEvent launchEvt)
		{
			MetaGameType gt;
			try { gt = FindGameTypeWithIdent(launchEvt.GameType.Trim()); }
			catch (Exception) { gt = null; }

			if (gt == null) //defer for later when we know of this server
				return false;
			else
			{
				Execute.OnUiThread(() => LaunchGame(launchEvt.SourceWindow, gt.Ident),
									launchEvt.SourceWindow.Dispatcher, System.Windows.Threading.DispatcherPriority.Input);
				return true;
			}
		}
		
		public void Handle(LaunchStartGameEvent launchEvt)
		{
			if (LaunchFromEvent(launchEvt) == false)
				_queuedLaunchEvt = launchEvt;
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

		private LaunchProgress _dlgWindow = null;

		protected void BeginLaunchProcess(Window parentWnd, Server server, string gameTypeIdent)
		{
			MetaGameType gameType = null;
			try { gameType = FindGameTypeWithIdent(gameTypeIdent); }
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
			culledAddons.AddRange(ModDetails.AddOns.Where(x => { return gameType.AddOnNames.Count(y => { return x.Name.Equals(y, StringComparison.OrdinalIgnoreCase); }) > 0; }).AsEnumerable());
			
			var enabledPlugins = ModDetails.Plugins.Where(x => UserSettings.Current.EnabledPlugins.Count(y => y.Equals(x.Ident, StringComparison.OrdinalIgnoreCase)) > 0).AsEnumerable();
			foreach (var plugin in enabledPlugins)
			{
				var addonForPlugin = ModDetails.AddOns.SingleOrDefault(x => x.Name.Equals(plugin.Addon, StringComparison.OrdinalIgnoreCase));
				if (addonForPlugin == null)
				{
					MessageBox.Show(String.Format("Could not find addon '{0}' referenced by plugin '{1}'",plugin.Addon,plugin.Ident),
									"Internal error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				culledAddons.Add(addonForPlugin);
			}
			_dlgWindow = new LaunchProgress(parentWnd, gameType, culledAddons);
			_dlgWindow.Closed += (object sender, EventArgs e) =>
				{
					if (_dlgWindow.InstallSuccessfull == true)
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
			bool isBeta = false;
			try
			{
				var versions = CalculatedGameSettings.Current.Versions;
				var bestVer = versions.BestVersion;
				int bestRev = bestVer.BuildNo ?? 0;
				if (bestRev <= 0)
					throw new NullReferenceException();

				exePath = bestVer.ExePath;
				if (bestVer.Equals(versions.Beta))
				{
					isBeta = true;
					gameName = "Arma 2: Operation Arrowhead Beta";
				}
				else
				{
					isBeta = false;
					gameName = "Arma 2: Operation Arrowhead";
				}
			}
			catch (NullReferenceException)
			{
				MessageBox.Show("Could not find an appropriate version of the game.",
								"Game launch error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			if(UserSettings.Current.GameOptions.LaunchUsingSteam)
			{
				exePath = Path.Combine(LocalMachineInfo.Current.SteamPath, "steam.exe");
				if(!File.Exists(exePath))
				{
					MessageBox.Show("Could not find Steam, please adjust your options or check your Steam installation.",
                        "Steam launch error",MessageBoxButton.OK,MessageBoxImage.Error);
					return false;
				}

				int appId = 33930;

                DirectoryInfo pathInfo = null;

                try
                {
                    pathInfo = new DirectoryInfo(CalculatedGameSettings.Current.Arma2OAPath);
                }
                catch (ArgumentException aex)
                {
                    var overridenPath = string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.Arma2OADirectoryOverride);

                    Execute.OnUiThreadSync(() =>
                    {
                        InfoPopup popup = new InfoPopup("Invalid Path To Arma2: OA", parentWnd);
                        popup.Headline.Content = "Game path could not be located";
                        popup.SetMessage(overridenPath ? "Invalid Game override path, please enter a new game path or remove it" : "Game could not located via the registry, please enter an override path");

                        popup.Show();
                    }, null, System.Windows.Threading.DispatcherPriority.Input);

                    return false;
                }
                
                for (pathInfo = pathInfo.Parent; pathInfo != null; pathInfo = pathInfo.Parent )
                {
                    if (pathInfo.Name.Equals("steamapps",StringComparison.OrdinalIgnoreCase))
                    {
                        string manifestName = "appmanifest_" + appId.ToString() + ".acf";
                        string fullManifestPath = Path.Combine(pathInfo.FullName, manifestName);
                        if (!File.Exists(fullManifestPath))
                        {
							Execute.OnUiThreadSync(() =>
							{
								InfoPopup popup = new InfoPopup("User intervention required", parentWnd);
								popup.Headline.Content = "Game couldn't be launched";
								popup.SetMessage("According to Steam,\n" +
													gameName + " is not installed.\n" +
													"Please install it from the Library tab.\n" +
													"Or by clicking on the following link:");
								popup.SetLink("steam://install/" + appId.ToString() + "/", "Install " + gameName);
								popup.Show();
							}, null, System.Windows.Threading.DispatcherPriority.Input);
							
                            return false;
                        }
                        break;
                    }
                }
                if (pathInfo == null)
                {
                    MessageBox.Show("Steam launch impossible, '" + gameName + "' isn't located inside a SteamLibrary folder.",
                        "Game launch error",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                    return false;
                }
                else { pathInfo = null; }

                arguments.Append(" -applaunch " + appId.ToString());
			}

			if(UserSettings.Current.GameOptions.MultiGpu)
			{
				arguments.Append(" -winxp");
			}

			if(UserSettings.Current.GameOptions.WindowedMode)
			{
				arguments.Append(" -window");
			}

			if(!string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.AdditionalStartupParameters))
			{
				arguments.Append(" " + UserSettings.Current.GameOptions.AdditionalStartupParameters);
			}

			arguments.Append(" -noSplash -noFilePatching");
            if (server != null)
            {
                arguments.Append(" -connect=" + server.IpAddress);
                arguments.Append(" -port=" + server.Port);
				if (!string.IsNullOrWhiteSpace(server.Password))
					arguments.Append(" -password=" + server.Password);
            }

            var modArgSb = new StringBuilder(String.Format("-mod={0};Expansion;ca", CalculatedGameSettings.Current.Arma2Path));

			var addOnNames = addOns.Select(x => x.Name).AsEnumerable();
			foreach (var addon in addOnNames)
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
			arguments.Append(" \"" + modArgSb.ToString() + "\"");
			modArgSb.Clear(); modArgSb = null;

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
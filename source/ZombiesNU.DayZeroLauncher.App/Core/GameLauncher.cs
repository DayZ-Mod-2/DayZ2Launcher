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

	public class GameLauncher : BindableBase
	{
		protected class GameTypeNotFound : Exception
		{
			public GameTypeNotFound(string gameTypeIdent) : base("Could not find gameType")
			{
				GameType = gameTypeIdent;
			}

			public string GameType { get; set; }
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
			set
			{
				_modDetails = value;

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
		}

		protected MetaGameType FindGameTypeWithIdent(string gameTypeIdent)
		{
			var gameType = ModDetails.GameTypes.FirstOrDefault(x => { return String.Equals(x.Ident, gameTypeIdent, StringComparison.OrdinalIgnoreCase); });
			if (gameType == null)
				throw new GameTypeNotFound(gameTypeIdent);

			return gameType;
		}

		public void LaunchGame(Window parentWnd, string gameTypeIdent)
        {
			BeginLaunchProcess(parentWnd, null, gameTypeIdent);
        }

        public void JoinServer(Window parentWnd, Server server)
        {
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

			var culledAddons = ModDetails.AddOns.Where(x => { return gameType.AddOnNames.Count(y => { return x.Name.Equals(y, StringComparison.OrdinalIgnoreCase); }) > 0; });
			_dlgWindow = new LaunchProgress(parentWnd, gameType, culledAddons);
			_dlgWindow.Closed += (object sender, EventArgs e) =>
				{
					if (_dlgWindow.InstallSuccessfull == true)
					{
						new Thread(() =>
							{
								bool launchedGame = ActuallyLaunchGame(parentWnd, server, gameType);
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

        protected static bool ActuallyLaunchGame(Window parentWnd, Server server, MetaGameType gameType)
		{
            CloseGame();
			var arguments = new StringBuilder();

			string exePath;			
			if(UserSettings.Current.GameOptions.LaunchUsingSteam)
			{
				exePath = Path.Combine(LocalMachineInfo.Current.SteamPath, "steam.exe");
				if(!File.Exists(exePath))
				{
					MessageBox.Show("Could not find Steam, please adjust your options or check your Steam installation.",
                        "Steam launch error",MessageBoxButton.OK,MessageBoxImage.Error);
					return false;
				}

                int mainVersionRev = 0;
                {
                    string mainEXE = GameVersions.BuildArma2OAExePath(CalculatedGameSettings.Current.Arma2OAPath);
                    var mainVersion = GameVersions.ExtractArma2OABetaVersion(mainEXE);
                    if (mainVersion != null) mainVersionRev = mainVersion.Revision;
                }
                int betaVersionRev = 0;
                {
                    string betaEXE = GameVersions.BuildArma2OAExePath(Path.Combine(CalculatedGameSettings.Current.Arma2OAPath, "Expansion\\beta"));
                    var betaVersion = GameVersions.ExtractArma2OABetaVersion(betaEXE);
                    if (betaVersion != null) betaVersionRev = betaVersion.Revision;
                }

                int appId = 219540;
                string gameName = "Arma 2: Operation Arrowhead Beta";
                if (mainVersionRev > betaVersionRev)
                {
                    appId = 33930;
                    gameName = "Arma 2: Operation Arrowhead";
                }

                var pathInfo = new DirectoryInfo(CalculatedGameSettings.Current.Arma2OAPath);
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
			else
			{
				exePath = CalculatedGameSettings.Current.Arma2OAExePath;
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
                arguments.Append(" -password=" + server.Password);
            }

			string modArg = String.Format("-mod={0};Expansion;Expansion\\beta;Expansion\\beta\\Expansion", CalculatedGameSettings.Current.Arma2Path);
			foreach (var addon in gameType.AddOnNames)
			{
				string fullPath = Path.Combine(CalculatedGameSettings.Current.AddonsPath, addon);
				fullPath.Replace('/', '\\');
				
				string rootPath = CalculatedGameSettings.Current.Arma2OAPath;
				rootPath.Replace('/', '\\');
				if (!rootPath.EndsWith("\\"))
					rootPath += "\\";

				if (fullPath.StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase))
					fullPath = fullPath.Substring(rootPath.Length);

				modArg += String.Format(";{0}", fullPath);
			}
			arguments.Append(" \"" + modArg + "\"");

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
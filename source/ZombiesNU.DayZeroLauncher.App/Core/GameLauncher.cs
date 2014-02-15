using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using NLog;
using System.Threading;
using zombiesnu.DayZeroLauncher.App.Ui;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;

namespace zombiesnu.DayZeroLauncher.App.Core
{
    public enum Mod {
        DayZeroPodagorsk,
        DayZeroChernarus
    }

	public static class GameLauncher
	{
		private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void LaunchGame(Window parentWnd, Mod mod)
        {
            string modArg = String.Format(" \"-mod={0};Expansion;Expansion\\beta;Expansion\\beta\\Expansion;{1};{2}\"", CalculatedGameSettings.Current.Arma2Path, CalculatedGameSettings.Current.DayZPath, CalculatedGameSettings.Current.DayZPath.Substring(0, CalculatedGameSettings.Current.DayZPath.Length - "@DayZero".Length) + "@" + mod);
            JoinServer(parentWnd, null, modArg);
        }

        public static void JoinServer(Window parentWnd, Server server)
        {
            string modArg = String.Format(" \"-mod={0};Expansion;Expansion\\beta;expansion\\beta\\Expansion;{1};{2}\"", CalculatedGameSettings.Current.Arma2Path, CalculatedGameSettings.Current.DayZPath, CalculatedGameSettings.Current.DayZPath.Substring(0, CalculatedGameSettings.Current.DayZPath.Length-"@DayZero".Length) + server.Mod);
            JoinServer(parentWnd, server, modArg);
        }

        static void JoinServer(Window parentWnd, Server server, string modArg)
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
					return;
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
                            InfoPopup popup = new InfoPopup();
                            popup.Owner = parentWnd;
                            popup.Title = "User intervention required";
                            popup.Headline.Content = "Game couldn't be launched";
                            popup.SetMessage(       "According to Steam,\n" + 
                                                    gameName + " is not installed.\n" +
                                                    "Please install it from the Library tab.\n" +
                                                    "If it does not appear in your Library,\n" +
                                                    "you can acquire it by going here:");
                            popup.SetLink("http://store.steampowered.com/app/" + appId.ToString() + "/");
                            popup.Show();
                            return;
                        }
                        break;
                    }
                }
                if (pathInfo == null)
                {
                    MessageBox.Show("Steam launch impossible '" + gameName + "' isn't located inside a SteamLibrary folder.",
                        "Game launch error",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                    return;
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
            arguments.AppendFormat(modArg);

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

                if(UserSettings.Current.GameOptions.CloseDayZeroLauncher){
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                }
			}
			catch(Exception)
			{
			}
			finally
			{
				arguments.Clear();
			}
		}

        public static void CloseGame()
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
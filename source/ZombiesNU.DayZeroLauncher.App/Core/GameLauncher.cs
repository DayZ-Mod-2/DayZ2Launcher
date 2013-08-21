using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using NLog;
using System.Threading;

namespace zombiesnu.DayZeroLauncher.App.Core
{
    public enum Mod {
        DayZeroPodagorsk,
        DayZeroChernarus
    }

	public static class GameLauncher
	{
		private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void LaunchGame(Mod mod)
        {
            string modArg = String.Format(" \"-mod={0};Expansion;Expansion\\beta;Expansion\\beta\\Expansion;{1};{2}\"", CalculatedGameSettings.Current.Arma2Path, CalculatedGameSettings.Current.DayZPath, CalculatedGameSettings.Current.DayZPath.Substring(0, CalculatedGameSettings.Current.DayZPath.Length - "@DayZero".Length) + "@" + mod);
            JoinServer(null, modArg);
        }

        public static void JoinServer(Server server)
        {
            string modArg = String.Format(" \"-mod={0};Expansion;Expansion\\beta;expansion\\beta\\Expansion;{1};{2}\"", CalculatedGameSettings.Current.Arma2Path, CalculatedGameSettings.Current.DayZPath, CalculatedGameSettings.Current.DayZPath.Substring(0, CalculatedGameSettings.Current.DayZPath.Length-"@DayZero".Length) + server.Mod);
            JoinServer(server, modArg);
        }

		static void JoinServer(Server server, string modArg)
		{
            CloseGame();
			var arguments = new StringBuilder();

			string exePath;
			
			if(UserSettings.Current.GameOptions.LaunchUsingSteam)
			{
				exePath = Path.Combine(LocalMachineInfo.Current.SteamPath, "steam.exe");
				if(!File.Exists(exePath))
				{
					MessageBox.Show("Could not find Steam, please adjust your options or check your Steam installation.");
					return;
				}

                arguments.Append(" -applaunch 33930");

                    string mainEXE = Path.Combine(CalculatedGameSettings.Current.Arma2OAPath, @"arma2oa.exe");
                    string betaEXE = Path.Combine(CalculatedGameSettings.Current.Arma2OAPath, @"Expansion\beta\arma2oa.exe");
					if(File.Exists(mainEXE) && File.Exists(betaEXE))
					{
						var mainExeVersion = FileVersionInfo.GetVersionInfo(mainEXE).ProductVersion;
						var betaExeVersion = FileVersionInfo.GetVersionInfo(betaEXE).ProductVersion;

						if (mainExeVersion != betaExeVersion)
						{
							File.Copy(mainEXE, mainEXE + "_" + mainExeVersion, true);
							File.Copy(betaEXE, mainEXE, true);
						}
					}
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
                                        Verb = "runas",
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayZ2.DayZ2Launcher.App.Core
{
	class GameLauncher
	{
		public bool IsLaunching { get; set; }

		/*
		public static async Task<bool> LaunchGame(Server server, string[] mods)
		{
			if (server != null)
			{

			}

			if (UserSettings.Current.GameOptions.LaunchUsingSteam)
			{
				
			}
		}

		private static async Task<bool> LaunchGameSteam()
		{
			string manifest = Path.Combine(LocalMachineInfo.Current.SteamPath, "steamapps", "appmanifest_33930.acf");

			if (!File.Exists(manifest))
			{

			}
		}

		private static async Task<bool> LaunchGameExe()
		{

		}

		private static string GetLaunchArguments(Server server)
		{

		}

		public static bool IsRunning()
		{
			return false;
		}

		public static Task CloseGame()
		{
			foreach (Process process in Process.GetProcessesByName("arma2oa"))
			{
				process.Kill();
				process.WaitForExit();
			}
		}
		*/
	}
}

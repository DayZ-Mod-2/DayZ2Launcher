using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class GameLauncher
	{
		public bool CanLaunch { get; set; }

#nullable enable
		public bool LaunchGame(Server? server)
		{
			bool battleye = server?.Battleye ?? true;
			// TODO: does the battleye version require additional args "0 0"?
			string exe = Path.Combine(CalculatedGameSettings.Current.Arma2OAPath, battleye ? "ArmA2OA_BE.exe" : "ArmA2OA.exe");

			if (!File.Exists(exe))
			{
				MessageBox.Show($"Executable file does not exist: {exe}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			string args = GetLaunchArguments(server);

			var process = new Process()
			{
				StartInfo =
				{
					FileName = exe,
					UseShellExecute = true,
					Arguments = args,
					WorkingDirectory = CalculatedGameSettings.Current.Arma2OAPath
				}
			};

			bool succeeded = process.Start();

			if (succeeded && UserSettings.Current.GameOptions.CloseDayZLauncher)
			{
				// TODO: shutdown
			}

			return succeeded;
		}

		private static string GetLaunchArguments(Server? server)
		{
			List<string> args = new()
			{
				"-noSplash"
				,"-noFilePatching"
			};

			// TODO: dont hardcode mod name
			args.Add($"-mod={Path.Combine(UserSettings.ContentDataPath, "@DayZ2")}");
			// args.Add(new StringBuilder().Append("-mods=").AppendJoin(';', server.Mods).ToString());


			if (UserSettings.Current.GameOptions.WindowedMode)
				args.Add("-window");

			if (UserSettings.Current.GameOptions.MultiGpu)
				args.Add("-winxp");

			if (!string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.AdditionalStartupParameters))
				args.Add(UserSettings.Current.GameOptions.AdditionalStartupParameters);

			if (server != null)
			{
				args.Add($"-connect={server.Hostname}");
				args.Add($"-port={server.GamePort}");
			}

			return new StringBuilder().AppendJoin(' ', args).ToString();
		}
#nullable disable

		public static bool IsRunning()
		{
			return Process.GetProcessesByName("arma2oa").Any();
		}

		public static async Task CloseGame(CancellationToken cancellationToken)
		{
			foreach (Process process in Process.GetProcessesByName("arma2oa"))
			{
				process.Kill();
				await process.WaitForExitAsync(cancellationToken);
			}
		}
	}
}

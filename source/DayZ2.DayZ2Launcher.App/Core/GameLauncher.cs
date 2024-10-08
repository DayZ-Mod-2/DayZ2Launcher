﻿using DayZ2.DayZ2Launcher.App.Ui;
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
		private readonly CrashLogUploader m_crashLogUploader;
		public string DayZCurrentVersion { get; set; }
		private const string ProcessName = "arma2oa";

		public class GameLaunchedEventArgs : EventArgs
		{
			public int ProcessId { get; set; }
		}
		public event EventHandler<GameLaunchedEventArgs> GameLaunched;
		
		public class GameClosedEventArgs : EventArgs
		{
			public int ExitCode { get; set; }
		}
		public event EventHandler<GameClosedEventArgs> GameClosed;

		public GameLauncher(CrashLogUploader crashLogUploader)
		{
			m_crashLogUploader = crashLogUploader;
		}

#nullable enable
		public bool LaunchGame(Server? server)
		{
			bool battleye = server?.Battleye ?? true;

			string exe = Path.Combine(CalculatedGameSettings.Current.Arma2OAPath, battleye ? "ArmA2OA_BE.exe" : "ArmA2OA.exe");

			if (!File.Exists(exe))
			{
				MessageBox.Show($"Executable file does not exist: {exe}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}

			string args = GetLaunchArguments(server);

			m_crashLogUploader.GameStarting();
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
			process.EnableRaisingEvents = true;
			process.Exited += delegate(object? sender, EventArgs eventArgs)
			{
				GameClosed.Invoke(this, new GameClosedEventArgs(){ ExitCode = process.ExitCode });
				OnGameExit(process.ExitCode);
			};

			bool succeeded = process.Start();
			GameLaunched.Invoke(this, new GameLaunchedEventArgs(){ ProcessId = process.Id });
			App.Current.Minimize();

			/*
			if (succeeded && UserSettings.Current.GameOptions.CloseDayZLauncher)
			{
				UserSettings.Current.Save();
				App.Current.RequestShutdown();
			}
			*/

			return succeeded;
		}

		private void OnGameExit(int exitCode)
		{
			App.Current.BringToForeground();

			if (exitCode == 0)
			{
				m_crashLogUploader.GameClosed(DayZCurrentVersion);
			}
			else
			{
				m_crashLogUploader.GameCrashed(DayZCurrentVersion);
			}
		}

		private static string GetLaunchArguments(Server? server)
		{
			bool battleye = server?.Battleye ?? true;

			List<string> args = new();

			if (battleye)
			{
				args.Add("0");
				args.Add("0");
			}

			args.Add("-noSplash");
			args.Add("-noFilePatching");

			args.Add($"\"-mod={CalculatedGameSettings.Current.Arma2Path};Expansion;ca\"");
			args.Add($"\"-mod={Path.Combine(UserSettings.ContentDataPath, "@DayZ2")}\"");

			if (UserSettings.Current.GameOptions.WindowedMode)
				args.Add("-window");

			if (UserSettings.Current.GameOptions.MultiGpu)
				args.Add("-winxp");

			// TODO: escape additional parameters too?
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
			return Process.GetProcessesByName(ProcessName).Any();
		}

		public static Task CloseGameAsync(CancellationToken cancellationToken)
		{
			return Task.WhenAll(Process.GetProcessesByName(ProcessName).Select(n =>
			{
				n.Kill();
				return n.WaitForExitAsync(cancellationToken);
			}));
		}
	}
}

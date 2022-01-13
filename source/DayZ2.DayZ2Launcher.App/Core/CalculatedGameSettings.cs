using System;
using System.IO;
using Newtonsoft.Json;

namespace DayZ2.DayZ2Launcher.App.Core
{
	public class CalculatedGameSettings : BindableBase
	{
		private static CalculatedGameSettings _current;

		public static CalculatedGameSettings Current
		{
			get
			{
				if (_current == null)
				{
					_current = new CalculatedGameSettings();
					_current.Update();
				}
				return _current;
			}
		}

		public string Arma2Path { get; set; }
		public string Arma2OAPath { get; set; }
		public GameVersions Versions { get; set; }
		public string ModContentVersion { get; set; }

		public void Update()
		{
			SetArma2Path();
			SetArma2OAPath();
			SetGameVersions();
			SetModContentVersion();
		}

		public void SetArma2Path()
		{
			if (!string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.Arma2DirectoryOverride))
				Arma2Path = UserSettings.Current.GameOptions.Arma2DirectoryOverride;
			else
				Arma2Path = LocalMachineInfo.Current.Arma2Path;

			PropertyHasChanged("Arma2Path");
		}

		public void SetArma2OAPath()
		{
			if (!string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.Arma2OADirectoryOverride))
				Arma2OAPath = UserSettings.Current.GameOptions.Arma2OADirectoryOverride;
			else
				Arma2OAPath = LocalMachineInfo.Current.Arma2OAPath;

			PropertyHasChanged("Arma2OAPath");
		}

		private void SetGameVersions()
		{
			if (!string.IsNullOrEmpty(Arma2OAPath))
				Versions = new GameVersions(Arma2OAPath);
			else
				Versions = null;

			PropertyHasChanged("Versions");
		}

		private void SetModContentVersion()
		{
			try
			{
				ModContentVersion = File.ReadAllText(UserSettings.ContentCurrentTagFile).Trim();
			}
			catch (Exception)
			{
				ModContentVersion = null;
			}

			PropertyHasChanged("ModContentVersion");
		}
	}
}

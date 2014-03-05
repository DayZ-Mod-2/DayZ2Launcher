using System;
using System.IO;
using Newtonsoft.Json;

namespace zombiesnu.DayZeroLauncher.App.Core
{	public class LocatorInfo
	{
		[JsonProperty("patches")]
		public HashWebClient.RemoteFileInfo Patches = null;
		[JsonProperty("mods")]
		public HashWebClient.RemoteFileInfo Mods = null;
		[JsonProperty("installers")]
		public HashWebClient.RemoteFileInfo Installers = null;

		static public LocatorInfo LoadFromString(string jsonText)
		{
			return JsonConvert.DeserializeObject<LocatorInfo>(jsonText);
		}
	}

	public class CalculatedGameSettings : BindableBase
	{
		private static CalculatedGameSettings _current;
		public static CalculatedGameSettings Current
		{
			get
			{
				if(_current == null)
				{
					_current = new CalculatedGameSettings();
					_current.Update();
				}
				return _current;
			}
		}

		public string Arma2Path { get; set; }
		public string Arma2OAPath { get; set; }
		public string Arma2OAExePath { get; set; }
		public string AddonsPath { get; set; }
		public Version Arma2OABetaVersion { get; set; }
		public string ModContentVersion { get; set; }

		public LocatorInfo Locator { get; set; }

		public void Update()
		{
			SetArma2Path();
			SetArma2OAPath();
			SetArma2OAExePath();
			SetAddonsPath();
			SetArma2OABetaVersion();
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

		private void SetArma2OAExePath()
		{
			if (!string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.Arma2OADirectoryOverride))
                Arma2OAExePath = GameVersions.BuildArma2OAExePath(UserSettings.Current.GameOptions.Arma2OADirectoryOverride);
			else
				Arma2OAExePath = LocalMachineInfo.Current.Arma2OABetaExe;

			PropertyHasChanged("Arma2OAExePath");
		}

		public void SetAddonsPath()
		{
			if (!string.IsNullOrWhiteSpace(UserSettings.Current.GameOptions.AddonsDirectoryOverride))
				AddonsPath = UserSettings.Current.GameOptions.AddonsDirectoryOverride;
			else
				AddonsPath = Arma2OAPath;

			PropertyHasChanged("AddonsPath");
		}

		private void SetArma2OABetaVersion()
		{
			if(!string.IsNullOrEmpty(Arma2OAExePath))
				Arma2OABetaVersion = GameVersions.ExtractArma2OABetaVersion(Arma2OAExePath);
			else
				Arma2OABetaVersion = null;

			PropertyHasChanged("Arma2OABetaVersion");
		}

		private void SetModContentVersion()
		{
			try
			{
				ModContentVersion = File.ReadAllText(UserSettings.ContentCurrentTagFile).Trim();
			}
			catch (Exception) { ModContentVersion = null; }

			PropertyHasChanged("ModContentVersion");
		}
	}
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class MetaModDetails
	{
		[JsonProperty("addons")]
		public List<MetaAddon> AddOns;

		[JsonProperty("gametypes")]
		public List<MetaGameType> GameTypes;

		public static string GetFileName(string versionString)
		{
			return Path.Combine(UserSettings.ContentMetaPath, versionString + ".json");
		}

		public static MetaModDetails LoadFromFile(string fullPath)
		{
			var modDetails = JsonConvert.DeserializeObject<MetaModDetails>(File.ReadAllText(fullPath));
			return modDetails;
		}
	}

	public static class GameVersions
	{
		public static string BuildArma2OAExePath(string arma2OAPath)
		{
			return Path.Combine(arma2OAPath, "arma2oa.exe");
		}

		public static Version ExtractArma2OABetaVersion(string arma2OAExePath)
		{
			if(!File.Exists(arma2OAExePath))
				return null;

			var versionInfo = FileVersionInfo.GetVersionInfo(arma2OAExePath);
			Version version;
			if(Version.TryParse(versionInfo.ProductVersion, out version))
			{
				return version;
			}
			return null;
		}
	}
}
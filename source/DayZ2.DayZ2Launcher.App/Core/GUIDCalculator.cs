using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SteamKit2;

namespace DayZ2.DayZ2Launcher.App.Core
{
	internal class GUIDCalculator
	{
		private const string Arma2AppManifestFile = "appmanifest_33930.acf";

		private static string MD5Hex(byte[] bytes)
		{
			MD5 mD = MD5.Create();
			byte[] array = mD.ComputeHash(bytes);
			return array.Aggregate("", (current, b) => current + $"{b:x2}");
		}

		private static string GetManifestPath()
		{
			string result = "";

			var steamConfig = new DirectoryInfo(LocalMachineInfo.Current.SteamPath);
			string steamAppsDir = Path.Combine(steamConfig.FullName, "steamapps");
			string manifestFile = Path.Combine(steamAppsDir, Arma2AppManifestFile);

			if (File.Exists(manifestFile))
			{
				result = manifestFile;
			}
			else
			{
				// Is the game located in an alternative library folder..?
				steamConfig = new DirectoryInfo(CalculatedGameSettings.Current.Arma2OAPath);
				for (steamConfig = steamConfig.Parent; steamConfig != null; steamConfig = steamConfig.Parent)
				{
					if (steamConfig.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
					{
						manifestFile = Path.Combine(steamConfig.FullName, Arma2AppManifestFile);
						if (File.Exists(manifestFile))
						{
							result = manifestFile;
						}
						break;
					}
				}
			}
			return result;
		}

		private static KeyValue GetAppManifestValue(string manifestPath, string key)
		{
			var acfKeys = new KeyValue();
			var reader = new StreamReader(manifestPath);
			var _ = new KVTextReader(acfKeys, reader.BaseStream);
			reader.Close();
			return acfKeys.Children.FirstOrDefault(k => k.Name == key);
		}

		public static string GetKey()
		{
			string result = "Could not calculate GUID.";
			string fullManifestPath = GetManifestPath();

			if (!string.IsNullOrEmpty(fullManifestPath))
			{
				try
				{
					KeyValue lastOwner = GetAppManifestValue(fullManifestPath, "LastOwner");

					if (lastOwner != null && !string.IsNullOrEmpty(lastOwner.Value))
					{
						long steamId = long.Parse(lastOwner.Value);
						int i = 2;

						byte[] parts = { (byte)'B', (byte)'E', 0, 0, 0, 0, 0, 0, 0, 0 };

						do
						{
							parts[i++] = (byte)(steamId & 0xFF);
						} while ((steamId >>= 8) != 0);


						result = MD5Hex(parts);
					}
				}
				catch (Exception) { }
			}
			return result;
		}
	}
}

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SteamKit2;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	internal class GUIDCalculator
	{
		private const string Arma2AppManifestFile = "appmanifest_33930.acf";

		private static string MD5Hex(byte[] bytes)
		{
			MD5 mD = new MD5CryptoServiceProvider();
			byte[] array = mD.ComputeHash(bytes);
			string str = "";
			byte[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				byte b = array2[i];
				str += string.Format("{0:x2}", b);
			}
			return str;
		}

		private static string GetSteamAppsFolderPath()
		{
			DirectoryInfo steamConfig;
			string result = "";

			steamConfig = new DirectoryInfo(LocalMachineInfo.Current.SteamPath);
			string steamAppsDir = Path.Combine(steamConfig.FullName, "SteamApps");

			if (Directory.Exists(steamAppsDir))
			{
				result = steamAppsDir;
			}
			return result;
		}

		private static KeyValue GetAppManifestValue(string manifestPath, string key)
		{
			var acfKeys = new KeyValue();
			var reader = new StreamReader(manifestPath);
			var acfReader = new KVTextReader(acfKeys, reader.BaseStream);
			reader.Close();
			return acfKeys.Children.FirstOrDefault(k => k.Name == key);
		}

		public static string GetKey()
		{
			string result = "Could not calculate GUID.";
			string steamAppsPath = GetSteamAppsFolderPath();
			string fullManifestPath = Path.Combine(steamAppsPath, Arma2AppManifestFile);

			if (!String.IsNullOrEmpty(fullManifestPath) && File.Exists(fullManifestPath))
			{
				try
				{
					KeyValue lastOwner = GetAppManifestValue(fullManifestPath, "LastOwner");

					if (lastOwner != null && !String.IsNullOrEmpty(lastOwner.Value))
					{
						Int64 steamID = Int64.Parse(lastOwner.Value);
						int i = 2;

						byte[] parts = {(byte) 'B', (byte) 'E', 0, 0, 0, 0, 0, 0, 0, 0};

						do
						{
							parts[i++] = (byte) (steamID & 0xFF);
						} while ((steamID >>= 8) != 0);


						result = MD5Hex(parts);
					}
				}
				catch (Exception) {}
			}
			return result;
		}
	}
}
using System;
using System.Deployment.Application;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using Microsoft.Win32;

// ReSharper disable InconsistentNaming

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class LocalMachineInfo : BindableBase
	{
		private static LocalMachineInfo _current;
		private string _arma2OaPath;
		private string _arma2Path;
		private string _steamPath;

		public static LocalMachineInfo Current
		{
			get
			{
				if (_current == null)
				{
					_current = new LocalMachineInfo();
					_current.Update();
				}
				return _current;
			}
		}

		public Version DayZeroLauncherVersion
		{
			get
			{
				if (ApplicationDeployment.IsNetworkDeployed)
					return ApplicationDeployment.CurrentDeployment.CurrentVersion;
				return Assembly.GetEntryAssembly().GetName().Version;
			}
		}

		public string Arma2Path
		{
			get { return _arma2Path; }
			private set
			{
				_arma2Path = value;
				PropertyHasChanged("Arma2Path");
			}
		}

		public string Arma2OAPath
		{
			get { return _arma2OaPath; }
			private set
			{
				_arma2OaPath = value;
				PropertyHasChanged("Arma2OAPath");
			}
		}

		public string SteamPath
		{
			get { return _steamPath; }
			private set
			{
				_steamPath = value;
				PropertyHasChanged("SteamPath");
			}
		}

		public void Update()
		{
			SetPaths();
		}

		private void SetPaths()
		{
			using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
			{
				var perm = RegistryKeyPermissionCheck.Default;
				var rights = RegistryRights.QueryValues;

				try
				{
					using (RegistryKey steamKey = baseKey.OpenSubKey("SOFTWARE\\Valve\\Steam", perm, rights))
					{
						string possibleSteamPath = (string) steamKey.GetValue("InstallPath", "");
						steamKey.Close();
						steamKey.Dispose();
						if (Directory.Exists(possibleSteamPath))
						{
							SteamPath = possibleSteamPath;
						}
						else
						{
							SteamPath = "";
						}
					}
				}
				catch (Exception)
				{
					SteamPath = "";
				} // No Steam..? We need steam!

				try
				{
					using (RegistryKey bohemiaKey = baseKey.OpenSubKey("SOFTWARE\\Bohemia Interactive Studio", perm, rights))
					{
						try
						{
							RegistryKey arma2Key = bohemiaKey.OpenSubKey("ArmA 2", perm, rights);
							string possibleArma2Path = (string)arma2Key.GetValue("main", "");
							arma2Key.Close();
							arma2Key.Dispose();
							if (Directory.Exists(possibleArma2Path))
							{
								Arma2Path = possibleArma2Path;
							}
							else
							{
								Arma2Path = "";
							}
						}
						catch (Exception)
						{
							Arma2Path = "";
						} // No ArmA2 key found. Not started?

						try
						{
							RegistryKey oaKey = bohemiaKey.OpenSubKey("ArmA 2 OA", perm, rights);
							string possibleArma2OAPath = (string)oaKey.GetValue("main", "");
							oaKey.Close();
							oaKey.Dispose();
							if (Directory.Exists(possibleArma2OAPath))
							{
								Arma2OAPath = possibleArma2OAPath;
							}
							else
							{
								Arma2OAPath = "";
							}
						}
						catch (Exception)
						{
							Arma2OAPath = "";
						} // No ArmA2OA key found.

						bohemiaKey.Close();
					}

					// Try and figure out one's path based on the other...
					if (string.IsNullOrWhiteSpace(Arma2Path)
					    && !string.IsNullOrWhiteSpace(Arma2OAPath))
					{
						var pathInfo = new DirectoryInfo(Arma2OAPath);
						if (pathInfo.Parent != null)
						{
							string possibleArma2Path = Path.Combine(pathInfo.Parent.FullName, "arma 2");
							if (Directory.Exists(possibleArma2Path))
							{
								Arma2Path = possibleArma2Path;
							}
						}
					}
					if (!string.IsNullOrWhiteSpace(Arma2Path)
					    && string.IsNullOrWhiteSpace(Arma2OAPath))
					{
						var pathInfo = new DirectoryInfo(Arma2Path);
						if (pathInfo.Parent != null)
						{
							string possibleArma2OAPath = Path.Combine(pathInfo.Parent.FullName, "arma 2 operation arrowhead");
							if (Directory.Exists(possibleArma2OAPath))
							{
								Arma2OAPath = possibleArma2OAPath;
							}
						}
					}
				}
				catch (Exception) //no bohemia key found
				{
					Arma2Path = "";
					Arma2OAPath = "";
				}
			}
		}
	}
}
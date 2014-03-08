using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Deployment.Application;

// ReSharper disable InconsistentNaming
namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class LocalMachineInfo : BindableBase
	{
		private static LocalMachineInfo _current;
		public static LocalMachineInfo Current
		{
			get
			{
				if(_current == null)
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
				else
					return Assembly.GetEntryAssembly().GetName().Version; 
			}
		}

		private string _arma2Path;
		public string Arma2Path
		{
			get { return _arma2Path; }
			private set
			{
				_arma2Path = value;
				PropertyHasChanged("Arma2Path");
			}
		}

		private string _arma2OaPath;
		public string Arma2OAPath
		{
			get { return _arma2OaPath; }
			private set
			{
				_arma2OaPath = value;
				PropertyHasChanged("Arma2OAPath");
			}
		}

		private string _steamPath;
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
            using (var baseKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32))
			{
				var perm = RegistryKeyPermissionCheck.Default;
				var rights = RegistryRights.QueryValues;

				try
				{
					using (var steamKey = baseKey.OpenSubKey("SOFTWARE\\Valve\\Steam", perm, rights))
					{
						SteamPath = (string)steamKey.GetValue("InstallPath", "");
						steamKey.Close();
					}
				}
				catch (Exception) { SteamPath = ""; } //no steam key found

				try
				{
					using (var bohemiaKey = baseKey.OpenSubKey("SOFTWARE\\Bohemia Interactive Studio", perm, rights))
					{
						try
						{
							RegistryKey arma2Key = bohemiaKey.OpenSubKey("ArmA 2", perm, rights);
							Arma2Path = (string)arma2Key.GetValue("main", "");
							arma2Key.Close(); arma2Key.Dispose();
						}
						catch (Exception) { Arma2Path = ""; } //no arma2 key found

						try
						{
							RegistryKey oaKey = bohemiaKey.OpenSubKey("ArmA 2 OA", perm, rights);
							Arma2OAPath = (string)oaKey.GetValue("main", "");
							oaKey.Close(); oaKey.Dispose();
						}
						catch (Exception) { Arma2OAPath = ""; } //no arma2oa key found

						bohemiaKey.Close();
					}

					//Try and figure out one's path based on the other
					if (string.IsNullOrWhiteSpace(Arma2Path)
						&& !string.IsNullOrWhiteSpace(Arma2OAPath))
					{
						var pathInfo = new DirectoryInfo(Arma2OAPath);
						if (pathInfo.Parent != null)
						{
							Arma2Path = Path.Combine(pathInfo.Parent.FullName, "arma 2");
						}
					}
					if (!string.IsNullOrWhiteSpace(Arma2Path)
						&& string.IsNullOrWhiteSpace(Arma2OAPath))
					{
						var pathInfo = new DirectoryInfo(Arma2Path);
						if (pathInfo.Parent != null)
						{
							Arma2OAPath = Path.Combine(pathInfo.Parent.FullName, "arma 2 operation arrowhead");
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
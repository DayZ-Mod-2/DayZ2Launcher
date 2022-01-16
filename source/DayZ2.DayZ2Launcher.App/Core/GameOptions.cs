using System.Runtime.Serialization;
using DayZ2.DayZ2Launcher.App.Ui;

namespace DayZ2.DayZ2Launcher.App.Core
{
	[DataContract]
	public class GameOptions : ViewModelBase
	{
		[DataMember] private string m_additionalStartupParameters;
		[DataMember] private string m_arma2DirectoryOverride;
		[DataMember] private bool m_arma2OASteamUpdate;
		[DataMember] private string m_arma2OaDirectoryOverride;
		[DataMember] private bool m_closeDayZLauncher;
		[DataMember] private string m_customBranchName;
		[DataMember] private string m_customBranchPass;
		[DataMember] private bool m_multiGpu;
		[DataMember] private bool m_twentyFourHourTimeFormat;
		[DataMember] private bool m_windowedMode;

		public string AdditionalStartupParameters
		{
			get => m_additionalStartupParameters;
			set => SetValue(ref m_additionalStartupParameters, value, UserSettings.Current.Save);
		}

		public string GUID => GUIDCalculator.GetKey();

		public bool Arma2OASteamUpdate
		{
			get => m_arma2OASteamUpdate;
			set => SetValue(ref m_arma2OASteamUpdate, value, UserSettings.Current.Save);
		}

		public bool WindowedMode
		{
			get => m_windowedMode;
			set => SetValue(ref m_windowedMode, value, UserSettings.Current.Save);
		}

		public bool MultiGpu
		{
			get => m_multiGpu;
			set => SetValue(ref m_multiGpu, value, UserSettings.Current.Save);
		}

		public bool CloseDayZLauncher
		{
			get => m_closeDayZLauncher;
			set => SetValue(ref m_closeDayZLauncher, value, UserSettings.Current.Save);
		}

		public string Arma2DirectoryOverride
		{
			get => m_arma2DirectoryOverride;
			set => SetValue(ref m_arma2DirectoryOverride, value, () =>
			{
				UserSettings.Current.Save();
				CalculatedGameSettings.Current.Update();
			});
		}

		public string Arma2OADirectoryOverride
		{
			get => m_arma2OaDirectoryOverride;
			set => SetValue(ref m_arma2OaDirectoryOverride, value, () =>
			{
				UserSettings.Current.Save();
				CalculatedGameSettings.Current.Update();
			});
		}

		public string CustomBranchName
		{
			get => m_customBranchName;
			set => SetValue(ref m_customBranchName, value, UserSettings.Current.Save);
		}

		public string CustomBranchPass
		{
			get => m_customBranchPass;
			set => SetValue(ref m_customBranchPass, value, UserSettings.Current.Save);
		}

		public bool TwentyFourHourTimeFormat
		{
			get => m_twentyFourHourTimeFormat;
			set => SetValue(ref m_twentyFourHourTimeFormat, value, UserSettings.Current.Save);
		}
	}
}

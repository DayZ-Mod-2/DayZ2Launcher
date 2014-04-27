using System.Runtime.Serialization;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	[DataContract]
	public class GameOptions : BindableBase
	{
		[DataMember] private string _additionalStartupParameters;
		[DataMember] private bool _launchUsingSteam;
        [DataMember] private bool _arma2OASteamUpdate;
		[DataMember] private bool _windowedMode;
		[DataMember] private bool _multiGpu;
        [DataMember] private bool _closeDayZeroLauncher;
		[DataMember] private string _arma2DirectoryOverride;
		[DataMember] private string _arma2OaDirectoryOverride;
        [DataMember] private string _AddonsDirectoryOverride;
		[DataMember] private string _customBranchName;
		[DataMember] private string _customBranchPass;
		[DataMember] private bool _twentyFourHourTimeFormat;

		public string AdditionalStartupParameters
		{
			get { return _additionalStartupParameters; }
			set
			{
				_additionalStartupParameters = value;
				PropertyHasChanged("AdditionalStartupParameters");
				UserSettings.Current.Save();
			}
		}
		
		public bool LaunchUsingSteam
		{
			get { return _launchUsingSteam; }
			set
			{
				_launchUsingSteam = value;
				PropertyHasChanged("LaunchUsingSteam");
				UserSettings.Current.Save();
			}
		}

        public string GUID
        {
            get { return GUIDCalculator.GetKey(); }
        }

        public bool Arma2OASteamUpdate
		{
            get { return _arma2OASteamUpdate; }
			set
			{
                _arma2OASteamUpdate = value;
                PropertyHasChanged("Arma2OASteamUpdate");
				UserSettings.Current.Save();
			}
		}

		public bool WindowedMode
		{
			get { return _windowedMode; }
			set
			{
				_windowedMode = value;
				PropertyHasChanged("WindowedMode");
				UserSettings.Current.Save();
			}
		}

		public bool MultiGpu
		{
			get { return _multiGpu; }
			set
			{
				_multiGpu = value;
				PropertyHasChanged("MultiGpu");
				UserSettings.Current.Save();
			}
		}

        public bool CloseDayZeroLauncher
        {
            get { return _closeDayZeroLauncher; }
            set
            {
                _closeDayZeroLauncher = value;
                PropertyHasChanged("CloseDayZeroLauncher");
                UserSettings.Current.Save();
            }
        }

		public string Arma2DirectoryOverride
		{
			get { return _arma2DirectoryOverride; }
			set
			{
				_arma2DirectoryOverride = value;
				PropertyHasChanged("Arma2DirectoryOverride");
				UserSettings.Current.Save();
				CalculatedGameSettings.Current.Update();
			}
		}

		public string Arma2OADirectoryOverride
		{
			get { return _arma2OaDirectoryOverride; }
			set
			{
				_arma2OaDirectoryOverride = value;
				PropertyHasChanged("Arma2OADirectoryOverride");
				UserSettings.Current.Save();
				CalculatedGameSettings.Current.Update();
			}
		}

        public string AddonsDirectoryOverride
        {
            get { return _AddonsDirectoryOverride; }
            set
            {
                _AddonsDirectoryOverride = value;
				PropertyHasChanged("AddonsDirectoryOverride");
                UserSettings.Current.Save();
				CalculatedGameSettings.Current.Update();
            }
        }

		public string CustomBranchName
		{
			get { return _customBranchName; }
			set
			{
				_customBranchName = value;
				PropertyHasChanged("CustomBranchName");
				UserSettings.Current.Save();
			}
		}

		public string CustomBranchPass
		{
			get { return _customBranchPass; }
			set
			{
				_customBranchPass = value;
				PropertyHasChanged("CustomBranchPass");
				UserSettings.Current.Save();
			}
		}
		
		public bool TwentyFourHourTimeFormat
		{
			get { return _twentyFourHourTimeFormat; }
			set
			{
				_twentyFourHourTimeFormat = value;
				PropertyHasChanged("TwentyFourHourTimeFormat");
				UserSettings.Current.Save();
			}
		}
	}
}
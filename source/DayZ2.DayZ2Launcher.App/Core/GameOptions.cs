using System.Runtime.Serialization;

namespace DayZ2.DayZ2Launcher.App.Core
{
    [DataContract]
    public class GameOptions : BindableBase
    {
        [DataMember] private string _additionalStartupParameters;
        [DataMember] private string _arma2DirectoryOverride;
        [DataMember] private bool _arma2OASteamUpdate;
        [DataMember] private string _arma2OaDirectoryOverride;
        [DataMember] private bool _closeDayZLauncher;
        [DataMember] private string _customBranchName;
        [DataMember] private string _customBranchPass;
        [DataMember] private bool _launchUsingSteam;
        [DataMember] private bool _multiGpu;
        [DataMember] private bool _twentyFourHourTimeFormat;
        [DataMember] private bool _windowedMode;

        public string AdditionalStartupParameters
        {
            get => _additionalStartupParameters;
            set
            {
                _additionalStartupParameters = value;
                PropertyHasChanged("AdditionalStartupParameters");
                UserSettings.Current.Save();
            }
        }

        public bool LaunchUsingSteam
        {
            get => _launchUsingSteam;
            set
            {
                _launchUsingSteam = value;
                PropertyHasChanged("LaunchUsingSteam");
                UserSettings.Current.Save();
            }
        }

        public string GUID => GUIDCalculator.GetKey();

        public bool Arma2OASteamUpdate
        {
            get => _arma2OASteamUpdate;
            set
            {
                _arma2OASteamUpdate = value;
                PropertyHasChanged("Arma2OASteamUpdate");
                UserSettings.Current.Save();
            }
        }

        public bool WindowedMode
        {
            get => _windowedMode;
            set
            {
                _windowedMode = value;
                PropertyHasChanged("WindowedMode");
                UserSettings.Current.Save();
            }
        }

        public bool MultiGpu
        {
            get => _multiGpu;
            set
            {
                _multiGpu = value;
                PropertyHasChanged("MultiGpu");
                UserSettings.Current.Save();
            }
        }

        public bool CloseDayZLauncher
        {
            get => _closeDayZLauncher;
            set
            {
                _closeDayZLauncher = value;
                PropertyHasChanged("CloseDayZLauncher");
                UserSettings.Current.Save();
            }
        }

        public string Arma2DirectoryOverride
        {
            get => _arma2DirectoryOverride;
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
            get => _arma2OaDirectoryOverride;
            set
            {
                _arma2OaDirectoryOverride = value;
                PropertyHasChanged("Arma2OADirectoryOverride");
                UserSettings.Current.Save();
                CalculatedGameSettings.Current.Update();
            }
        }

        public string CustomBranchName
        {
            get => _customBranchName;
            set
            {
                _customBranchName = value;
                PropertyHasChanged("CustomBranchName");
                UserSettings.Current.Save();
            }
        }

        public string CustomBranchPass
        {
            get => _customBranchPass;
            set
            {
                _customBranchPass = value;
                PropertyHasChanged("CustomBranchPass");
                UserSettings.Current.Save();
            }
        }

        public bool TwentyFourHourTimeFormat
        {
            get => _twentyFourHourTimeFormat;
            set
            {
                _twentyFourHourTimeFormat = value;
                PropertyHasChanged("TwentyFourHourTimeFormat");
                UserSettings.Current.Save();
            }
        }
    }
}

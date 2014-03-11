using zombiesnu.DayZeroLauncher.App.Core;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	public class SettingsViewModel : ViewModelBase
	{
		private bool _isVisible;

		public SettingsViewModel()
		{
			Settings = UserSettings.Current;
			if (string.IsNullOrWhiteSpace(Settings.GameOptions.CustomBranchName))
			{
				CustomBranchEnabled = false;
				CustomBranchName = "release";
			}
			else
			{
				CustomBranchName = Settings.GameOptions.CustomBranchName;
				CustomBranchEnabled = true;
			}

			this.PropertyChanged += (sender, args) =>
			{
				//reconfigure TorrentEngine every time we close this panel, not just on clicking Done
				if (args.PropertyName == "IsVisible")
				{
					if (this.IsVisible == false)
						TorrentUpdater.ReconfigureEngine();
				}
			};
		}

		public UserSettings Settings { get; set; }

		public bool IsVisible
		{
			get { return _isVisible; }
			set
			{
				_isVisible = value;
				PropertyHasChanged("IsVisible");
			}
		}

        public bool IncludeUS
        {
            get { return Settings.IncludeUS; }
            set
            {
                Settings.IncludeUS = value;
                PropertyHasChanged("IncludeUS");
            }
        }

        public bool IncludeEU
        {
            get { return Settings.IncludeEU; }
            set
            {
                Settings.IncludeEU = value;
                PropertyHasChanged("IncludeEU");
            }
        }

        public bool IncludeAU
        {
            get { return Settings.IncludeAU; }
            set
            {
                Settings.IncludeAU = value;
                PropertyHasChanged("IncludeAU");
            }
        }

        public bool Arma2DirectoryOverride
        {
            get { return !string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2DirectoryOverride); }
            set
            {
                if (value)
                    Settings.GameOptions.Arma2DirectoryOverride = LocalMachineInfo.Current.Arma2Path ?? "Replace with full Arma2 Path";
                else
                    Settings.GameOptions.Arma2DirectoryOverride = null;

                PropertyHasChanged("Arma2Directory", "Arma2DirectoryOverride");
            }
        }

		public string Arma2Directory
		{
			get
			{
				if(!string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2DirectoryOverride))
				{
					return Settings.GameOptions.Arma2DirectoryOverride;
				}
				return LocalMachineInfo.Current.Arma2Path;
			}
			set
			{
				Settings.GameOptions.Arma2DirectoryOverride = value;

				PropertyHasChanged("Arma2Directory");
			}
		}

		public string Arma2OADirectory
		{
			get
			{
				if(!string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2OADirectoryOverride))
				{
					return Settings.GameOptions.Arma2OADirectoryOverride;
				}
				return LocalMachineInfo.Current.Arma2OAPath;
			}
			set
			{
				Settings.GameOptions.Arma2OADirectoryOverride = value;

				PropertyHasChanged("Arma2OADirectory", "Arma2OADirectoryOverride");
			}
		}

		public bool Arma2OADirectoryOverride
		{
			get { return !string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2OADirectoryOverride); }
			set
			{
				if(value)
					Settings.GameOptions.Arma2OADirectoryOverride = LocalMachineInfo.Current.Arma2OAPath ?? "Replace with full Arma2 OA Path";
				else
					Settings.GameOptions.Arma2OADirectoryOverride = null;

				PropertyHasChanged("Arma2OADirectory", "Arma2OADirectoryOverride");
			}
		}

        public string AddonsDirectory
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Settings.GameOptions.AddonsDirectoryOverride))
                {
                    return Settings.GameOptions.AddonsDirectoryOverride;
                }
                return LocalMachineInfo.Current.Arma2OAPath;
            }
            set
            {
                Settings.GameOptions.AddonsDirectoryOverride = value;
				PropertyHasChanged("AddonsDirectory", "AddonsDirectoryOverride");
            }
        }

        public bool AddonsDirectoryOverride
        {
            get { return !string.IsNullOrWhiteSpace(Settings.GameOptions.AddonsDirectoryOverride); }
            set
            {
                if (value)
					Settings.GameOptions.AddonsDirectoryOverride = Settings.GameOptions.AddonsDirectoryOverride ?? Arma2OADirectory;
                else
                    Settings.GameOptions.AddonsDirectoryOverride = null;

				PropertyHasChanged("AddonsDirectory", "AddonsDirectoryOverride");
            }
        }

		private bool _customBranchEnabled;
		public bool CustomBranchEnabled
		{
			get { return _customBranchEnabled; }
			set
			{
				_customBranchEnabled = value;
				if (value)
					Settings.GameOptions.CustomBranchName = CustomBranchName;
				else
					Settings.GameOptions.CustomBranchName = "";

				PropertyHasChanged("CustomBranchEnabled", "CustomBranchName");
			}
		}

		private string _customBranchName;
		public string CustomBranchName
		{
			get { return _customBranchName; }
			set
			{
				_customBranchName = value;
				if (CustomBranchEnabled)
					Settings.GameOptions.CustomBranchName = value;
				else
					Settings.GameOptions.CustomBranchName = "";

				PropertyHasChanged("CustomBranchName");
			}
		}

		public string DisplayDirectoryPrompt(System.Windows.Window parentWindow, bool allowNewFolder, string previousPath, string description)
		{
			var folderDlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

			if (allowNewFolder)
				folderDlg.ShowNewFolderButton = true;
			else
				folderDlg.ShowNewFolderButton = false;

			folderDlg.RootFolder = System.Environment.SpecialFolder.ProgramFilesX86;
			if (previousPath != null)
				folderDlg.SelectedPath = previousPath;

			if (description != null)
			{
				folderDlg.Description = description;
				folderDlg.UseDescriptionForTitle = true;
			}

			bool dialogAccepted = folderDlg.ShowDialog(parentWindow) ?? false;
			if (!dialogAccepted)
				return null;

			return folderDlg.SelectedPath;
		}

		public void Done()
		{
			IsVisible = false;
		}
	}
}
using System;
using System.Windows;
using Ookii.Dialogs.Wpf;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool _customBranchEnabled;
        private string _customBranchName;
        private bool _isVisible;

        public EventHandler TorrentSettingsChanged;

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

            PropertyChanged += (sender, args) =>
            {
                //reconfigure TorrentEngine every time we close this panel, not just on clicking Done
                if (args.PropertyName == "IsVisible")
                {
                    if (IsVisible == false)
                        TorrentSettingsChanged?.Invoke(this, null);
                }
            };
        }

        public UserSettings Settings { get; set; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                PropertyHasChanged("IsVisible");
            }
        }

        public bool IncludeUS
        {
            get => Settings.IncludeUS;
            set
            {
                Settings.IncludeUS = value;
                PropertyHasChanged("IncludeUS");
            }
        }

        public bool IncludeEU
        {
            get => Settings.IncludeEU;
            set
            {
                Settings.IncludeEU = value;
                PropertyHasChanged("IncludeEU");
            }
        }

        public bool IncludeAU
        {
            get => Settings.IncludeAU;
            set
            {
                Settings.IncludeAU = value;
                PropertyHasChanged("IncludeAU");
            }
        }

        public bool Arma2DirectoryOverride
        {
            get => !string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2DirectoryOverride);
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
                if (!string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2DirectoryOverride))
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
                if (!string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2OADirectoryOverride))
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
            get => !string.IsNullOrWhiteSpace(Settings.GameOptions.Arma2OADirectoryOverride);
            set
            {
                if (value)
                    Settings.GameOptions.Arma2OADirectoryOverride = LocalMachineInfo.Current.Arma2OAPath ??
                                                                    "Replace with full Arma2 OA Path";
                else
                    Settings.GameOptions.Arma2OADirectoryOverride = null;

                PropertyHasChanged("Arma2OADirectory", "Arma2OADirectoryOverride");
            }
        }

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

        public string DisplayDirectoryPrompt(Window parentWindow, bool allowNewFolder, string previousPath, string description)
        {
            var folderDlg = new VistaFolderBrowserDialog();

            if (allowNewFolder)
                folderDlg.ShowNewFolderButton = true;
            else
                folderDlg.ShowNewFolderButton = false;

            folderDlg.RootFolder = Environment.SpecialFolder.ProgramFilesX86;
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

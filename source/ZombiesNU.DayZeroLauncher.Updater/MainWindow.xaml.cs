using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using zombiesnu.DayZeroLauncher.InstallUtilities;
using NLog;

namespace zombiesnu.DayZeroLauncher.Updater
{
	/// <summary>
	/// 	Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public MainWindow()
		{
			InitializeComponent();
			Loaded += OnLoaded;
		}

		private static void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
		{
			try
			{
				RunInstaller();
			}
			catch(Exception ex)
			{
				_logger.Error(ex);
				LaunchDayZeroLauncher();
				Environment.Exit(0);
			}
		}

        private static void RunInstaller()
        {
            var thisLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    Arguments = "/i DayZeroLauncher.msi /quiet",
                    WorkingDirectory = Path.Combine(thisLocation, DownloadAndExtracter.PENDING_UPDATE_DIRECTORYNAME),
                    FileName = "msiexec"
                }
            };
            p.Start();
            p.WaitForExit();
            Directory.Delete(Path.Combine(thisLocation, DownloadAndExtracter.PENDING_UPDATE_DIRECTORYNAME), true);

            LaunchDayZeroLauncher();
            Environment.Exit(0);

        }

		private static void LaunchDayZeroLauncher()
		{
            var thisLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var p = new Process
			        	{
			        		StartInfo = new ProcessStartInfo
			        		            	{
			        		            		CreateNoWindow = false,
			        		            		UseShellExecute = true,
                                                WorkingDirectory = thisLocation,
                                                FileName = Path.Combine(thisLocation, "DayZeroLauncher.exe")
			        		            	}
			        	};
			p.Start();
			Environment.Exit(0);
		}
	}
}
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using zombiesnu.DayZeroLauncher.App.Core;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;
using Microsoft.Win32;

namespace zombiesnu.DayZeroLauncher.App
{
	public partial class App : Application
	{
		public static EventAggregator Events = new EventAggregator();
		
		protected override void OnStartup(StartupEventArgs e)
		{
			AppDomain.CurrentDomain.UnhandledException += UncaughtThreadException;
			DispatcherUnhandledException += UncaughtUiThreadException;

			LocalMachineInfo.Current.Update();
			base.OnStartup(e);
		}

		private void UncaughtException(Exception ex)
		{
			MessageBox.Show("It wasn't your fault, but something went really wrong.\r\nThe application will now exit\r\nException Details:\r\n" + ex.ToString(),
							"Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private bool _isUncaughtUiThreadException;
		private void UncaughtUiThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			_isUncaughtUiThreadException = true;
			UncaughtException(e.Exception);
		}

		private void UncaughtThreadException(object sender, UnhandledExceptionEventArgs e)
		{
			if(!_isUncaughtUiThreadException)
				UncaughtException(e.ExceptionObject as Exception);
		}
	}
}

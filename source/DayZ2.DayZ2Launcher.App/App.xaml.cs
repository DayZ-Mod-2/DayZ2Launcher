using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.DependencyInjection;

using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui;

namespace DayZ2.DayZ2Launcher.App
{
	public class AppCancellation
	{
		public CancellationToken Token { get; private set; }

		public AppCancellation(CancellationToken token)
		{
			Token = token;
		}
	}

	public partial class App : Application
	{
		readonly ServiceProvider m_serviceProvider;
		readonly CancellationTokenSource m_cancellationTokenSource = new();

		bool m_isUncaughtUiThreadException;

		public App()
		{
			ServiceCollection services = new();
			ConfigureServices(services);

			services.AddSingleton<IServiceProvider>(sp => sp);
			m_serviceProvider = services.BuildServiceProvider();
		}

		bool TryStartUniqueInstance()
		{
			const string EventGuid = "{a08c9217-041a-4d36-80a4-604d616c637b}";
			const string MutexGuid = "{5ffa3650-2e03-456f-8903-6ec964b2161b}";

			string userId;
			using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
				userId = identity.User?.ToString() ?? "null";

			string eventName = $"DayzLauncher-EVENT-{{{EventGuid}}}-{{{userId}}}";
			string mutexName = $"DayzLauncher-MUTEX-{{{MutexGuid}}}-{{{userId}}}";

			var sharedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

			bool wasCreated;
			var sharedMutex = new Mutex(true, mutexName, out wasCreated);

			if (!wasCreated)
			{
				sharedEvent.Set();
				return false;
			}

			// The mutex must be kept alive.
			var thread = new Thread((object mutex) =>
			{
				while (sharedEvent.WaitOne())
				{
					Current.Dispatcher.BeginInvoke(() => ((MainWindow)Current.MainWindow).BringToForeground());
				}
			});

			thread.IsBackground = true;
			thread.Start(sharedMutex);

			return true;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			if (!TryStartUniqueInstance())
			{
				Shutdown();
				return;
			}

			LocalMachineInfo.Current.Update();

			base.OnStartup(e);
		}

		void ApplicationStartup(object sender, StartupEventArgs e)
		{
			var mainWindow = new MainWindow
			{
				DataContext = m_serviceProvider.CreateInstance<MainWindowViewModel>()
			};
			mainWindow.Show();
		}

		void ConfigureServices(ServiceCollection services)
		{
			services.AddSingleton(new AppCancellation(m_cancellationTokenSource.Token));
			services.AddSingleton<ResourceLocator>();
			services.AddSingleton<GameLauncher>();
			services.AddSingleton<ModUpdater>();
		}


		void UncaughtException(Exception ex)
		{
			MessageBox.Show(
				"It wasn't your fault, but something went really wrong.\r\nThe application will now exit\r\nException Details:\r\n" +
				ex,
				"Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		void UncaughtUiThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			m_isUncaughtUiThreadException = true;
			UncaughtException(e.Exception);
		}

		void UncaughtThreadException(object sender, UnhandledExceptionEventArgs e)
		{
			if (!m_isUncaughtUiThreadException)
				UncaughtException(e.ExceptionObject as Exception);
		}
	}
}

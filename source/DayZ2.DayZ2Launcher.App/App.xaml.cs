using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		class AsyncDisposableProxy : IAsyncDisposable
		{
			readonly IDisposable m_disposable;

			public AsyncDisposableProxy(IDisposable disposable)
			{
				m_disposable = disposable;
			}

			public ValueTask DisposeAsync()
			{
				m_disposable.Dispose();
				return ValueTask.CompletedTask;
			}
		}

		public static new App Current => (App)Application.Current;

		readonly ServiceProvider m_serviceProvider;
		readonly CancellationTokenSource m_cancellationTokenSource = new();
		readonly List<IAsyncDisposable> m_shutdownCleanup = new();

		bool m_shutdownRequested = false;

		bool m_isUncaughtUiThreadException;

		public App()
		{
			ServiceCollection services = new();
			ConfigureServices(services);

			services.AddSingleton<IServiceProvider>(sp => sp);
			m_serviceProvider = services.BuildServiceProvider();
		}

		public void OnShutdown(IDisposable disposable)
		{
			m_shutdownCleanup.Add(new AsyncDisposableProxy(disposable));
		}

		public void OnShutdown(IAsyncDisposable disposable)
		{
			m_shutdownCleanup.Add(disposable);
		}

		bool TryStartUniqueInstance()
		{
			const string Guid = "{a08c9217-041a-4d36-80a4-604d616c637b}";

#pragma warning disable CA1416
			string userId;
			using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
				userId = identity.User?.ToString() ?? "null";
#pragma warning restore CA1416

			string eventName = $"DayzLauncher-EVENT-{Guid}-{{{userId}}}";
			string mutexName = $"DayzLauncher-MUTEX-{Guid}-{{{userId}}}";

			var sharedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

			bool wasCreated;
			var sharedMutex = new Mutex(true, mutexName, out wasCreated);

			if (!wasCreated)
			{
				sharedEvent.Set();
				return false;
			}

			var thread = new Thread(() =>
			{
				while (sharedEvent.WaitOne())
				{
					Current.Dispatcher.BeginInvoke(() => ((MainWindow)Current.MainWindow).BringToForeground());
				}
				GC.KeepAlive(sharedMutex);
			});

			thread.IsBackground = true;
			thread.Start();

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

		public void RequestShutdown()
		{
			if (m_shutdownRequested) return;
			m_shutdownRequested = true;

			foreach (Window window in Windows.Cast<Window>())
			{
				window.Close();
			}

			m_cancellationTokenSource.Cancel();

			Shutdown();

			/*
			async void ShutdownAsync()
			{
				const int Timeout = 5000;
				var shutdownTask = Task.WhenAll(m_shutdownCleanup.Select(async n =>
				{
					try
					{
						await n.DisposeAsync().AsTask();
					}
					catch (OperationCanceledException ex)
					{
					}
				}));
				//await Task.WhenAny(shutdownTask, Task.Delay(Timeout));
				var delayTask = Task.Delay(Timeout);
				bool timeout = await Task.WhenAny(shutdownTask, delayTask) == delayTask;

				Shutdown();
			}
			ShutdownAsync();
			*/
		}

		public void BringToForeground()
		{
			Current.Dispatcher.BeginInvoke(() => ((MainWindow)Current.MainWindow)?.BringToForeground());
		}

		public void Minimize()
		{
			Current.Dispatcher.BeginInvoke(() =>
			{
				if (MainWindow != null)
					MainWindow.WindowState = WindowState.Minimized;
			});
		}

		void ApplicationStartup(object sender, StartupEventArgs e)
		{
			var mainWindow = new MainWindow
			{
				DataContext = m_serviceProvider.CreateInstance<MainWindowViewModel>()
			};

			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			mainWindow.Closed += (object sender, EventArgs e) => RequestShutdown();

			mainWindow.Show();
		}

		void ConfigureServices(ServiceCollection services)
		{
			services.AddSingleton(new AppCancellation(m_cancellationTokenSource.Token));
			services.AddSingleton<AppActions>();
			services.AddSingleton<HttpClient>();
			services.AddSingleton<ResourceLocator>();
			services.AddSingleton<GameLauncher>();
			services.AddSingleton<ModUpdater>();
			services.AddSingleton<MotdUpdater>();
			services.AddSingleton<ServerUpdater>();
			services.AddSingleton<TorrentClient>();
			services.AddSingleton<CrashLogUploader>();
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

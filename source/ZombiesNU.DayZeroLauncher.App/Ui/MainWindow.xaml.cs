using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Net;
using System.Threading;
using Caliburn.Micro;
using zombiesnu.DayZeroLauncher.App.Core;
using System.Collections.Specialized;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, IHandle<App.LaunchCommandString>
	{
		private Timer _motdTimer;
		private bool _windowClosing = false;
		private void MotdDownloadComplete(object sender, DownloadStringCompletedEventArgs e)
		{
			using (var wc = (WebClient)sender)
			{
				if (e.Error != null)
				{
					bool ignoreError = false;
					if (e.Error is WebException)
					{
						var webExc = (WebException)e.Error;
						if (webExc.Response != null)
						{
							var httpResponse = (HttpWebResponse)webExc.Response;
							if (httpResponse.StatusCode == HttpStatusCode.NotFound)
								ignoreError = true;
						}
					}

					if (!ignoreError)
						SetMotdContents("Error getting message: " + e.Error.Message, true);
					else
						SetMotdContents("");
				}	
				else
					SetMotdContents(e.Result);
			}

			if (_windowClosing)
				return;

			var fiveMinuteDelay = (long)TimeSpan.FromMinutes(5).TotalMilliseconds;

			if (_motdTimer == null)
				_motdTimer = new Timer(StartMotdRequest, null, fiveMinuteDelay, Timeout.Infinite);
			else
				_motdTimer.Change(fiveMinuteDelay, Timeout.Infinite);
		}

		public void StartMotdRequest(object reqData=null)
		{
			var locator = CalculatedGameSettings.Current.Locator;
			if (reqData != null)
				locator = (LocatorInfo)reqData;

			string motdUrl = "https://update.zombies.nu/motd.txt";
			if (locator != null && locator.MotdUrl != null)
				motdUrl = locator.MotdUrl;

			if (string.IsNullOrEmpty(motdUrl))
			{
				SetMotdContents(null);
				return;
			}

			var wc = new WebClient();
			wc.DownloadStringCompleted += MotdDownloadComplete;
			wc.DownloadStringAsync(new Uri(motdUrl));
		}

		private void SetMotdContents(string motdText, bool error = false)
		{
			if (_windowClosing)
				return;

			Execute.OnUiThreadSync(() =>
				{
					AnnouncementMessage.Text = motdText;
					if (error)
						AnnouncementMessage.Foreground = System.Windows.Media.Brushes.Red;
					else
						AnnouncementMessage.Foreground = System.Windows.Media.Brushes.White;

					if (!String.IsNullOrEmpty(motdText))
						AnnouncementBorder.BorderBrush = System.Windows.Media.Brushes.Red;
					else
						AnnouncementBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
				}, this.Dispatcher, System.Windows.Threading.DispatcherPriority.ContextIdle);	
		}

		public class LaunchRoutedCommand
		{
			public LaunchRoutedCommand(NameValueCollection data, Window mainWnd)
			{
				Data = data;
				SourceWindow = mainWnd;
			}

			public NameValueCollection Data;
			public Window SourceWindow;
		}

		public MainWindow()
		{
			InitializeComponent();
			App.Events.Subscribe(this);

			KeyUp += OnKeyUp;

			Loaded += (sender, args) =>
			{
				if(UserSettings.Current.WindowSettings != null)
					UserSettings.Current.WindowSettings.Apply(this);

				var vm = new MainWindowViewModel();
				vm.UpdatesViewModel.LocatorChanged += (obj, e) =>
					{
						if (e.Cancelled == false && e.Error == null)
							StartMotdRequest(e.UserState);
						else
							StartMotdRequest(null);
					};
				DataContext = vm;

				//this will activate the window and do any command from cmdline if it exists
				App.Events.Publish(new App.LaunchCommandString(App.GetQueryParams()));
			};
			Closing += (sender, args) =>
			{
				App.Events.Unsubscribe(this);
				_windowClosing = true;				

				if (_motdTimer != null)
				{
					_motdTimer.Dispose();
					_motdTimer = null;
				}

				UserSettings.Current.WindowSettings = WindowSettings.Create(this);
				UserSettings.Current.Save();
			};

            this.Loaded += new RoutedEventHandler(Window_Loaded);
            this.SizeChanged += new SizeChangedEventHandler(WindowSize_Changed);
		}

        private void WindowSize_Changed(object sender, SizeChangedEventArgs e)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.From = Marquee.ActualWidth;
            doubleAnimation.To = -AnnouncementMessage.ActualWidth;
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            doubleAnimation.Duration = new Duration(TimeSpan.Parse("0:0:20"));
            AnnouncementMessage.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            //doubleAnimation.From = -AnnouncementMessage.ActualWidth;
            doubleAnimation.From = Marquee.ActualWidth;
            //doubleAnimation.To = Marquee.ActualWidth;
            doubleAnimation.To = -AnnouncementMessage.ActualWidth;
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            doubleAnimation.Duration = new Duration(TimeSpan.Parse("0:0:20"));
            AnnouncementMessage.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
        }

		public void Handle(App.LaunchCommandString launchEvt)
		{
			Execute.OnUiThread(() => Activate(), Dispatcher, System.Windows.Threading.DispatcherPriority.Input);
			string queryString = launchEvt.QueryString;
			if (string.IsNullOrWhiteSpace(queryString))
				return;

			NameValueCollection nameValueTable = null;
			string actionVal = null;
			try
			{
				nameValueTable = HttpUtility.ParseQueryString(queryString);
				actionVal = nameValueTable.Get("action");
			}
			catch (Exception ex)
			{
				System.Console.WriteLine("Exception parsing cmdline: " + ex.Message);
				actionVal = null;
			}

			if ("join".Equals(actionVal, StringComparison.OrdinalIgnoreCase))
			{
				System.Net.IPAddress ipAddr;
				int serverPort = 0;
				try
				{
					ipAddr = System.Net.IPAddress.Parse(nameValueTable.Get("ip"));
					serverPort = UInt16.Parse(nameValueTable.Get("port"), System.Globalization.CultureInfo.InvariantCulture);
				}
				catch (Exception ex)
				{
					System.Console.WriteLine("Exception parsing join args: " + ex.Message);
					ipAddr = null;
				}

				if (ipAddr != null && serverPort > 0)
					App.Events.Publish(new ServerListGrid.LaunchJoinServerEvent(ipAddr.ToString(), serverPort,
										 nameValueTable, this, launchEvt.QueryString));
			}
			else if ("launch".Equals(actionVal, StringComparison.OrdinalIgnoreCase))
			{
				string gameType = null;
				try
				{
					gameType = nameValueTable.Get("gameType");
					if (string.IsNullOrWhiteSpace(gameType))
						throw new ArgumentNullException("gameType");
				}
				catch (Exception ex)
				{
					System.Console.WriteLine("Exception parsing launch args: " + ex.Message);
					gameType = null;
				}

				if (gameType != null)
					App.Events.Publish(new GameLauncher.LaunchStartGameEvent(gameType, nameValueTable, this));
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
		{
			if(keyEventArgs.Key == Key.Escape)
			{
				ViewModel.Escape();
			}
		}

		private MainWindowViewModel ViewModel
		{
			get { return ((MainWindowViewModel) DataContext); }
		}

		private void MainWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		private void CloseButtonClick(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void MainWindow_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(e.OriginalSource != VisualRoot)
				return;

			ToggleMaximized();
		}

		private void ToggleMaximized()
		{
			if(WindowState == WindowState.Normal)
				WindowState = WindowState.Maximized;
			else
				WindowState = WindowState.Normal;
		}

		private void RefreshAll_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ServerList.UpdateAll();
		}

		private void TabHeader_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.CurrentTab = (ViewModelBase) ((Control) sender).DataContext;
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowSettings();
		}

		private void Donate_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=S2VZJLUWUG8RG");
		}

		private void MinimizeClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void ToggleMaxamimizeClick(object sender, RoutedEventArgs e)
		{
			ToggleMaximized();
		}

		private void Updates_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowUpdates();
		}

		private void Plugins_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowPlugins();
		}

        private void LaunchGameButton_Click(object sender, RoutedEventArgs e)
        {
			var buttonObj = (FrameworkElement)sender;
			var buttonContext = (GameLauncher.ButtonInfo)buttonObj.DataContext;

			ViewModel.Launcher.LaunchGame(this, buttonContext.Argument);
        }

        private void ZombiesNUImage_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://zombies.nu");
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }
	}
}

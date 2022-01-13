using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Caliburn.Micro;
using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui.Controls;

namespace DayZ2.DayZ2Launcher.App.Ui
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			App.Events.Subscribe(this);

			KeyUp += OnKeyUp;

			Loaded += (sender, args) =>
			{
				if (UserSettings.Current.WindowSettings != null)
					UserSettings.Current.WindowSettings.Apply(this);

				var vm = new MainWindowViewModel();
				DataContext = vm;

				//this will activate the window and do any command from cmdline if it exists
				App.Events.Publish(new App.LaunchCommandString(App.GetQueryParams()));
			};
			Closing += (sender, args) =>
			{
				App.Events.Unsubscribe(this);

				UserSettings.Current.WindowSettings = WindowSettings.Create(this);
				UserSettings.Current.Save();
			};

			Loaded += Window_Loaded;
			SizeChanged += WindowSize_Changed;
		}

		private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

		private void WindowSize_Changed(object sender, SizeChangedEventArgs e)
		{
			var doubleAnimation = new DoubleAnimation
			{
				From = Marquee.ActualWidth,
				To = -AnnouncementMessage.ActualWidth,
				RepeatBehavior = RepeatBehavior.Forever,
				Duration = new Duration(TimeSpan.Parse("0:0:20"))
			};
			AnnouncementMessage.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var doubleAnimation = new DoubleAnimation
			{
				From = Marquee.ActualWidth,
				To = -AnnouncementMessage.ActualWidth,
				RepeatBehavior = RepeatBehavior.Forever,
				Duration = new Duration(TimeSpan.Parse("0:0:20"))
			};
			AnnouncementMessage.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
		}

		private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
		{
			if (keyEventArgs.Key == Key.Escape)
			{
				ViewModel.Escape();
			}
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
			if (!Equals(e.OriginalSource, VisualRoot))
				return;

			ToggleMaximized();
		}

		private void ToggleMaximized()
		{
			if (WindowState == WindowState.Normal)
				WindowState = WindowState.Maximized;
			else
				WindowState = WindowState.Normal;
		}

		private void RefreshAll_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ServerList.UpdateAll();
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowSettings();
		}

		private void MinimizeClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void ToggleMaximizeClick(object sender, RoutedEventArgs e)
		{
			ToggleMaximized();
		}

		private void Updates_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ShowUpdates();
		}

		private void LaunchGameButton_Click(object sender, RoutedEventArgs e)
		{
			var buttonObj = (FrameworkElement)sender;
			var buttonContext = (GameLauncher_old.ButtonInfo)buttonObj.DataContext;

			ViewModel.Launcher.LaunchGame(this, buttonContext.Argument);
		}

		private void DiscordImage_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://discord.gg/7B69YbrJKJ");
		}

		private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
		{
		}

		public class LaunchRoutedCommand
		{
			public NameValueCollection Data;
			public Window SourceWindow;

			public LaunchRoutedCommand(NameValueCollection data, Window mainWnd)
			{
				Data = data;
				SourceWindow = mainWnd;
			}
		}
	}
}

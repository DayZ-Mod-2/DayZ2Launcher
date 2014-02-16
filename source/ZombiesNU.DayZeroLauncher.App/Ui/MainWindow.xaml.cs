using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using zombiesnu.DayZeroLauncher.App.Core;
using Application = System.Windows.Application;
using Control = System.Windows.Controls.Control;

using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			KeyUp += OnKeyUp;

			Loaded += (sender, args) =>
			{
				if(UserSettings.Current.WindowSettings != null)
				{
					UserSettings.Current.WindowSettings.Apply(this);
				}
				Activate();
				DataContext = new MainWindowViewModel();
			};
			Closing += (sender, args) =>
			{
				UserSettings.Current.WindowSettings = WindowSettings.Create(this);
				UserSettings.Current.Save();
			};


            this.Loaded += new RoutedEventHandler(Window_Loaded);
            this.SizeChanged += new SizeChangedEventHandler(WindowSize_Changed);

            string responseBody = "";
            if (GameUpdater.HttpGet("http://www.zombies.nu/motd.txt", out responseBody))
            {
                AnnouncementMessage.Text = responseBody;
            }
            if (!String.IsNullOrEmpty(responseBody))
                AnnouncementBorder.BorderBrush = System.Windows.Media.Brushes.Red;
            else
                AnnouncementBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
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

		private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
		{
			if(keyEventArgs.Key == Key.Escape)
			{
				ViewModel.Escape();
			}
		}

		public string CurrentVersion { get; set; }

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
			{
				//UAC Crash
//				var screen = Screen.FromHandle(new WindowInteropHelper(this).Handle);
//				MaxHeight = screen.WorkingArea.Height;
				WindowState = WindowState.Maximized;
			}
			else
			{
				WindowState = WindowState.Normal;
			}
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

        private void LaunchPodagorskButton_Click(object sender, RoutedEventArgs e)
        {
            GameLauncher.LaunchGame(this,Mod.DayZeroPodagorsk);
        }

        private void LaunchChernarusButton_Click(object sender, RoutedEventArgs e)
        {
            GameLauncher.LaunchGame(this,Mod.DayZeroChernarus);
        }

        private void BMRFImage_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://bmrf.me");
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
